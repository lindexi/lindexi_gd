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
        private readonly object _stateSync = new();

        private const int MaxSpeakCountPerRole = 5;
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(2);

        private CancellationTokenSource? _autoLoopCancellationTokenSource;
        private Task? _runningTask;

        /// <summary>
        /// 人类插话信号。当自动循环运行期间用户插话时设置为 1，
        /// 促使自动循环在当前角色发言完成后重启循环，
        /// 使选择器检测到人类消息并让助手立即回话用户。
        /// 使用 int + Interlocked 以保证原子消费，避免多次插话时的竞态。
        /// </summary>
        private int _humanInterjectSignal;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateSync)
            {
                if (_runningTask is { IsCompleted: false })
                {
                    return _runningTask;
                }

                _autoLoopCancellationTokenSource?.Dispose();
                _autoLoopCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _manager.IsRunning = true;
                _runningTask = RunWithCleanupAsync(
                    _autoLoopCancellationTokenSource,
                    _autoLoopCancellationTokenSource.Token);
                return _runningTask;
            }
        }

        private async Task RunWithCleanupAsync(
            CancellationTokenSource cancellationTokenSource,
            CancellationToken cancellationToken)
        {
            try
            {
                await RunCoreAsync(cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 正常取消
            }
            finally
            {
                lock (_stateSync)
                {
                    _manager.IsRunning = false;
                    _manager.CurrentSpeaker = null;
                }

                try
                {
                    // 自动循环结束后持久化会话（AI 发言产生的新消息已通过 AppendMessageAsync 持久化，
                    // 此处兜底确保角色定义等状态变更被保存）
                    if (Volatile.Read(ref _manager._isClosingOrClosed) == 0)
                    {
                        await _manager.SaveAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    lock (_stateSync)
                    {
                        if (ReferenceEquals(_autoLoopCancellationTokenSource, cancellationTokenSource))
                        {
                            _autoLoopCancellationTokenSource.Dispose();
                            _autoLoopCancellationTokenSource = null;
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            lock (_stateSync)
            {
                _autoLoopCancellationTokenSource?.Cancel();
            }
        }

        public async Task StopAsync()
        {
            (Task? runningTask, CancellationTokenSource? cancellationTokenSource) = StopAndGetRunningTask();
            if (runningTask is null)
            {
                return;
            }

            try
            {
                await runningTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested == true)
            {
                // 主动停止产生的正常取消。
            }
        }

        public async Task<bool> TryStopAsync()
        {
            (Task? runningTask, CancellationTokenSource? cancellationTokenSource) = StopAndGetRunningTask();
            if (runningTask is null)
            {
                return true;
            }

            Task completedTask = await Task.WhenAny(runningTask, Task.Delay(StopTimeout)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, runningTask))
            {
                return false;
            }

            try
            {
                await runningTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested == true)
            {
                // 主动停止产生的正常取消。
            }

            return true;
        }

        private (Task? RunningTask, CancellationTokenSource? CancellationTokenSource) StopAndGetRunningTask()
        {
            lock (_stateSync)
            {
                CancellationTokenSource? cancellationTokenSource = _autoLoopCancellationTokenSource;
                cancellationTokenSource?.Cancel();
                return (_runningTask, cancellationTokenSource);
            }
        }

        public Task? GetRunningTask()
        {
            lock (_stateSync)
            {
                return _runningTask;
            }
        }

        private async Task RunCoreAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ChatRoomMessage? triggerMessage = GetLatestTriggerMessage();
                if (triggerMessage is null)
                {
                    break;
                }

                bool shouldRestart = await RunAutoLoopTurnAsync(triggerMessage, cancellationToken)
                    .ConfigureAwait(true);
                if (!shouldRestart)
                {
                    break;
                }
            }
        }

        private async Task<bool> RunAutoLoopTurnAsync(ChatRoomMessage triggerMessage, CancellationToken cancellationToken)
        {
            var priorityRoles = new Stack<ChatRoomRole>();
            var defaultRoles = new Queue<ChatRoomRole>();
            var speakCounts = new Dictionary<string, int>(_manager.Roles.Count);
            string? lastSpeakerRoleId = null;
            bool managersInvokedForIdle = false;
            bool lastMessageMentionedLastSpeaker = false;
            int maxAutoLoopSteps = Math.Max(100, _manager.Roles.Count(r => !r.Definition.IsHuman));
            int stepCount = 0;

            EnqueueInitialRoles(triggerMessage, priorityRoles, defaultRoles);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Interlocked.Exchange(ref _humanInterjectSignal, 0) != 0)
                {
                    return true;
                }

                stepCount++;
                if (stepCount > maxAutoLoopSteps)
                {
                    return false;
                }

                ChatRoomRole? nextSpeaker = TryDequeueNextSpeaker(priorityRoles, defaultRoles, lastSpeakerRoleId, out bool hasPostponedRole);
                bool clearPostponedRolesAfterManager = false;
                string? postponedRoleIdToClear = null;
                IReadOnlyList<string>? additionalUserMessages = null;

                if (nextSpeaker is null)
                {
                    if (managersInvokedForIdle && !hasPostponedRole)
                    {
                        return false;
                    }

                    nextSpeaker = GetManagerRole();
                    if (nextSpeaker is null || nextSpeaker.Definition.RoleId == lastSpeakerRoleId)
                    {
                        return false;
                    }

                    managersInvokedForIdle = true;
                    clearPostponedRolesAfterManager = hasPostponedRole && lastMessageMentionedLastSpeaker;
                    postponedRoleIdToClear = clearPostponedRolesAfterManager ? lastSpeakerRoleId : null;
                }

                string nextSpeakerRoleId = nextSpeaker.Definition.RoleId;
                if (speakCounts.TryGetValue(nextSpeakerRoleId, out int speakCount) && speakCount >= MaxSpeakCountPerRole)
                {
                    ChatRoomRole? managerRole = GetManagerRole();
                    if (managerRole is null || managerRole.Definition.RoleId == lastSpeakerRoleId)
                    {
                        return false;
                    }

                    speakCounts[nextSpeakerRoleId] = 0;
                    additionalUserMessages = [BuildMaxSpeakCountReachedMessage(nextSpeaker, priorityRoles, defaultRoles)];
                    nextSpeaker = managerRole;
                    nextSpeakerRoleId = nextSpeaker.Definition.RoleId;
                }

                if (nextSpeaker.Definition.IsHuman || nextSpeakerRoleId == lastSpeakerRoleId)
                {
                    continue;
                }

                ChatRoomMessage? message = await StepAsync(nextSpeaker, additionalUserMessages, cancellationToken).ConfigureAwait(false);
                if (message is null)
                {
                    continue;
                }

                lastSpeakerRoleId = nextSpeakerRoleId;
                speakCounts[nextSpeakerRoleId] = speakCounts.GetValueOrDefault(nextSpeakerRoleId) + 1;

                IReadOnlyList<string> mentionedRoleIds = await HandleAutoLoopMessageAsync(message).ConfigureAwait(false);
                lastMessageMentionedLastSpeaker = mentionedRoleIds.Contains(nextSpeakerRoleId, StringComparer.Ordinal);
                if (clearPostponedRolesAfterManager && postponedRoleIdToClear is not null)
                {
                    RemoveQueuedRoles(priorityRoles, defaultRoles, postponedRoleIdToClear);
                }
                bool shouldAllowNextIdleManager = nextSpeaker.Definition.IsManagerRole &&
                    mentionedRoleIds.Any(roleId => speakCounts.GetValueOrDefault(roleId) > 0);
                PushMentionedRoles(priorityRoles, mentionedRoleIds);
                if (shouldAllowNextIdleManager)
                {
                    managersInvokedForIdle = false;
                }
            }

            return false;
        }

        public async Task<ChatRoomMessage?> StepAsync(ChatRoomRole role,
            CancellationToken cancellationToken = default)
        {
            ChatRoomMessage? message = await StepAsync(
                role,
                additionalUserMessages: null,
                cancellationToken).ConfigureAwait(false);
            if (message is not null)
            {
                await HandleAutoLoopMessageAsync(message).ConfigureAwait(false);
            }

            return message;
        }

        private async Task<ChatRoomMessage?> StepAsync(ChatRoomRole role,
            IReadOnlyList<string>? additionalUserMessages,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(role);

            if (role.Definition.IsHuman)
            {
                // 人类角色不通过 StepAsync 发言
                return null;
            }

            _manager.CurrentSpeaker = role;
            ChatRoomMessage? streamingMessage = null;

            try
            {
                // 注入聊天室上下文，供角色首次发言时构建系统提示词
                role.ChatRoomContext = BuildChatRoomContext();

                // 构建增量消息文本列表：自该角色上次发言之后的公开消息
                IReadOnlyList<string> incrementalUserMessages = BuildIncrementalUserMessages(role, additionalUserMessages);
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
                streamingMessage = new ChatRoomMessage
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
                    await _manager.Session.RemoveMessageAsync(streamingMessage).ConfigureAwait(false);
                    streamingMessage = null;
                    return null;
                }

                // 发言完成，标记流式结束（CopilotChatMessage 引用不变，Content 已流式更新到位）
                streamingMessage.IsStreaming = false;
                // 将最终内容回写到 StaticContent，确保持久化序列化时消息内容不丢失
                streamingMessage.StaticContent = assistantContent;
                ChatRoomMessage completedMessage = streamingMessage;
                streamingMessage = null;
                await _manager.SaveRoleAgentSessionStateAsync(role, cancellationToken).ConfigureAwait(false);
                return completedMessage;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (streamingMessage is { IsStreaming: true })
                {
                    await _manager.Session.RemoveMessageAsync(streamingMessage).ConfigureAwait(false);
                }

                return null;
            }
            catch (Exception ex)
            {
                if (streamingMessage is { IsStreaming: true })
                {
                    await _manager.Session.RemoveMessageAsync(streamingMessage).ConfigureAwait(false);
                }

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

        private IReadOnlyList<string> BuildIncrementalUserMessages(
            ChatRoomRole role,
            IReadOnlyList<string>? additionalUserMessages = null)
        {
            int additionalMessageCount = additionalUserMessages?.Count ?? 0;
            var list = new List<string>(additionalMessageCount);
            bool omitHumanPrefix = _manager.Roles.Count(r => !r.Definition.IsHuman) == 1;

            // 获取自该角色上次发言后的增量消息
            IReadOnlyList<ChatRoomMessage> incrementalMessages = _manager.Session.GetMessagesSinceLastSpeak(role.Definition.RoleId);

            if (incrementalMessages.Count == 0 && additionalMessageCount == 0)
            {
                return list;
            }

            if (incrementalMessages.Count > 0)
            {
                list.EnsureCapacity(incrementalMessages.Count + additionalMessageCount);
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

                string content = ChatRoomIncrementalMessageFormatter.Format(
                    message.Content,
                    message.IsHumanMessage,
                    message.SenderRoleName,
                    omitHumanPrefix);
                list.Add(content);
            }

            if (additionalUserMessages is not null)
            {
                foreach (string additionalUserMessage in additionalUserMessages)
                {
                    if (!string.IsNullOrWhiteSpace(additionalUserMessage))
                    {
                        list.Add(additionalUserMessage);
                    }
                }
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

            sb.AppendLine("消息角色规则：");
            sb.AppendLine("- 聊天室采用相对视角：对当前正在思考和回复的角色而言，只有你自己在 LLM 对话历史中是 Assistant 角色");
            sb.AppendLine("- 除你自己之外，聊天室内所有人类和非人类角色在 LLM 对话历史中都会作为 User 角色输入");
            sb.AppendLine("- 其他不同角色之间的身份差异不会通过 ChatRole 区分，而是通过消息文本说明，例如“某角色说：具体内容”");
            sb.AppendLine("- 当你看到“用户说：...”或“角色名说：...”时，应理解为对应的人类或非人类角色发表了该内容，而不是你自己的发言");
            sb.AppendLine();

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
            Stack<ChatRoomRole> priorityRoles,
            Queue<ChatRoomRole> defaultRoles)
        {
            IReadOnlyList<string> mentionedRoleIds = GetMentionedRoleIds(triggerMessage);
            if (mentionedRoleIds.Count > 0)
            {
                PushMentionedRoles(priorityRoles, mentionedRoleIds);
                return;
            }

            if (triggerMessage.IsHumanMessage)
            {
                foreach (ChatRoomRole role in _manager.Roles.Where(r =>
                             !r.Definition.IsHuman &&
                             r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate))
                {
                    defaultRoles.Enqueue(role);
                }
            }
        }

        private async Task<IReadOnlyList<string>> HandleAutoLoopMessageAsync(ChatRoomMessage message)
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
                await _manager.Persistence
                    .SavePublicMessageAsync(_manager.CurrentPersistenceSessionId, message)
                    .ConfigureAwait(false);
            }

            return mentionedRoleIds;
        }

        private IReadOnlyList<string> GetMentionedRoleIds(ChatRoomMessage message)
        {
            if (message.IsSystemMessage)
            {
                return [];
            }

            if (message.MentionedRoleIds.Count > 0)
            {
                return message.MentionedRoleIds;
            }

            return MentionParser.ParseMentions(message.Content, _manager.Roles);
        }

        private void PushMentionedRoles(Stack<ChatRoomRole> priorityRoles, IReadOnlyList<string> mentionedRoleIds)
        {
            if (mentionedRoleIds.Count == 0)
            {
                return;
            }

            var pushedRoleIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = mentionedRoleIds.Count - 1; i >= 0; i--)
            {
                string roleId = mentionedRoleIds[i];
                if (string.IsNullOrWhiteSpace(roleId) || !pushedRoleIds.Add(roleId))
                {
                    continue;
                }

                ChatRoomRole? role = _manager.Roles.FirstOrDefault(r =>
                    r.Definition.RoleId == roleId &&
                    !r.Definition.IsHuman);
                if (role is not null)
                {
                    RemoveQueuedRole(priorityRoles, roleId);
                    priorityRoles.Push(role);
                }
            }
        }

        private static void RemoveQueuedRole(Stack<ChatRoomRole> priorityRoles, string roleId)
        {
            ChatRoomRole[] remainingRoles = priorityRoles
                .Where(role => role.Definition.RoleId != roleId)
                .Reverse()
                .ToArray();
            priorityRoles.Clear();
            foreach (ChatRoomRole role in remainingRoles)
            {
                priorityRoles.Push(role);
            }
        }

        private static ChatRoomRole? TryDequeueNextSpeaker(
            Stack<ChatRoomRole> priorityRoles,
            Queue<ChatRoomRole> defaultRoles,
            string? lastSpeakerRoleId,
            out bool hasPostponedRole)
        {
            hasPostponedRole = false;
            var postponedPriorityRoles = new List<ChatRoomRole>();
            while (priorityRoles.Count > 0)
            {
                ChatRoomRole role = priorityRoles.Pop();
                if (role.Definition.IsHuman)
                {
                    continue;
                }

                if (role.Definition.RoleId == lastSpeakerRoleId)
                {
                    hasPostponedRole = true;
                    postponedPriorityRoles.Add(role);
                    continue;
                }

                RestorePriorityRoles(priorityRoles, postponedPriorityRoles);
                return role;
            }

            int defaultRoleCount = defaultRoles.Count;
            var postponedDefaultRoles = new List<ChatRoomRole>();
            for (int i = 0; i < defaultRoleCount; i++)
            {
                ChatRoomRole role = defaultRoles.Dequeue();
                if (role.Definition.IsHuman)
                {
                    continue;
                }

                if (role.Definition.RoleId == lastSpeakerRoleId)
                {
                    hasPostponedRole = true;
                    postponedDefaultRoles.Add(role);
                    continue;
                }

                RestorePriorityRoles(priorityRoles, postponedPriorityRoles);
                foreach (ChatRoomRole postponedRole in postponedDefaultRoles)
                {
                    defaultRoles.Enqueue(postponedRole);
                }

                return role;
            }

            RestorePriorityRoles(priorityRoles, postponedPriorityRoles);
            foreach (ChatRoomRole postponedRole in postponedDefaultRoles)
            {
                defaultRoles.Enqueue(postponedRole);
            }

            return null;
        }

        private static void RestorePriorityRoles(Stack<ChatRoomRole> priorityRoles, List<ChatRoomRole> postponedPriorityRoles)
        {
            for (int i = postponedPriorityRoles.Count - 1; i >= 0; i--)
            {
                priorityRoles.Push(postponedPriorityRoles[i]);
            }
        }

        private static void RemoveQueuedRoles(
            Stack<ChatRoomRole> priorityRoles,
            Queue<ChatRoomRole> defaultRoles,
            string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return;
            }

            ChatRoomRole[] remainingPriorityRoles = priorityRoles
                .Where(role => role.Definition.RoleId != roleId)
                .Reverse()
                .ToArray();
            priorityRoles.Clear();
            foreach (ChatRoomRole role in remainingPriorityRoles)
            {
                priorityRoles.Push(role);
            }

            int defaultRoleCount = defaultRoles.Count;
            for (int i = 0; i < defaultRoleCount; i++)
            {
                ChatRoomRole role = defaultRoles.Dequeue();
                if (role.Definition.RoleId != roleId)
                {
                    defaultRoles.Enqueue(role);
                }
            }
        }

        private static string BuildMaxSpeakCountReachedMessage(
            ChatRoomRole limitedRole,
            Stack<ChatRoomRole> priorityRoles,
            Queue<ChatRoomRole> defaultRoles)
        {
            string waitingRoleNames = BuildWaitingRoleNames(priorityRoles, defaultRoles);
            return $"角色「{limitedRole.Definition.RoleName}」在当前自动循环中已经达到最大发言次数 {MaxSpeakCountPerRole} 次，因此本次未继续让它发言。\n" +
                $"如果你认为「{limitedRole.Definition.RoleName}」仍然需要继续发言，请在回复中明确 @{limitedRole.Definition.RoleName}；否则可以不再 @ 它。\n" +
                $"当前仍在等待发言的角色包括：{waitingRoleNames}。";
        }

        private static string BuildWaitingRoleNames(Stack<ChatRoomRole> priorityRoles, Queue<ChatRoomRole> defaultRoles)
        {
            var roleNames = new List<string>(priorityRoles.Count + defaultRoles.Count);
            var roleIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (ChatRoomRole role in priorityRoles.Reverse())
            {
                if (!role.Definition.IsHuman && roleIds.Add(role.Definition.RoleId))
                {
                    roleNames.Add(role.Definition.RoleName);
                }
            }

            foreach (ChatRoomRole role in defaultRoles)
            {
                if (!role.Definition.IsHuman && roleIds.Add(role.Definition.RoleId))
                {
                    roleNames.Add(role.Definition.RoleName);
                }
            }

            return roleNames.Count == 0 ? "无" : string.Join('、', roleNames);
        }

        private ChatRoomRole? GetManagerRole()
        {
            return _manager.Roles.FirstOrDefault(r => !r.Definition.IsHuman && r.Definition.IsManagerRole);
        }
    }
}
