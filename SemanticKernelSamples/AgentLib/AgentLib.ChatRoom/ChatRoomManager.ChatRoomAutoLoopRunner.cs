using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

public sealed partial class ChatRoomManager
{
    private sealed class ChatRoomAutoLoopRunner
    {
        public ChatRoomAutoLoopRunner(ChatRoomManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        private readonly ChatRoomManager _manager;

        private CancellationTokenSource? _autoLoopCancellationTokenSource;

        /// <summary>
        /// 人类插话信号。当自动循环运行期间用户插话时设置为 1，
        /// 促使自动循环在当前角色发言完成后重启循环，
        /// 使选择器检测到人类消息并让助手立即回话用户。
        /// 使用 int + Interlocked 以保证原子消费，避免多次插话时的竞态。
        /// </summary>
        private int _humanInterjectSignal;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_manager.IsRunning)
            {
                return;
            }

            _autoLoopCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken loopCancellationToken = _autoLoopCancellationTokenSource.Token;

            _manager.IsRunning = true;

            try
            {
                while (!loopCancellationToken.IsCancellationRequested)
                {
                    ChatRoomMessage? triggerMessage = GetLatestTriggerMessage();
                    if (triggerMessage is null)
                    {
                        break;
                    }

                    bool shouldRestart = await RunAutoLoopTurnAsync(triggerMessage, loopCancellationToken)
                        .ConfigureAwait(false);
                    if (!shouldRestart)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (loopCancellationToken.IsCancellationRequested)
            {
                // 正常取消
            }
            finally
            {
                _manager.IsRunning = false;
                _manager.CurrentSpeaker = null;
                _autoLoopCancellationTokenSource?.Dispose();
                _autoLoopCancellationTokenSource = null;

                // 自动循环结束后持久化会话（AI 发言产生的新消息已通过 AppendMessageAsync 持久化，
                // 此处兜底确保角色定义等状态变更被保存）
                _ = _manager.SaveAsync();
            }
        }

        public void Stop()
        {
            _autoLoopCancellationTokenSource?.Cancel();
        }

        private async Task<bool> RunAutoLoopTurnAsync(ChatRoomMessage triggerMessage, CancellationToken cancellationToken)
        {
            var pendingRoles = new Queue<ChatRoomRole>();
            var attemptedRoleIds = new HashSet<string>();
            bool managersInvoked = false;
            bool canInvokeManagers = triggerMessage.IsHumanMessage || triggerMessage.MentionedRoleIds.Count > 0;
            int maxAutoLoopSteps = Math.Max(100, _manager.Roles.Count(r => !r.Definition.IsHuman));
            int stepCount = 0;

            EnqueueInitialRoles(triggerMessage, pendingRoles, attemptedRoleIds);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Interlocked.Exchange(ref _humanInterjectSignal, 0) != 0)
                {
                    return true;
                }

                if (pendingRoles.Count == 0)
                {
                    if (managersInvoked || !canInvokeManagers)
                    {
                        return false;
                    }

                    managersInvoked = true;
                    EnqueueManagerRoles(pendingRoles, attemptedRoleIds);
                    if (pendingRoles.Count == 0)
                    {
                        return false;
                    }
                }

                stepCount++;
                if (stepCount > maxAutoLoopSteps)
                {
                    return false;
                }

                ChatRoomRole nextSpeaker = pendingRoles.Dequeue();
                if (nextSpeaker.Definition.IsHuman || !attemptedRoleIds.Add(nextSpeaker.Definition.RoleId))
                {
                    continue;
                }

                ChatRoomMessage? message = await StepAsync(nextSpeaker, cancellationToken).ConfigureAwait(false);
                if (message is null)
                {
                    continue;
                }

                await HandleAutoLoopMessageAsync(message, pendingRoles, attemptedRoleIds).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<ChatRoomMessage?> StepAsync(ChatRoomRole role,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(role);

            if (role.Definition.IsHuman)
            {
                // 人类角色不通过 StepAsync 发言
                return null;
            }

            _manager.CurrentSpeaker = role;

            try
            {
                // 注入聊天室上下文，供角色首次发言时构建系统提示词
                role.ChatRoomContext = BuildChatRoomContext();

                // 构建增量消息文本列表：自该角色上次发言之后的公开消息
                IReadOnlyList<string> incrementalUserMessages = BuildIncrementalUserMessages(role);
                if (incrementalUserMessages.Count == 0)
                {
                    // 没有新的用户消息，角色无需发言
                    return null;
                }

                // 追加角色管理工具和工作区路径审批工具到本次发言
                List<AITool> additionalTools =
                [
                    .. ChatRoomRoleManagementTools.CreateTools(_manager),
                    .. WorkspacePathTools.CreateSetWorkspacePathTool(_manager),
                ];

                // 调用角色发言，获取包含流式 CopilotChatMessage 的结果
                ChatRoomSpeakResult? speakResult = await role
                    .SpeakAsync
                    (
                        incrementalUserMessages,
                        additionalTools,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                if (speakResult is null)
                {
                    return null;
                }

                // 创建流式消息并立即追加到 Messages 集合，UI 通过绑定感知实时更新
                var streamingMessage = new ChatRoomMessage
                {
                    SenderRoleId = role.Definition.RoleId,
                    SenderRoleName = role.Definition.RoleName,
                    CopilotChatMessage = speakResult.AssistantChatMessage,
                    ModelDisplayName = speakResult.ModelDisplayName,
                    IsStreaming = true,
                };
                await _manager.Session.AddMessageAsync(streamingMessage).ConfigureAwait(false);

                // 等待发言完成，获取最终文本
                string? assistantContent = await speakResult.FinalContentTask.ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(assistantContent))
                {
                    // 空回复，从 Messages 移除流式消息
                    _manager.Session.Messages.Remove(streamingMessage);
                    return null;
                }

                // 发言完成，标记流式结束（CopilotChatMessage 引用不变，Content 已流式更新到位）
                streamingMessage.IsStreaming = false;
                // 将最终内容回写到 StaticContent，确保持久化序列化时消息内容不丢失
                streamingMessage.StaticContent = assistantContent;
                return streamingMessage;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                // 角色发言失败，引发事件
                _manager.OnRoleSpeakFailed?.Invoke(_manager, new RoleSpeakFailedEventArgs(role, ex));

                // 返回系统消息
                return ChatRoomMessage.CreateSystem($"角色 「{role.Definition.RoleName}」发言失败：{ex.Message}");
            }
            finally
            {
                _manager.CurrentSpeaker = null;
            }
        }

        public void NotifyHumanInterjected()
        {
            if (_manager.IsRunning)
            {
                Interlocked.Exchange(ref _humanInterjectSignal, 1);
            }
        }

        private IReadOnlyList<string> BuildIncrementalUserMessages(ChatRoomRole role)
        {
            var list = new List<string>();

            // 获取自该角色上次发言后的增量消息
            IReadOnlyList<ChatRoomMessage> incrementalMessages = _manager.Session.GetMessagesSinceLastSpeak(role.Definition.RoleId);

            if (incrementalMessages.Count == 0)
            {
                return list;
            }

            foreach (ChatRoomMessage message in incrementalMessages)
            {
                // 跳过系统消息
                if (message.IsSystemMessage)
                {
                    continue;
                }

                // 跳过自身的消息
                if (!string.IsNullOrEmpty(message.SenderRoleId) && message.SenderRoleId == role.Definition.RoleId)
                {
                    continue;
                }

                string prefix;
                if (message.IsHumanMessage)
                {
                    prefix = "用户";
                }
                else
                {
                    prefix = string.IsNullOrEmpty(message.SenderRoleName) ? "另一位参与者" : message.SenderRoleName;
                }

                string content = $"{prefix}说：{message.Content}";
                list.Add(content);
            }

            return list;
        }

        /// <summary>
        /// 构建聊天室上下文提示词，描述当前角色列表、@用法和协作规范。
        /// 注入到角色的系统提示词中，引导角色优先协调而非自己执行。
        /// </summary>
        private string BuildChatRoomContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 聊天室协作指引 ===");

            // 角色列表
            if (_manager.Roles.Count > 0)
            {
                sb.AppendLine("当前聊天室的角色：");
                foreach (ChatRoomRole r in _manager.Roles)
                {
                    string roleInfo = $"- {r.Definition.RoleName}";
                    if (r.Definition.IsHuman)
                    {
                        roleInfo += "（人类）";
                    }
                    else if (r.Definition.IsManagerRole)
                    {
                        roleInfo += "（管理者）";
                    }
                    else if (r.Definition.ParticipationMode == ChatRoomParticipationMode.MentionOnly)
                    {
                        roleInfo += "（仅被@时参与）";
                    }
                    sb.AppendLine(roleInfo);
                }
                sb.AppendLine();
            }

            sb.AppendLine("@机制：");
            sb.AppendLine("- 在消息中使用 @【角色名】 或 @角色名 可以指定某角色接下来回复");
            sb.AppendLine("- @角色名 后面必须加一个空格，禁止紧跟任何标点符号（如 ，。：；！等），否则无法正确识别");
            sb.AppendLine("- 被 @ 的角色会在当前发言者之后自动发言");
            sb.AppendLine("- 如果只是提及某个角色而非要求其发言，请勿使用 @ 前缀，否则会导致该角色被触发发言");
            sb.AppendLine();
            sb.AppendLine("协作原则：");
            sb.AppendLine("- 优先通过 @ 其他角色让他们完成各自擅长的工作，而非自己包揽");
            sb.AppendLine("- 如果缺少合适的专业角色，可使用 create_character 工具创建新角色");
            sb.AppendLine("- 创建新角色后，通过 @ 该角色来分配任务，并附上需要它做的事情");

            return sb.ToString().TrimEnd();
        }

