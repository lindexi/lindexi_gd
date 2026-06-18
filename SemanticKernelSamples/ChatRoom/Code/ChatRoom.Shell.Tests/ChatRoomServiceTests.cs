using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatRoom.Shell.Tests;

/// <summary>
/// <see cref="ChatRoomService"/> 的单元测试。
/// </summary>
[TestClass]
public class ChatRoomServiceTests
{
    private string _tempDir = string.Empty;
    private TestMainThreadDispatcher _dispatcher = null!;
    private ModelProviderService _modelProviderService = null!;
    private ChatRoomService _chatRoomService = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ChatRoomServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _dispatcher = new TestMainThreadDispatcher();
        var settings = new AppSettings
        {
            PersistencePath = _tempDir,
            DefaultMaxRounds = 5,
        };
        _modelProviderService = new ModelProviderService(settings);
        _chatRoomService = new ChatRoomService(
            _dispatcher,
            _modelProviderService,
            _tempDir,
            5);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _chatRoomService.CloseCurrentSession();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    /// <summary>
    /// 创建新会话后应有一个活跃的 ChatRoomManager。
    /// </summary>
    [TestMethod]
    public async Task CreateNewSession_ShouldSetActiveManager()
    {
        Assert.IsFalse(_chatRoomService.HasActiveSession);

        ChatRoomManager manager = await _chatRoomService.CreateNewSessionAsync("测试会话");

        Assert.IsNotNull(manager);
        Assert.IsTrue(_chatRoomService.HasActiveSession);
        Assert.AreSame(manager, _chatRoomService.CurrentManager);
        Assert.AreEqual("测试会话", manager.Session.Title);
    }

    /// <summary>
    /// 添加角色后 Roles 集合应包含该角色。
    /// </summary>
    [TestMethod]
    public async Task AddRole_ShouldAddToRolesCollection()
    {
        await _chatRoomService.CreateNewSessionAsync();

        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "test-role",
            RoleName = "测试角色",
            SystemPrompt = "你是一个测试角色。",
        };

        await _chatRoomService.AddRoleAsync(definition);

        Assert.AreEqual(1, _chatRoomService.CurrentManager!.Roles.Count);
        Assert.AreEqual("test-role", _chatRoomService.CurrentManager.Roles[0].Definition.RoleId);
    }

    /// <summary>
    /// 移除角色后 Roles 集合不应包含该角色。
    /// </summary>
    [TestMethod]
    public async Task RemoveRole_ShouldRemoveFromRolesCollection()
    {
        await _chatRoomService.CreateNewSessionAsync();

        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "removable-role",
            RoleName = "可移除角色",
        };
        await _chatRoomService.AddRoleAsync(definition);

        Assert.AreEqual(1, _chatRoomService.CurrentManager!.Roles.Count);

        _chatRoomService.RemoveRole("removable-role");

        Assert.AreEqual(0, _chatRoomService.CurrentManager.Roles.Count);
    }

    /// <summary>
    /// 关闭会话后 HasActiveSession 应为 false。
    /// </summary>
    [TestMethod]
    public async Task CloseCurrentSession_ShouldClearActiveSession()
    {
        await _chatRoomService.CreateNewSessionAsync();
        Assert.IsTrue(_chatRoomService.HasActiveSession);

        _chatRoomService.CloseCurrentSession();

        Assert.IsFalse(_chatRoomService.HasActiveSession);
        Assert.IsNull(_chatRoomService.CurrentManager);
    }

    /// <summary>
    /// 没有活跃会话时调用 HumanInterjectAsync 应抛出异常。
    /// </summary>
    [TestMethod]
    public async Task HumanInterject_WithoutActiveSession_ShouldThrow()
    {
        InvalidOperationException? thrown = null;
        try
        {
            await _chatRoomService.HumanInterjectAsync("test", "human", "我");
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        Assert.IsNotNull(thrown);
    }

    /// <summary>
    /// 人类插话后消息应出现在会话的 Messages 中。
    /// </summary>
    [TestMethod]
    public async Task HumanInterject_ShouldAddMessageToSession()
    {
        await _chatRoomService.CreateNewSessionAsync();

        await _chatRoomService.HumanInterjectAsync("你好", "human", "我");

        Assert.AreEqual(1, _chatRoomService.CurrentManager!.Session.Messages.Count);
        Assert.AreEqual("你好", _chatRoomService.CurrentManager.Session.Messages[0].Content);
        Assert.IsTrue(_chatRoomService.CurrentManager.Session.Messages[0].IsHumanMessage);
    }

    /// <summary>
    /// SessionChanged 事件应在创建和关闭会话时触发。
    /// </summary>
    [TestMethod]
    public async Task SessionChanged_ShouldFireOnCreateAndClose()
    {
        int eventCount = 0;
        _chatRoomService.SessionChanged += (_, _) => eventCount++;

        await _chatRoomService.CreateNewSessionAsync();
        Assert.AreEqual(1, eventCount);

        _chatRoomService.CloseCurrentSession();
        Assert.AreEqual(2, eventCount);
    }

    /// <summary>
    /// MessageAdded 事件应在人类插话时触发。
    /// </summary>
    [TestMethod]
    public async Task MessageAdded_ShouldFireOnHumanInterject()
    {
        await _chatRoomService.CreateNewSessionAsync();

        ChatRoomMessage? receivedMessage = null;
        _chatRoomService.MessageAdded += (_, msg) => receivedMessage = msg;

        await _chatRoomService.HumanInterjectAsync("测试消息", "human", "我");

        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("测试消息", receivedMessage.Content);
    }

    /// <summary>
    /// 保存后应能通过 SessionService 加载会话列表。
    /// </summary>
    [TestMethod]
    public async Task Save_ThenListSessions_ShouldFindSession()
    {
        ChatRoomManager manager = await _chatRoomService.CreateNewSessionAsync("持久化测试");
        await _chatRoomService.AddRoleAsync(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "角色1",
        });
        await _chatRoomService.SaveAsync();

        var sessionService = new SessionService(new ChatRoomPersistence(_tempDir));
        var sessions = sessionService.ListSessions();

        Assert.IsTrue(sessions.Count >= 1);
        Assert.IsTrue(sessions.Any(s => s.Title == "持久化测试"));
    }
}
