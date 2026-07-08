using System.ComponentModel;
using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using Moq;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomManagerTests
{
    [TestMethod]
    public void Constructor_WithNullSession_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ChatRoomManager(null!));
    }

    [TestMethod]
    public void Constructor_WithValidSession_SetsSessionProperty()
    {
        var session = new ChatRoomSession();

        var manager = new ChatRoomManager(session);

        Assert.AreSame(session, manager.Session);
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatesNonNullSession()
    {
        var manager = new ChatRoomManager();

        Assert.IsNotNull(manager.Session);
    }

    [TestMethod]
    public void Constructor_Parameterless_SessionHasNonEmptyId()
    {
        var manager = new ChatRoomManager();

        Assert.AreNotEqual(Guid.Empty, manager.Session.SessionId);
    }

    [TestMethod]
    public void IsRunning_DefaultValue_IsFalse()
    {
        var manager = new ChatRoomManager();

        Assert.IsFalse(manager.IsRunning);
    }

    [TestMethod]
    public void CanStartLoop_WhenNotRunningAndNoRoles_ReturnsFalse()
    {
        var manager = new ChatRoomManager();

        Assert.IsFalse(manager.CanStartLoop);
    }

    [TestMethod]
    public async Task CanStartLoop_WhenNotRunningAndHasRoles_ReturnsTrue()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            IsHuman = false,
        };
        var role = new ChatRoomRole(definition);
        await manager.AddRoleAsync(role);

        Assert.IsTrue(manager.CanStartLoop);
    }

    [TestMethod]
    public async Task CanStartLoop_WhenNotRunningAndHasMultipleRoles_ReturnsTrue()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var definition1 = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Role 1",
        };
        var definition2 = new ChatRoomRoleDefinition
        {
            RoleId = "role-2",
            RoleName = "Role 2",
        };
        await manager.AddRoleAsync(new ChatRoomRole(definition1));
        await manager.AddRoleAsync(new ChatRoomRole(definition2));

        Assert.IsTrue(manager.CanStartLoop);
    }

    [TestMethod]
    public void CanStop_WhenNotRunning_ReturnsFalse()
    {
        var manager = new ChatRoomManager();

        Assert.IsFalse(manager.CanStop);
    }

    [TestMethod]
    public async Task IsRunning_AfterStartAutoLoopWithoutTrigger_ChangesToTrueThenFalse()
    {
        var manager = new ChatRoomManager();

        var changedProperties = new List<string>();
        ((INotifyPropertyChanged)manager).PropertyChanged += (_, args) =>
            changedProperties.Add(args.PropertyName!);

        await manager.StartAutoLoopAsync();

        Assert.IsFalse(manager.IsRunning);
        Assert.Contains("IsRunning", changedProperties);
        Assert.Contains("CanStartLoop", changedProperties);
        Assert.Contains("CanStop", changedProperties);
    }

    [TestMethod]
    public async Task CanStartLoop_WhenRoleRemovedAfterBeingAdded_ReturnsFalse()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        };
        var role = new ChatRoomRole(definition);
        await manager.AddRoleAsync(role);

        Assert.IsTrue(manager.CanStartLoop);

        manager.RemoveRole(role.Definition.RoleId);

        Assert.IsFalse(manager.CanStartLoop);
    }

    [TestMethod]
    public void CurrentSpeaker_DefaultValue_IsNull()
    {
        var manager = new ChatRoomManager();

        Assert.IsNull(manager.CurrentSpeaker);
    }

    [TestMethod]
    public void IsSpeaking_DefaultValue_IsFalse()
    {
        var manager = new ChatRoomManager();

        Assert.IsFalse(manager.IsSpeaking);
    }

    [TestMethod]
    public async Task StepAsync_NullRole_ThrowsArgumentNullException()
    {
        var manager = new ChatRoomManager();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => manager.StepAsync(null!));
    }

    [TestMethod]
    public async Task StepAsync_HumanRole_ReturnsNull()
    {
        var manager = new ChatRoomManager();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "human-1",
            RoleName = "Human",
            IsHuman = true,
        };
        var role = new ChatRoomRole(definition);

        ChatRoomMessage? result = await manager.StepAsync(role);

        Assert.IsNull(result);
        Assert.IsNull(manager.CurrentSpeaker);
        Assert.IsFalse(manager.IsSpeaking);
    }

    [TestMethod]
    public async Task StepAsync_NonHumanRole_ClearsCurrentSpeakerInFinally()
    {
        var manager = new ChatRoomManager();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            IsHuman = false,
        };
        var role = new ChatRoomRole(definition);

        await manager.StepAsync(role);

        Assert.IsNull(manager.CurrentSpeaker);
        Assert.IsFalse(manager.IsSpeaking);
    }

    [TestMethod]
    public async Task StepAsync_NonHumanRole_FiresOnSpeakingChangedEvent()
    {
        var manager = new ChatRoomManager();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            IsHuman = false,
        };
        var role = new ChatRoomRole(definition);

        var speakingEvents = new List<SpeakingChangedEventArgs>();
        manager.OnSpeakingChanged += (_, args) => speakingEvents.Add(args);

        await manager.StepAsync(role);

        Assert.HasCount(2, speakingEvents);

        // First event: null → role
        Assert.IsNull(speakingEvents[0].PreviousSpeaker);
        Assert.AreSame(role, speakingEvents[0].CurrentSpeaker);

        // Second event: role → null (from finally)
        Assert.AreSame(role, speakingEvents[1].PreviousSpeaker);
        Assert.IsNull(speakingEvents[1].CurrentSpeaker);
    }

    [TestMethod]
    public async Task StepAsync_NonHumanRole_FiresIsSpeakingPropertyChanged()
    {
        var manager = new ChatRoomManager();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            IsHuman = false,
        };
        var role = new ChatRoomRole(definition);

        var propertyChanges = new List<string?>();
        ((INotifyPropertyChanged)manager).PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        await manager.StepAsync(role);

        Assert.Contains("IsSpeaking", propertyChanges);
    }

    [TestMethod]
    public async Task StepAsync_NonHumanRole_WhenSpeakThrows_FiresOnRoleSpeakFailedAndReturnsSystemMessage()
    {
        var manager = new ChatRoomManager();
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            IsHuman = false,
        };
        var role = new ChatRoomRole(definition);

        // 需要先添加消息，使 BuildIncrementalUserMessages 返回非空内容，否则 StepAsync 会提前返回 null
        await manager.HumanInterjectAsync("测试消息", "human", "Human");

        RoleSpeakFailedEventArgs? eventArgs = null;
        manager.OnRoleSpeakFailed += (_, args) => eventArgs = args;

        ChatRoomMessage? result = await manager.StepAsync(role);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSystemMessage);
        Assert.Contains("Test Role", result.Content);
        Assert.IsNotNull(eventArgs);
        Assert.AreSame(role, eventArgs.Role);
        Assert.IsNotNull(eventArgs.Exception);
    }

    [TestMethod]
    public async Task StartAutoLoopAsync_WithoutTrigger_CleansUpCurrentSpeakerAndIsSpeaking()
    {
        var manager = new ChatRoomManager();

        await manager.StartAutoLoopAsync();

        Assert.IsNull(manager.CurrentSpeaker);
        Assert.IsFalse(manager.IsSpeaking);
    }

    [TestMethod]
    public async Task HumanInterjectAsync_NullContent_ThrowsArgumentNullException()
    {
        var manager = new ChatRoomManager();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            manager.HumanInterjectAsync(null!, "human-1", "Human"));
    }

    [TestMethod]
    public async Task HumanInterjectAsync_ValidContent_AppendsMessageAndFiresEvent()
    {
        var manager = new ChatRoomManager();
        ChatRoomMessage? receivedMessage = null;
        manager.OnMessageAdded += (_, msg) => receivedMessage = msg;

        await manager.HumanInterjectAsync("Hello", "human-1", "Human");

        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("Hello", receivedMessage.Content);
        Assert.IsTrue(receivedMessage.IsHumanMessage);
        Assert.AreEqual("human-1", receivedMessage.SenderRoleId);
        Assert.AreEqual("Human", receivedMessage.SenderRoleName);
        Assert.HasCount(1, manager.Session.Messages);
        Assert.AreEqual("Hello", manager.Session.Messages[0].Content);
    }

    [TestMethod]
    public async Task HumanInterjectAsync_AddsMessageToSession()
    {
        var manager = new ChatRoomManager();

        await manager.HumanInterjectAsync("Test message", "human-1", "Human");

        Assert.HasCount(1, manager.Session.Messages);
        Assert.AreEqual("Test message", manager.Session.Messages[0].Content);
    }

    [TestMethod]
    [Timeout(300)]
    public void Stop_WhenNotRunning_DoesNotThrow()
    {
        var manager = new ChatRoomManager();

        manager.Stop();

        // No exception means the null conditional Cancel was a no-op
    }

    [TestMethod]
    public void RegisterRoleModelProviders_NullDictionary_ThrowsArgumentNullException()
    {
        var manager = new ChatRoomManager();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            manager.RegisterRoleModelProviders(null!));
    }

    [TestMethod]
    public void RegisterRoleModelProviders_EmptyRoles_DoesNotThrow()
    {
        var manager = new ChatRoomManager();
        var providers = new Dictionary<string, ILanguageModelProvider>();

        manager.RegisterRoleModelProviders(providers);

        // No exception means it handled empty roles gracefully
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_RoleWithNullModelProviderId_RegistersAllProviders()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = null,
        };
        var role = new ChatRoomRole(definition);
        await manager.AddRoleAsync(role);

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels()).Returns(new List<ILanguageModel>());
        var providers = new Dictionary<string, ILanguageModelProvider>
        {
            ["other-provider"] = mockProvider.Object,
        };

        manager.RegisterRoleModelProviders(providers);

        // 无论 ModelProviderId 为何值，都注册所有可用提供商
        mockProvider.Verify(p => p.GetSupportedModels(), Times.Once);
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_RoleWithEmptyModelProviderId_RegistersAllProviders()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = "",
        };
        var role = new ChatRoomRole(definition);
        await manager.AddRoleAsync(role);

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels()).Returns(new List<ILanguageModel>());
        var providers = new Dictionary<string, ILanguageModelProvider>
        {
            ["other-provider"] = mockProvider.Object,
        };

        manager.RegisterRoleModelProviders(providers);

        // 无论 ModelProviderId 为何值，都注册所有可用提供商
        mockProvider.Verify(p => p.GetSupportedModels(), Times.Once);
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_MatchingProvider_RegistersOnEndpointManager()
    {
        var manager = new ChatRoomManager();

        var mockModel = new Mock<ILanguageModel>();
        mockModel.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "my-provider", ModelName = "test-model" });

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });

        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["my-provider"] = mockProvider.Object,
        });

        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = "my-provider",
        };
        var role = new ChatRoomRole(definition);
        await manager.AddRoleAsync(role);

        // 无论 ModelProviderId 为何值，都注册所有可用提供商
        mockProvider.Verify(p => p.GetSupportedModels(), Times.Once);
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_ProviderNotInDictionary_ThrowsPrimaryModelNotFound()
    {
        var manager = new ChatRoomManager();

        var mockModel = new Mock<ILanguageModel>();
        mockModel.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "other-provider", ModelName = "test-model" });

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });

        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["other-provider"] = mockProvider.Object,
        });

        var definition = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = "missing-provider",
        };
        var role = new ChatRoomRole(definition);

        // 新逻辑：所有提供商都会注册，但找不到 ModelProviderId 匹配的首选模型时抛出异常
        await Assert.ThrowsExactlyAsync<PrimaryModelNotFoundException>(() => manager.AddRoleAsync(role));
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_MultipleRoles_AllProvidersRegistered()
    {
        var manager = new ChatRoomManager();

        var mockModelA = new Mock<ILanguageModel>();
        mockModelA.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "provider-a", ModelName = "model-a" });
        var mockModelB = new Mock<ILanguageModel>();
        mockModelB.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "provider-b", ModelName = "model-b" });

        var mockProviderA = new Mock<ILanguageModelProvider>();
        mockProviderA.Setup(p => p.GetSupportedModels()).Returns(new List<ILanguageModel> { mockModelA.Object });
        var mockProviderB = new Mock<ILanguageModelProvider>();
        mockProviderB.Setup(p => p.GetSupportedModels()).Returns(new List<ILanguageModel> { mockModelB.Object });

        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["provider-a"] = mockProviderA.Object,
            ["provider-b"] = mockProviderB.Object,
        });

        var definition1 = new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Role 1",
            ModelProviderId = "provider-a",
        };
        var definition2 = new ChatRoomRoleDefinition
        {
            RoleId = "role-2",
            RoleName = "Role 2",
            ModelProviderId = null,
        };
        var definition3 = new ChatRoomRoleDefinition
        {
            RoleId = "role-3",
            RoleName = "Role 3",
            ModelProviderId = "provider-b",
        };
        await manager.AddRoleAsync(new ChatRoomRole(definition1));
        await manager.AddRoleAsync(new ChatRoomRole(definition2));
        await manager.AddRoleAsync(new ChatRoomRole(definition3));

        // 新逻辑：每个角色都注册所有可用提供商
        mockProviderA.Verify(p => p.GetSupportedModels(), Times.Exactly(3));
        mockProviderB.Verify(p => p.GetSupportedModels(), Times.Exactly(3));
    }

    [TestMethod]
    public async Task SaveAsync_PersistenceIsNull_ReturnsImmediately()
    {
        var manager = new ChatRoomManager();

        await manager.SaveAsync();

        // No exception, no persistence — just returns
    }

    [TestMethod]
    public async Task SaveAsync_PersistenceIsNull_DoesNotThrow()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        await manager.AddRoleAsync(new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test",
        }));

        await manager.SaveAsync();

        // No exception thrown
    }

    [TestMethod]
    public async Task LoadAsync_PersistenceIsNull_ReturnsImmediately()
    {
        var manager = new ChatRoomManager();

        await manager.LoadAsync("some-session-id");

        // No exception thrown
    }

    [TestMethod]
    public async Task LoadAsync_PersistenceIsNull_DoesNotThrow()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        await manager.AddRoleAsync(new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test",
        }));

        await manager.LoadAsync("some-session-id");

        // No exception thrown, roles remain unchanged
        Assert.HasCount(1, manager.Roles);
    }

    [TestMethod]
    public void SpeakingChangedEventArgs_BothNull_SetsPropertiesCorrectly()
    {
        var args = new SpeakingChangedEventArgs(null, null);

        Assert.IsNull(args.PreviousSpeaker);
        Assert.IsNull(args.CurrentSpeaker);
    }

    [TestMethod]
    public void SpeakingChangedEventArgs_BothNotNull_SetsPropertiesCorrectly()
    {
        var previous = new ChatRoomRole(new ChatRoomRoleDefinition { RoleId = "prev", RoleName = "Prev" });
        var current = new ChatRoomRole(new ChatRoomRoleDefinition { RoleId = "curr", RoleName = "Curr" });

        var args = new SpeakingChangedEventArgs(previous, current);

        Assert.AreSame(previous, args.PreviousSpeaker);
        Assert.AreSame(current, args.CurrentSpeaker);
    }

    [TestMethod]
    public void SpeakingChangedEventArgs_PreviousNullCurrentNotNull_SetsPropertiesCorrectly()
    {
        var current = new ChatRoomRole(new ChatRoomRoleDefinition { RoleId = "curr", RoleName = "Curr" });

        var args = new SpeakingChangedEventArgs(null, current);

        Assert.IsNull(args.PreviousSpeaker);
        Assert.AreSame(current, args.CurrentSpeaker);
    }

    [TestMethod]
    public void SpeakingChangedEventArgs_PreviousNotNullCurrentNull_SetsPropertiesCorrectly()
    {
        var previous = new ChatRoomRole(new ChatRoomRoleDefinition { RoleId = "prev", RoleName = "Prev" });

        var args = new SpeakingChangedEventArgs(previous, null);

        Assert.AreSame(previous, args.PreviousSpeaker);
        Assert.IsNull(args.CurrentSpeaker);
    }

    [TestMethod]
    public async Task AddRoleAsync_AfterRegisterProviders_NewRoleGetsModels()
    {
        var manager = new ChatRoomManager();

        var mockProvider = new Mock<ILanguageModelProvider>();
        var mockModel = new Mock<ILanguageModel>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        });
        await manager.AddRoleAsync(role);

        Assert.HasCount(1, role.EndpointManager.GetSupportedModels());
    }

    [TestMethod]
    public async Task AddRoleAsync_AfterRegisterProviders_NewRolePassesEnsureModelAvailable()
    {
        var manager = new ChatRoomManager();

        var mockProvider = new Mock<ILanguageModelProvider>();
        var mockModel = new Mock<ILanguageModel>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        });
        await manager.AddRoleAsync(role);

        role.EnsureModelAvailable();
    }

    [TestMethod]
    public async Task AddRoleAsync_BeforeRegisterProviders_NewRoleHasNoModels()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        });
        await manager.AddRoleAsync(role);

        Assert.HasCount(0, role.EndpointManager.GetSupportedModels());
    }

    [TestMethod]
    public async Task AddRoleAsync_WithSpecificModelProviderId_RegistersAllProvidersAndSetsPrimary()
    {
        var manager = new ChatRoomManager();

        var mockModelA = new Mock<ILanguageModel>();
        mockModelA.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "provider-a", ModelName = "model-a" });

        var mockModelB = new Mock<ILanguageModel>();
        mockModelB.SetupGet(m => m.ModelDefinition)
            .Returns(new ModelDefinition { Provider = "provider-b", ModelName = "model-b" });

        var mockProviderA = new Mock<ILanguageModelProvider>();
        mockProviderA.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModelA.Object });

        var mockProviderB = new Mock<ILanguageModelProvider>();
        mockProviderB.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModelB.Object });

        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["provider-a"] = mockProviderA.Object,
            ["provider-b"] = mockProviderB.Object,
        });

        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
            ModelProviderId = "provider-a",
        });
        await manager.AddRoleAsync(role);

        // 新逻辑：所有提供商都注册，但首选模型由 ModelProviderId 决定
        Assert.HasCount(2, role.EndpointManager.GetSupportedModels());
        Assert.AreSame(mockModelA.Object, role.EndpointManager.PrimaryModel);
        mockProviderA.Verify(p => p.GetSupportedModels(), Times.Once);
        mockProviderB.Verify(p => p.GetSupportedModels(), Times.Once);
    }

    [TestMethod]
    public async Task AddRoleAsync_HumanRole_SkipsModelRegistration()
    {
        var manager = new ChatRoomManager();

        var mockProvider = new Mock<ILanguageModelProvider>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel>());
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "human-1",
            RoleName = "Human",
            IsHuman = true,
        });
        await manager.AddRoleAsync(role);

        Assert.HasCount(0, role.EndpointManager.GetSupportedModels());
        mockProvider.Verify(p => p.GetSupportedModels(), Times.Never);
    }

    [TestMethod]
    public async Task AddRoleAsync_NullRole_ThrowsArgumentNullException()
    {
        var manager = new ChatRoomManager();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => manager.AddRoleAsync(null!));
    }

    [TestMethod]
    public async Task RemoveRole_ExistingRole_RemovesFromRoles()
    {
        var manager = new ChatRoomManager();
        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());
        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        });
        await manager.AddRoleAsync(role);

        Assert.HasCount(1, manager.Roles);

        manager.RemoveRole("role-1");

        Assert.HasCount(0, manager.Roles);
    }

    [TestMethod]
    public async Task RemoveRole_NonExistentRole_DoesNotThrow()
    {
        var manager = new ChatRoomManager();

        manager.RemoveRole("non-existent");
    }

    [TestMethod]
    public void RemoveRole_EmptyRoleId_ThrowsArgumentException()
    {
        var manager = new ChatRoomManager();

        Assert.ThrowsExactly<ArgumentException>(() => manager.RemoveRole(""));
    }

    [TestMethod]
    public async Task RegisterRoleModelProviders_StoresProvidersForFutureAddRole()
    {
        var manager = new ChatRoomManager();

        var mockProvider = new Mock<ILanguageModelProvider>();
        var mockModel = new Mock<ILanguageModel>();
        mockProvider.Setup(p => p.GetSupportedModels())
            .Returns(new List<ILanguageModel> { mockModel.Object });

        manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>
        {
            ["test-provider"] = mockProvider.Object,
        });

        // AddRoleAsync after registration → new role should get models
        var role = new ChatRoomRole(new ChatRoomRoleDefinition
        {
            RoleId = "role-1",
            RoleName = "Test Role",
        });
        await manager.AddRoleAsync(role);

        Assert.HasCount(1, role.EndpointManager.GetSupportedModels());
    }

    #region 持久化往返测试

    private static string CreateTempPersistenceFolder()
    {
        return Path.Join(Path.GetTempPath(), "ChatRoomManagerTests", Path.GetRandomFileName());
    }

    private static void CleanupTempFolder(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [TestMethod]
    public async Task SaveAsync_ThenLoadAsync_RestoresRolesAndMessages()
    {
        // Arrange
        string tempFolder = CreateTempPersistenceFolder();
        try
        {
            var persistence = new ChatRoomPersistence(tempFolder);
            var session = new ChatRoomSession();
            var manager = new ChatRoomManager(session)
            {
                Persistence = persistence,
            };
            manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            var roleDef = new ChatRoomRoleDefinition
            {
                RoleId = "role-persist-1",
                RoleName = "持久化角色",
                SystemPrompt = "你是一个测试角色",
            };
            await manager.AddRoleAsync(new ChatRoomRole(roleDef));

            await manager.HumanInterjectAsync("你好", "human", "用户");

            // Act — 保存
            await manager.SaveAsync();

            // 用新的 manager 从磁盘加载
            var persistence2 = new ChatRoomPersistence(tempFolder);
            var loadedSession = new ChatRoomSession();
            var loadedManager = new ChatRoomManager(loadedSession)
            {
                Persistence = persistence2,
            };
            loadedManager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            await loadedManager.LoadAsync(session.SessionId.ToString("N"));

            // Assert — 角色已恢复
            Assert.HasCount(1, loadedManager.Roles);
            Assert.AreEqual("role-persist-1", loadedManager.Roles[0].Definition.RoleId);
            Assert.AreEqual("持久化角色", loadedManager.Roles[0].Definition.RoleName);
            Assert.AreEqual("你是一个测试角色", loadedManager.Roles[0].Definition.SystemPrompt);

            // Assert — 消息已恢复
            Assert.HasCount(1, loadedManager.Session.Messages);
            Assert.AreEqual("你好", loadedManager.Session.Messages[0].Content);
            Assert.IsTrue(loadedManager.Session.Messages[0].IsHumanMessage);
        }
        finally
        {
            CleanupTempFolder(tempFolder);
        }
    }

    [TestMethod]
    public async Task SaveAsync_ThenLoadAsync_RestoresAssistantMessageContent()
    {
        // Arrange
        string tempFolder = CreateTempPersistenceFolder();
        try
        {
            var persistence = new ChatRoomPersistence(tempFolder);
            var session = new ChatRoomSession();
            var manager = new ChatRoomManager(session)
            {
                Persistence = persistence,
            };
            manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            // 直接通过 AppendMessageAsync 添加一条 AI 消息（带 StaticContent）
            var aiMessage = ChatRoomMessage.CreateAssistant("AI 回复内容", "role-ai", "AI 角色");
            // 模拟流式完成后的状态：StaticContent 已设置
            await manager.HumanInterjectAsync("用户提问", "human", "用户");
            // 手动追加 AI 消息到 Session
            await session.AddMessageAsync(aiMessage);

            // Act — 保存
            await manager.SaveAsync();

            // 用新的 manager 从磁盘加载
            var persistence2 = new ChatRoomPersistence(tempFolder);
            var loadedSession = new ChatRoomSession();
            var loadedManager = new ChatRoomManager(loadedSession)
            {
                Persistence = persistence2,
            };
            loadedManager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            await loadedManager.LoadAsync(session.SessionId.ToString("N"));

            // Assert — AI 消息内容已恢复
            Assert.HasCount(2, loadedManager.Session.Messages);
            // 第一条是人类消息
            Assert.AreEqual("用户提问", loadedManager.Session.Messages[0].Content);
            // 第二条是 AI 消息
            Assert.AreEqual("AI 回复内容", loadedManager.Session.Messages[1].Content);
            Assert.AreEqual("role-ai", loadedManager.Session.Messages[1].SenderRoleId);
            Assert.AreEqual("AI 角色", loadedManager.Session.Messages[1].SenderRoleName);
        }
        finally
        {
            CleanupTempFolder(tempFolder);
        }
    }

    [TestMethod]
    public async Task AddRoleAsync_WithPersistence_SavesConfigToDisk()
    {
        // Arrange
        string tempFolder = CreateTempPersistenceFolder();
        try
        {
            var persistence = new ChatRoomPersistence(tempFolder);
            var manager = new ChatRoomManager
            {
                Persistence = persistence,
            };
            manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            var roleDef = new ChatRoomRoleDefinition
            {
                RoleId = "auto-save-role",
                RoleName = "自动保存角色",
            };
            await manager.AddRoleAsync(new ChatRoomRole(roleDef));

            // Act — 重新从磁盘加载，验证是否已自动持久化
            var persistence2 = new ChatRoomPersistence(tempFolder);
            var loadedManager = new ChatRoomManager
            {
                Persistence = persistence2,
            };
            loadedManager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            await loadedManager.LoadAsync(manager.Session.SessionId.ToString("N"));

            // Assert
            Assert.HasCount(1, loadedManager.Roles);
            Assert.AreEqual("auto-save-role", loadedManager.Roles[0].Definition.RoleId);
        }
        finally
        {
            CleanupTempFolder(tempFolder);
        }
    }

    [TestMethod]
    public async Task RemoveRole_WithPersistence_SavesConfigToDisk()
    {
        // Arrange
        string tempFolder = CreateTempPersistenceFolder();
        try
        {
            var persistence = new ChatRoomPersistence(tempFolder);
            var manager = new ChatRoomManager
            {
                Persistence = persistence,
            };
            manager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            var roleDef1 = new ChatRoomRoleDefinition
            {
                RoleId = "role-keep",
                RoleName = "保留角色",
            };
            var roleDef2 = new ChatRoomRoleDefinition
            {
                RoleId = "role-remove",
                RoleName = "删除角色",
            };
            await manager.AddRoleAsync(new ChatRoomRole(roleDef1));
            await manager.AddRoleAsync(new ChatRoomRole(roleDef2));

            // Act — 删除角色
            await manager.RemoveRoleAsync("role-remove");

            // 从磁盘重新加载
            var persistence2 = new ChatRoomPersistence(tempFolder);
            var loadedManager = new ChatRoomManager
            {
                Persistence = persistence2,
            };
            loadedManager.RegisterRoleModelProviders(new Dictionary<string, ILanguageModelProvider>());

            await loadedManager.LoadAsync(manager.Session.SessionId.ToString("N"));

            // Assert — 只保留了一个角色
            Assert.HasCount(1, loadedManager.Roles);
            Assert.AreEqual("role-keep", loadedManager.Roles[0].Definition.RoleId);
        }
        finally
        {
            CleanupTempFolder(tempFolder);
        }
    }

    [TestMethod]
    public async Task HumanInterjectAsync_WithPersistence_SavesConfigToDisk()
    {
        // Arrange
        string tempFolder = CreateTempPersistenceFolder();
        try
        {
            var persistence = new ChatRoomPersistence(tempFolder);
            var manager = new ChatRoomManager
            {
                Persistence = persistence,
            };

            // Act — 人类插话
            await manager.HumanInterjectAsync("测试持久化消息", "human", "用户");

            // 从磁盘重新加载
            var persistence2 = new ChatRoomPersistence(tempFolder);
            var loadedManager = new ChatRoomManager
            {
                Persistence = persistence2,
            };

            await loadedManager.LoadAsync(manager.Session.SessionId.ToString("N"));

            // Assert — 消息已持久化
            Assert.HasCount(1, loadedManager.Session.Messages);
            Assert.AreEqual("测试持久化消息", loadedManager.Session.Messages[0].Content);
        }
        finally
        {
            CleanupTempFolder(tempFolder);
        }
    }

    #endregion
}