        private ChatRoomMessage? GetLatestTriggerMessage()
        {
            for (int i = _manager.Session.Messages.Count - 1; i >= 0; i--)
            {
                ChatRoomMessage message = _manager.Session.Messages[i];
                if (!message.IsSystemMessage)
                {
                    return message;
                }
            }

            return null;
        }

        private void EnqueueInitialRoles(
            ChatRoomMessage triggerMessage,
            Queue<ChatRoomRole> pendingRoles,
            HashSet<string> attemptedRoleIds)
        {
            if (!triggerMessage.IsSystemMessage)
            {
                EnqueueMentionedRoles(pendingRoles, triggerMessage.MentionedRoleIds, attemptedRoleIds);
            }

            if (pendingRoles.Count > 0)
            {
                return;
            }

            if (triggerMessage.IsHumanMessage)
            {
                foreach (ChatRoomRole role in GetDefaultWorkerRoles())
                {
                    pendingRoles.Enqueue(role);
                }
            }
        }

        private async Task HandleAutoLoopMessageAsync(
            ChatRoomMessage message,
            Queue<ChatRoomRole> pendingRoles,
            HashSet<string> attemptedRoleIds)
        {
            IReadOnlyList<string> mentionedRoleIds = MentionParser.ParseMentions(message.Content, _manager.Roles);
            if (mentionedRoleIds.Count > 0)
            {
                message.MentionedRoleIds = mentionedRoleIds;
            }

            // 系统消息等非流式消息不在 Messages 集合中，需要追加。
            if (!_manager.Session.Messages.Contains(message))
            {
                await _manager.AppendMessageAsync(message).ConfigureAwait(false);
            }
            else if (_manager.Persistence is not null)
            {
                // 流式消息已在 Messages 集合中，UI 通过 CollectionChanged 感知；无需重复触发 OnMessageAdded，仅持久化。
                _ = _manager.Persistence.SavePublicMessageAsync(_manager.Session.SessionId, message)
                    .ContinueWith(static t =>
                    {
                        if (t.IsFaulted && t.Exception is not null)
                        {
                            Debug.Fail($"持久化公开消息失败: {t.Exception.InnerException?.Message}");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }

            EnqueueMentionedRoles(pendingRoles, mentionedRoleIds, attemptedRoleIds);
        }

        private void EnqueueMentionedRoles(
            Queue<ChatRoomRole> pendingRoles,
            IReadOnlyList<string> mentionedRoleIds,
            HashSet<string> attemptedRoleIds)
        {
            if (mentionedRoleIds.Count == 0)
            {
                return;
            }

            var queuedRoleIds = new HashSet<string>(pendingRoles.Select(r => r.Definition.RoleId));
            foreach (string roleId in mentionedRoleIds)
            {
                if (string.IsNullOrWhiteSpace(roleId) || attemptedRoleIds.Contains(roleId) || !queuedRoleIds.Add(roleId))
                {
                    continue;
                }

                ChatRoomRole? role = _manager.Roles.FirstOrDefault(r => r.Definition.RoleId == roleId && !r.Definition.IsHuman);
                if (role is not null)
                {
                    pendingRoles.Enqueue(role);
                }
            }
        }

        private void EnqueueManagerRoles(Queue<ChatRoomRole> pendingRoles, HashSet<string> attemptedRoleIds)
        {
            foreach (ChatRoomRole role in GetManagerRoles())
            {
                if (!attemptedRoleIds.Contains(role.Definition.RoleId))
                {
                    pendingRoles.Enqueue(role);
                }
            }
        }

        private IEnumerable<ChatRoomRole> GetDefaultWorkerRoles()
        {
            return _manager.Roles.Where(r =>
                !r.Definition.IsHuman &&
                !r.Definition.IsManagerRole &&
                r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate);
        }

        private IEnumerable<ChatRoomRole> GetManagerRoles()
        {
            return _manager.Roles.Where(r => !r.Definition.IsHuman && r.Definition.IsManagerRole);
        }
    }
}
