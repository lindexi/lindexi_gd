using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Text.Json;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomPersistenceTests
{
    private string _tempBaseFolder = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempBaseFolder = Path.Join(Path.GetTempPath(), "ChatRoomPersistenceTests", Path.GetRandomFileName());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempBaseFolder))
        {
            Directory.Delete(_tempBaseFolder, recursive: true);
        }
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_NullBaseFolder_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new ChatRoomPersistence(null!));
    }

    [TestMethod]
    public void Constructor_ValidBaseFolder_CreatesDirectory()
    {
        // Act
        _ = new ChatRoomPersistence(_tempBaseFolder);

        // Assert
        Assert.IsTrue(Directory.Exists(_tempBaseFolder), "Base folder should be created.");
    }

    #endregion

    #region SaveConfigAsync Tests

    [TestMethod]
    public async Task SaveConfigAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => persistence.SaveConfigAsync(null!));
    }

    [TestMethod]
    public async Task SaveConfigAsync_ValidData_SavesConfigFile()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var data = new ChatRoomSessionData
        {
            SessionId = sessionId,
            Title = "测试聊天室",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Act
        await persistence.SaveConfigAsync(data);

        // Assert
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId.ToString("N"));
        string configPath = Path.Join(sessionFolder, "room.config.json");
        Assert.IsTrue(File.Exists(configPath), "Config file should be created.");

        string json = await File.ReadAllTextAsync(configPath);
        ChatRoomSessionData? loaded = JsonSerializer.Deserialize<ChatRoomSessionData>(json);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(sessionId, loaded.SessionId);
        Assert.AreEqual("测试聊天室", loaded.Title);
    }

    [TestMethod]
    public async Task SaveConfigAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            Title = "测试",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => persistence.SaveConfigAsync(data, cts.Token));
    }

    #endregion

    #region LoadConfigAsync Tests

    [TestMethod]
    public async Task LoadConfigAsync_NullSessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => persistence.LoadConfigAsync(null!));
    }

    [TestMethod]
    public async Task LoadConfigAsync_ConfigFileNotExists_ReturnsNull()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        string nonExistentSessionId = Guid.NewGuid().ToString("N");

        // Act
        ChatRoomSessionData? result = await persistence.LoadConfigAsync(nonExistentSessionId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task LoadConfigAsync_ConfigFileExists_ReturnsDeserializedData()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var originalData = new ChatRoomSessionData
        {
            SessionId = sessionId,
            Title = "加载测试",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Save first
        await persistence.SaveConfigAsync(originalData);

        // Act
        ChatRoomSessionData? result = await persistence.LoadConfigAsync(sessionId.ToString("N"));

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(sessionId, result.SessionId);
        Assert.AreEqual("加载测试", result.Title);
    }

    [TestMethod(DisplayName = "源生成上下文应往返执行种类且不写出运行时字段")]
    [Timeout(5000)]
    public void SourceGeneratedContextShouldRoundTripExecutionKindWithoutRuntimeState()
    {
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Roles =
            [
                new ChatRoomRoleDefinition
                {
                    RoleId = "coding-role",
                    ExecutionKind = ChatRoomRoleExecutionKind.Coding,
                    RoleName = "编程角色",
                },
            ],
        };

        string json = JsonSerializer.Serialize(data, ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);
        ChatRoomSessionData? result = JsonSerializer.Deserialize(
            json,
            ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);

        Assert.AreEqual(ChatRoomRoleExecutionKind.Coding, result!.Roles.Single().ExecutionKind);
        StringAssert.Contains(json, "\"ExecutionKind\": 1");
        Assert.IsFalse(json.Contains("Tools", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("CodingAgent", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("Executor", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "加载会话时应拒绝未知执行种类")]
    [Timeout(5000)]
    public async Task LoadConfigAsyncShouldRejectUnknownExecutionKind()
    {
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var data = new ChatRoomSessionData
        {
            SessionId = sessionId,
            CreatedAt = DateTimeOffset.UtcNow,
            Roles =
            [
                new ChatRoomRoleDefinition
                {
                    RoleId = "unknown-role",
                    ExecutionKind = (ChatRoomRoleExecutionKind)99,
                },
            ],
        };
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId.ToString("N"));
        Directory.CreateDirectory(sessionFolder);
        string json = JsonSerializer.Serialize(data, ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);
        await File.WriteAllTextAsync(Path.Join(sessionFolder, "room.config.json"), json);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
            persistence.LoadConfigAsync(sessionId.ToString("N")));
    }

    [TestMethod(DisplayName = "保存会话时应拒绝人类 Coding 角色")]
    [Timeout(5000)]
    public async Task SaveConfigAsyncShouldRejectHumanCodingRole()
    {
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            Roles =
            [
                new ChatRoomRoleDefinition
                {
                    RoleId = "invalid-human",
                    ExecutionKind = ChatRoomRoleExecutionKind.Coding,
                    IsHuman = true,
                },
            ],
        };

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => persistence.SaveConfigAsync(data));
    }

    [TestMethod(DisplayName = "保存会话时应拒绝包含目录穿越的角色标识")]
    [Timeout(5000)]
    public async Task SaveConfigAsyncShouldRejectRoleIdPathTraversal()
    {
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            Roles =
            [
                new ChatRoomRoleDefinition
                {
                    RoleId = Path.Join("..", "..", "escaped-role"),
                    RoleName = "无效角色",
                },
            ],
        };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => persistence.SaveConfigAsync(data));
    }

    [TestMethod(DisplayName = "加载会话时应拒绝包含目录穿越的角色标识")]
    [Timeout(5000)]
    public async Task LoadConfigAsyncShouldRejectRoleIdPathTraversal()
    {
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var data = new ChatRoomSessionData
        {
            SessionId = sessionId,
            Roles =
            [
                new ChatRoomRoleDefinition
                {
                    RoleId = Path.Join("..", "..", "escaped-role"),
                    RoleName = "无效角色",
                },
            ],
        };
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId.ToString("N"));
        Directory.CreateDirectory(sessionFolder);
        string json = JsonSerializer.Serialize(data, ChatRoomJsonSerializerContext.Default.ChatRoomSessionData);
        await File.WriteAllTextAsync(Path.Join(sessionFolder, "room.config.json"), json);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            persistence.LoadConfigAsync(sessionId.ToString("N")));
    }

    [TestMethod(DisplayName = "删除会话时应拒绝 Windows 目录别名")]
    [Timeout(5000)]
    public void DeleteShouldRejectWindowsDirectoryAlias()
    {
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        Assert.ThrowsExactly<ArgumentException>(() => persistence.Delete("..."));
        Assert.IsTrue(Directory.Exists(_tempBaseFolder));
    }

    #endregion

    #region SavePublicMessageAsync Tests

    [TestMethod]
    public async Task SavePublicMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => persistence.SavePublicMessageAsync(Guid.NewGuid(), null!));
    }

    [TestMethod]
    public async Task SavePublicMessageAsync_SystemMessage_CreatesLogFile()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var message = ChatRoomMessage.CreateSystem("系统消息内容");

        // Act
        await persistence.SavePublicMessageAsync(sessionId, message);

        // Assert - verify that the public logger created a log file
        string publicLogsPath = Path.Join(_tempBaseFolder, "public_logs");
        Assert.IsTrue(Directory.Exists(publicLogsPath), "Public logs directory should exist.");

        // The log file is created inside a date-based subfolder
        string[] allFiles = Directory.GetFiles(publicLogsPath, "*.log", SearchOption.AllDirectories);
        Assert.IsNotEmpty(allFiles);
    }

    [TestMethod]
    public async Task SavePublicMessageAsync_HumanMessage_CreatesLogFile()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var message = ChatRoomMessage.CreateHuman("人类消息", "human1", "用户");

        // Act
        await persistence.SavePublicMessageAsync(sessionId, message);

        // Assert
        string publicLogsPath = Path.Join(_tempBaseFolder, "public_logs");
        string[] allFiles = Directory.GetFiles(publicLogsPath, "*.log", SearchOption.AllDirectories);
        Assert.IsNotEmpty(allFiles);
    }

    [TestMethod]
    public async Task SavePublicMessageAsync_AssistantMessage_CreatesLogFile()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var message = ChatRoomMessage.CreateAssistant("助手消息", "assistant1", "助手");

        // Act
        await persistence.SavePublicMessageAsync(sessionId, message);

        // Assert
        string publicLogsPath = Path.Join(_tempBaseFolder, "public_logs");
        string[] allFiles = Directory.GetFiles(publicLogsPath, "*.log", SearchOption.AllDirectories);
        Assert.IsNotEmpty(allFiles);
    }

    #endregion

    #region SaveRoleMessageAsync Tests

    [TestMethod]
    public async Task SaveRoleMessageAsync_NullRoleId_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var copilotMessage = new CopilotChatMessage(ChatRole.Assistant, "测试内容");

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => persistence.SaveRoleMessageAsync(Guid.NewGuid(), null!, copilotMessage));
    }

    [TestMethod]
    public async Task SaveRoleMessageAsync_NullCopilotMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => persistence.SaveRoleMessageAsync(Guid.NewGuid(), "role1", null!));
    }

    [TestMethod]
    public async Task SaveRoleMessageAsync_ValidInput_CreatesRoleLogFiles()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var copilotMessage = new CopilotChatMessage(ChatRole.Assistant, "角色消息内容");

        // Act
        await persistence.SaveRoleMessageAsync(sessionId, "role1", copilotMessage);

        // Assert - the role logger should create log files in the session/roleId folder
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId.ToString());
        Assert.IsTrue(Directory.Exists(sessionFolder), "Session folder should exist.");

        string roleFolder = Path.Join(sessionFolder, "role1");
        Assert.IsTrue(Directory.Exists(roleFolder), "Role folder should exist.");
    }

    [TestMethod]
    public async Task SaveRoleMessageAsync_SameRoleId_ReusesLogger()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        var sessionId = Guid.NewGuid();
        var message1 = new CopilotChatMessage(ChatRole.Assistant, "消息1");
        var message2 = new CopilotChatMessage(ChatRole.User, "消息2");

        // Act - call twice with same roleId
        await persistence.SaveRoleMessageAsync(sessionId, "role1", message1);
        await persistence.SaveRoleMessageAsync(sessionId, "role1", message2);

        // Assert - both messages should be logged
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId.ToString());
        string roleFolder = Path.Join(sessionFolder, "role1");
        Assert.IsTrue(Directory.Exists(roleFolder), "Role folder should exist.");
    }

    #endregion

    #region ListSessionIds Tests

    [TestMethod]
    public void ListSessionIds_BaseFolderNotExists_ReturnsEmptyList()
    {
        // Arrange - use a path that doesn't exist
        string nonExistentFolder = Path.Join(_tempBaseFolder, "non_existent");
        var persistence = new ChatRoomPersistence(nonExistentFolder);
        // Delete the folder that was just created by the constructor
        Directory.Delete(nonExistentFolder, recursive: true);

        // Act
        IReadOnlyList<string> result = persistence.ListSessionIds();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ListSessionIds_BaseFolderExistsWithNoSubdirectories_ReturnsEmptyList()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act
        IReadOnlyList<string> result = persistence.ListSessionIds();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ListSessionIds_BaseFolderExistsWithSubdirectories_ReturnsDirectoryNames()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        string session1Folder = Path.Join(_tempBaseFolder, "session1");
        string session2Folder = Path.Join(_tempBaseFolder, "session2");
        Directory.CreateDirectory(session1Folder);
        Directory.CreateDirectory(session2Folder);

        // Act
        IReadOnlyList<string> result = persistence.ListSessionIds();

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result);
        Assert.IsTrue(result.Contains("session1"));
        Assert.IsTrue(result.Contains("session2"));
    }

    [TestMethod]
    public void ListSessionIds_BaseFolderHasFiles_OnlyReturnsDirectories()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        Directory.CreateDirectory(Path.Join(_tempBaseFolder, "session1"));
        File.WriteAllText(Path.Join(_tempBaseFolder, "some_file.txt"), "content");

        // Act
        IReadOnlyList<string> result = persistence.ListSessionIds();

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(1, result);
        Assert.IsTrue(result.Contains("session1"));
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public void Delete_NullSessionId_ThrowsArgumentNullException()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => persistence.Delete(null!));
    }

    [TestMethod]
    public void Delete_SessionFolderNotExists_DoesNothing()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        string nonExistentSessionId = "non_existent_session";

        // Act - should not throw
        persistence.Delete(nonExistentSessionId);

        // Assert - base folder should still exist, no side effects
        Assert.IsTrue(Directory.Exists(_tempBaseFolder));
    }

    [TestMethod]
    public void Delete_SessionFolderExists_DeletesFolder()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        string sessionId = "session_to_delete";
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId);
        Directory.CreateDirectory(sessionFolder);
        File.WriteAllText(Path.Join(sessionFolder, "test.txt"), "data");

        // Act
        persistence.Delete(sessionId);

        // Assert
        Assert.IsFalse(Directory.Exists(sessionFolder), "Session folder should be deleted.");
    }

    [TestMethod]
    public void Delete_AlreadyDeletedSession_DoesNothing()
    {
        // Arrange
        var persistence = new ChatRoomPersistence(_tempBaseFolder);
        string sessionId = "session_to_delete_twice";
        string sessionFolder = Path.Join(_tempBaseFolder, sessionId);
        Directory.CreateDirectory(sessionFolder);

        // Act - delete twice
        persistence.Delete(sessionId);
        persistence.Delete(sessionId);

        // Assert - no exception thrown, folder is gone
        Assert.IsFalse(Directory.Exists(sessionFolder));
    }

    #endregion
}
