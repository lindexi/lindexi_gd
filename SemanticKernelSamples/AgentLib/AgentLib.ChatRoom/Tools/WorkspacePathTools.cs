using AgentLib.Tools;

using Microsoft.Extensions.AI;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom.Tools;

/// <summary>
/// 提供设置工作区路径的审批工具。
/// AI 调用 <c>set_workspace_path</c> 后，需用户审批同意才会实际生效。
/// </summary>
public static class WorkspacePathTools
{
    /// <summary>
    /// 创建设置工作区路径的审批工具。
    /// 该工具被 <see cref="HumanApprovalTool.Wrap"/> 包装，调用时会在聊天界面弹出审批面板，
    /// 用户同意后才会执行路径设置。
    /// </summary>
    /// <param name="chatRoomManager">聊天室管理器。</param>
    /// <returns>包含审批包装的 <c>set_workspace_path</c> 工具。</returns>
    public static IReadOnlyList<AITool> CreateSetWorkspacePathTool(ChatRoomManager chatRoomManager)
    {
        ArgumentNullException.ThrowIfNull(chatRoomManager);

        AITool tool = AIFunctionFactory.Create(
            (string path) => SetWorkspacePath(chatRoomManager, path),
            name: "set_workspace_path",
            description: "设置工作区路径，启用文件系统工具（读写文件、搜索文件等）。调用后需用户审批同意才会生效。" +
                "参数：path（必填，工作区根目录的绝对路径）。");

        return [HumanApprovalTool.Wrap(tool, "AI 请求设置工作区路径，请确认路径是否安全。")];
    }

    private static Task<string> SetWorkspacePath(ChatRoomManager chatRoomManager, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult("错误：工作区路径不能为空。");
        }

        string trimmedPath = path.Trim();

        try
        {
            if (!Directory.Exists(trimmedPath))
            {
                return Task.FromResult($"错误：目录不存在: {trimmedPath}");
            }

            chatRoomManager.SetWorkspacePath(trimmedPath);
            return Task.FromResult($"✅ 工作区路径已设置为: {trimmedPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"设置工作区路径失败: {ex.Message}");
        }
    }
}
