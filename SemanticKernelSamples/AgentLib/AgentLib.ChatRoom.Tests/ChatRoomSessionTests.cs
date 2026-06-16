using System.ComponentModel;
using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using Moq;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomSessionTests
{
    [TestMethod]
    public void Constructor_WithSessionIdAndCreatedAt_SetsSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 3, 15, 14, 30, 0, TimeSpan.FromHours(8));

        // Act
        var session = new ChatRoomSession(sessionId, createdAt);

        // Assert
        Assert.AreEqual(sessionId, session.SessionId);
    }

    [TestMethod]
    public void Constructor_WithSessionIdAndCreatedAt_SetsCreatedAt()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 3, 15, 14, 30, 0, TimeSpan.FromHours(8));

        // Act
        var session = new ChatRoomSession(sessionId, createdAt);

        // Assert
        Assert.AreEqual(createdAt, session.CreatedAt);
    }

    [TestMethod]
    public void Constructor_WithSessionIdAndCreatedAt_TitleDefaultsToChatRoom()
    {
        // Arrange & Act
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Assert
        Assert.AreEqual("聊天室", session.Title);
    }

    [TestMethod]
    public void Constructor_WithSessionIdAndCreatedAt_MainThreadDispatcherDefaultsToNull()
    {
        // Arrange & Act
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Assert
        Assert.IsNull(session.MainThreadDispatcher);
    }

    [TestMethod]
    public void Constructor_Parameterless_GeneratesNonEmptySessionId()
    {
        // Arrange & Act
        var session = new ChatRoomSession();

        // Assert
        Assert.AreNotEqual(Guid.Empty, session.SessionId);
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatedAtIsCloseToNow()
    {
        // Arrange
        var before = DateTimeOffset.Now;

        // Act
        var session = new ChatRoomSession();
        var after = DateTimeOffset.Now;

        // Assert
        Assert.IsTrue(session.CreatedAt >= before);
        Assert.IsTrue(session.CreatedAt <= after);
    }

    [TestMethod]
    public void Constructor_Parameterless_TitleDefaultsToChatRoom()
    {
        // Arrange & Act
        var session = new ChatRoomSession();

        // Assert
        Assert.AreEqual("聊天室", session.Title);
    }

    [TestMethod]
    public void Title_Set_ChangesValue()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act
        session.Title = "新标题";

        // Assert
        Assert.AreEqual("新标题", session.Title);
    }

    [TestMethod]
    public void Title_SetSameValue_DoesNotFirePropertyChanged()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var firedProperties = new List<string>();
        ((INotifyPropertyChanged)session).PropertyChanged += (_, args) =>
            firedProperties.Add(args.PropertyName!);

        // Act
        session.Title = "聊天室";

        // Assert
        Assert.IsEmpty(firedProperties);
    }

    [TestMethod]
    public void Title_SetDifferentValue_FiresPropertyChangedForTitle()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var firedProperties = new List<string>();
        ((INotifyPropertyChanged)session).PropertyChanged += (_, args) =>
            firedProperties.Add(args.PropertyName!);

        // Act
        session.Title = "新标题";

        // Assert
        Assert.Contains("Title", firedProperties);
    }

    [TestMethod]
    public void Title_SetDifferentValue_FiresPropertyChangedForDisplayText()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var firedProperties = new List<string>();
        ((INotifyPropertyChanged)session).PropertyChanged += (_, args) =>
            firedProperties.Add(args.PropertyName!);

        // Act
        session.Title = "新标题";

        // Assert
        Assert.Contains("DisplayText", firedProperties);
    }

    [TestMethod]
    public void Title_SetNull_SetsToNull()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act
        session.Title = null!;

        // Assert
        Assert.IsNull(session.Title);
    }

    [TestMethod]
    public void DisplayText_ReturnsFormattedTitleAndCreatedAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 3, 15, 14, 30, 0, TimeSpan.FromHours(8));
        var session = new ChatRoomSession(Guid.NewGuid(), createdAt);
        session.Title = "测试";

        // Act
        var displayText = session.DisplayText;

        // Assert
        Assert.AreEqual("测试 03-15 14:30", displayText);
    }

    [TestMethod]
    public void DisplayText_UpdatesWhenTitleChanges()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 3, 15, 14, 30, 0, TimeSpan.FromHours(8));
        var session = new ChatRoomSession(Guid.NewGuid(), createdAt);
        var changedProperties = new List<string>();
        ((INotifyPropertyChanged)session).PropertyChanged += (_, args) =>
            changedProperties.Add(args.PropertyName!);

        // Act
        session.Title = "新标题";

        // Assert
        Assert.AreEqual("新标题 03-15 14:30", session.DisplayText);
        Assert.Contains("DisplayText", changedProperties);
    }

    [TestMethod]
    public void MainThreadDispatcher_DefaultIsNull()
    {
        // Arrange & Act
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Assert
        Assert.IsNull(session.MainThreadDispatcher);
    }

    [TestMethod]
    public void MainThreadDispatcher_CanBeSetViaInit()
    {
        // Arrange
        var mockDispatcher = new Mock<IMainThreadDispatcher>();

        // Act
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = mockDispatcher.Object
        };

        // Assert
        Assert.AreSame(mockDispatcher.Object, session.MainThreadDispatcher);
    }

    [TestMethod]
    public void MainThreadDispatcher_InitNull_StaysNull()
    {
        // Act
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = null
        };

        // Assert
        Assert.IsNull(session.MainThreadDispatcher);
    }

    [TestMethod]
    public async Task AddMessageAsync_WithoutDispatcher_AddsMessageToCollection()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };

        // Act
        await session.AddMessageAsync(message);

        // Assert
        Assert.HasCount(1, session.Messages);
        Assert.AreSame(message, session.Messages[0]);
    }

    [TestMethod]
    public async Task AddMessageAsync_WithoutDispatcher_UpdatesLastSpeakTime()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = timestamp,
        };

        // Act
        await session.AddMessageAsync(message);

        // Assert
        Assert.IsTrue(session.HasRoleSpoken("role1"));
    }

    [TestMethod]
    public async Task AddMessageAsync_WithoutDispatcher_NullMessageThrows()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => session.AddMessageAsync(null!));
    }

    [TestMethod]
    public async Task AddMessageAsync_WithDispatcher_InvokesOnMainThread()
    {
        // Arrange
        var mockDispatcher = new Mock<IMainThreadDispatcher>();
        mockDispatcher
            .Setup(d => d.CheckAccess())
            .Returns(true);
        mockDispatcher
            .Setup(d => d.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(func => func());
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = mockDispatcher.Object,
        };
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };

        // Act
        await session.AddMessageAsync(message);

        // Assert
        mockDispatcher.Verify(d => d.InvokeAsync(It.IsAny<Func<Task>>()), Times.Once);
        Assert.HasCount(1, session.Messages);
    }

    [TestMethod]
    public async Task AddMessageAsync_WithDispatcher_DoesNotAddDirectly()
    {
        // Arrange
        var mockDispatcher = new Mock<IMainThreadDispatcher>();
        // Don't invoke the action - simulate dispatcher that queues but doesn't execute synchronously
        mockDispatcher
            .Setup(d => d.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns(Task.CompletedTask);
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = mockDispatcher.Object,
        };
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };

        // Act
        await session.AddMessageAsync(message);

        // Assert
        mockDispatcher.Verify(d => d.InvokeAsync(It.IsAny<Func<Task>>()), Times.Once);
        // Message is not added because dispatcher didn't execute the action
        Assert.HasCount(0, session.Messages);
    }

    [TestMethod]
    public void AddMessage_AddsMessageToCollection()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };

        // Act
        session.AddMessage(message);

        // Assert
        Assert.HasCount(1, session.Messages);
        Assert.AreSame(message, session.Messages[0]);
    }

    [TestMethod]
    public void AddMessage_UpdatesLastSpeakTime()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = timestamp,
        };

        // Act
        session.AddMessage(message);

        // Assert
        Assert.IsTrue(session.HasRoleSpoken("role1"));
    }

    [TestMethod]
    public void AddMessage_NullMessageThrows()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => session.AddMessage(null!));
    }

    [TestMethod]
    public void GetMessagesSinceLastSpeak_NullRoleIdThrows()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => session.GetMessagesSinceLastSpeak(null!));
    }

    [TestMethod]
    public void GetMessagesSinceLastSpeak_RoleNeverSpoken_ReturnsAllMessages()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message1 = new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };
        var message2 = new ChatRoomMessage
        {
            Content = "Msg2",
            SenderRoleId = "role2",
            SenderRoleName = "Role Two",
        };
        session.AddMessage(message1);
        session.AddMessage(message2);

        // Act
        var result = session.GetMessagesSinceLastSpeak("role3");

        // Assert
        Assert.HasCount(2, result);
        Assert.AreSame(message1, result[0]);
        Assert.AreSame(message2, result[1]);
    }

    [TestMethod]
    public void GetMessagesSinceLastSpeak_RoleHasSpoken_ReturnsMessagesAfterLastSpeak()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var t1 = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message1 = new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = t1,
        };
        var message2 = new ChatRoomMessage
        {
            Content = "Msg2",
            SenderRoleId = "role2",
            SenderRoleName = "Role Two",
            Timestamp = t2,
        };
        var message3 = new ChatRoomMessage
        {
            Content = "Msg3",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = t3,
        };
        session.AddMessage(message1);
        session.AddMessage(message2);
        session.AddMessage(message3);

        // Act - role2 spoke at t2, so should get messages after t2
        var result = session.GetMessagesSinceLastSpeak("role2");

        // Assert - only message3 is after t2
        Assert.HasCount(1, result);
        Assert.AreSame(message3, result[0]);
    }

    [TestMethod]
    public void GetMessagesSinceLastSpeak_RoleSpokeLast_ReturnsEmptyList()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var t1 = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero);
        var message1 = new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = t1,
        };
        var message2 = new ChatRoomMessage
        {
            Content = "Msg2",
            SenderRoleId = "role2",
            SenderRoleName = "Role Two",
            Timestamp = t2,
        };
        session.AddMessage(message1);
        session.AddMessage(message2);

        // Act - role2 spoke at t2 (the last message), so nothing after
        var result = session.GetMessagesSinceLastSpeak("role2");

        // Assert
        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void GetMessagesSinceLastSpeak_NoMessages_ReturnsEmptyList()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act
        var result = session.GetMessagesSinceLastSpeak("role1");

        // Assert
        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void HasRoleSpoken_NullRoleIdThrows()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => session.HasRoleSpoken(null!));
    }

    [TestMethod]
    public void HasRoleSpoken_RoleNeverSpoken_ReturnsFalse()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act
        var result = session.HasRoleSpoken("role1");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasRoleSpoken_RoleHasSpoken_ReturnsTrue()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message = new ChatRoomMessage
        {
            Content = "Hello",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };
        session.AddMessage(message);

        // Act
        var result = session.HasRoleSpoken("role1");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void FromPersistence_NullDataThrows()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => ChatRoomSession.FromPersistence(null!));
    }

    [TestMethod]
    public void FromPersistence_RestoresSessionIdAndCreatedAtAndTitle()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var data = new ChatRoomSessionData
        {
            SessionId = sessionId,
            CreatedAt = createdAt,
            Title = "测试聊天室",
        };

        // Act
        var session = ChatRoomSession.FromPersistence(data);

        // Assert
        Assert.AreEqual(sessionId, session.SessionId);
        Assert.AreEqual(createdAt, session.CreatedAt);
        Assert.AreEqual("测试聊天室", session.Title);
    }

    [TestMethod]
    public void FromPersistence_RestoresMessages()
    {
        // Arrange
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Messages = new List<ChatRoomMessage>
            {
                new()
                {
                    Content = "Msg1",
                    SenderRoleId = "role1",
                    SenderRoleName = "Role One",
                },
                new()
                {
                    Content = "Msg2",
                    SenderRoleId = "role2",
                    SenderRoleName = "Role Two",
                },
            },
        };

        // Act
        var session = ChatRoomSession.FromPersistence(data);

        // Assert
        Assert.HasCount(2, session.Messages);
        Assert.AreEqual("Msg1", session.Messages[0].Content);
        Assert.AreEqual("Msg2", session.Messages[1].Content);
    }

    [TestMethod]
    public void FromPersistence_RestoresLastSpeakTimes()
    {
        // Arrange
        var data = new ChatRoomSessionData
        {
            SessionId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Messages = new List<ChatRoomMessage>
            {
                new()
                {
                    Content = "Msg1",
                    SenderRoleId = "role1",
                    SenderRoleName = "Role One",
                },
                new()
                {
                    Content = "Msg2",
                    SenderRoleId = "role2",
                    SenderRoleName = "Role Two",
                },
            },
        };

        // Act
        var session = ChatRoomSession.FromPersistence(data);

        // Assert
        Assert.IsTrue(session.HasRoleSpoken("role1"));
        Assert.IsTrue(session.HasRoleSpoken("role2"));
    }

    [TestMethod]
    public void FromPersistence_EmptyMessages_Works()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var data = new ChatRoomSessionData
        {
            SessionId = sessionId,
            CreatedAt = createdAt,
        };

        // Act
        var session = ChatRoomSession.FromPersistence(data);

        // Assert
        Assert.AreEqual(sessionId, session.SessionId);
        Assert.HasCount(0, session.Messages);
    }

    [TestMethod]
    public void ToPersistence_WithoutMessages_LastActivityAtEqualsCreatedAt()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var session = new ChatRoomSession(sessionId, createdAt);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.AreEqual(createdAt, result.LastActivityAt);
    }

    [TestMethod]
    public void ToPersistence_WithMessages_LastActivityAtEqualsLastMessageTimestamp()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var lastTimestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var session = new ChatRoomSession(sessionId, createdAt);
        session.AddMessage(new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
            Timestamp = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero),
        });
        session.AddMessage(new ChatRoomMessage
        {
            Content = "Msg2",
            SenderRoleId = "role2",
            SenderRoleName = "Role Two",
            Timestamp = lastTimestamp,
        });
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.AreEqual(lastTimestamp, result.LastActivityAt);
    }

    [TestMethod]
    public void ToPersistence_CopiesSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var session = new ChatRoomSession(sessionId, createdAt);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.AreEqual(sessionId, result.SessionId);
    }

    [TestMethod]
    public void ToPersistence_CopiesTitle()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            Title = "自定义标题",
        };
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.AreEqual("自定义标题", result.Title);
    }

    [TestMethod]
    public void ToPersistence_CopiesCreatedAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(8));
        var session = new ChatRoomSession(Guid.NewGuid(), createdAt);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.AreEqual(createdAt, result.CreatedAt);
    }

    [TestMethod]
    public void ToPersistence_CopiesRoleDefinitionsToRoles()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var roleDefinitions = new List<ChatRoomRoleDefinition>
        {
            new() { RoleId = "role1", RoleName = "Role One" },
            new() { RoleId = "role2", RoleName = "Role Two" },
        };

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.HasCount(2, result.Roles);
        Assert.AreEqual("role1", result.Roles[0].RoleId);
        Assert.AreEqual("role2", result.Roles[1].RoleId);
    }

    [TestMethod]
    public void ToPersistence_RoleDefinitionsListIsIndependentCopy()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var roleDefinitions = new List<ChatRoomRoleDefinition>
        {
            new() { RoleId = "role1", RoleName = "Role One" },
        };

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Modify the original list
        roleDefinitions.Clear();

        // Assert - result should still have the original role
        Assert.HasCount(1, result.Roles);
    }

    [TestMethod]
    public void ToPersistence_CopiesMessages()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var message1 = new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        };
        var message2 = new ChatRoomMessage
        {
            Content = "Msg2",
            SenderRoleId = "role2",
            SenderRoleName = "Role Two",
        };
        session.AddMessage(message1);
        session.AddMessage(message2);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.HasCount(2, result.Messages);
        Assert.AreSame(message1, result.Messages[0]);
        Assert.AreSame(message2, result.Messages[1]);
    }

    [TestMethod]
    public void ToPersistence_MessagesListIsIndependentCopy()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        session.AddMessage(new ChatRoomMessage
        {
            Content = "Msg1",
            SenderRoleId = "role1",
            SenderRoleName = "Role One",
        });
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Modify the original collection
        session.Messages.Clear();

        // Assert - result should still have the original message
        Assert.HasCount(1, result.Messages);
    }

    [TestMethod]
    public void ToPersistence_EmptyRoleDefinitions_ProducesEmptyRoles()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.HasCount(0, result.Roles);
    }

    [TestMethod]
    public void ToPersistence_EmptyMessages_ProducesEmptyMessagesList()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);
        var roleDefinitions = new List<ChatRoomRoleDefinition>();

        // Act
        var result = session.ToPersistence(roleDefinitions);

        // Assert
        Assert.HasCount(0, result.Messages);
    }

    [TestMethod]
    public void ToPersistence_NullRoleDefinitions_ThrowsArgumentNullException()
    {
        // Arrange
        var session = new ChatRoomSession(Guid.NewGuid(), DateTimeOffset.Now);

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => session.ToPersistence(null!));
    }
}