using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;

using ChatRoom.AvaloniaShell.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatRoom.Shell.Tests;

/// <summary>
/// <see cref="RoleLobbyViewModel"/> 的角色添加行为测试。
/// </summary>
[TestClass]
public sealed class RoleLobbyViewModelTests
{
    private const string FirstTemplateId = "first-template";
    private const string SecondTemplateId = "second-template";
    private const string FirstRoleName = "测试角色一";
    private const string SecondRoleName = "测试角色二";

    private string _tempDirectory = string.Empty;
    private ChatRoomService _chatRoomService = null!;
    private RoleLobbyViewModel _viewModel = null!;

    /// <summary>
    /// 获取当前测试上下文。
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// 为每个测试创建独立会话和角色模板。
    /// </summary>
    [TestInitialize]
    public async Task SetupAsync()
    {
        _tempDirectory = Path.Join(Path.GetTempPath(), $"RoleLobbyViewModelTests_{Guid.NewGuid():N}");
        TestContext.WriteLine($"测试数据目录：{_tempDirectory}");

        string sessionsDirectory = Path.Join(_tempDirectory, "Sessions");
        var settings = new AppSettings
        {
            PersistencePath = sessionsDirectory,
        };
        _chatRoomService = new ChatRoomService(
            new TestMainThreadDispatcher(),
            new ModelProviderService(settings),
            sessionsDirectory);
        await _chatRoomService.CreateNewSessionAsync();

        var templateService = new RoleTemplateService(Path.Join(_tempDirectory, "RoleTemplates"));
        templateService.RegisterRuntimeTemplate(CreateTemplate(FirstTemplateId, FirstRoleName));
        templateService.RegisterRuntimeTemplate(CreateTemplate(SecondTemplateId, SecondRoleName));

        _viewModel = new RoleLobbyViewModel(templateService, _chatRoomService);
        _viewModel.RefreshTemplates();
    }

    /// <summary>
    /// 释放测试会话持有的运行时资源。
    /// </summary>
    [TestCleanup]
    public async Task CleanupAsync()
    {
        await _chatRoomService.DisposeAsync();
    }

    /// <summary>
    /// 添加角色后不应请求离开角色大厅。
    /// </summary>
    [TestMethod(DisplayName = "添加角色后应停留在角色大厅")]
    [Timeout(10000)]
    public async Task AddRoleShouldNotRequestLeavingLobby()
    {
        int backRequestedCount = 0;
        _viewModel.BackRequested += (_, _) => backRequestedCount++;

        await AddTemplateAsync(FirstTemplateId, expectedRoleCount: 1);

        Assert.AreEqual(0, backRequestedCount);
    }

    /// <summary>
    /// 用户应能在角色大厅连续添加多个不同角色。
    /// </summary>
    [TestMethod(DisplayName = "角色大厅应支持连续添加多个不同角色")]
    [Timeout(10000)]
    public async Task LobbyShouldAddMultipleDifferentRoles()
    {
        await AddTemplateAsync(FirstTemplateId, expectedRoleCount: 1);
        await AddTemplateAsync(SecondTemplateId, expectedRoleCount: 2);

        Assert.AreEqual(2, _chatRoomService.CurrentManager!.Roles.Count);
    }

    /// <summary>
    /// 重复添加同名角色时应显示错误提示，而不是让异常逸出。
    /// </summary>
    [TestMethod(DisplayName = "重复添加同名角色时应显示错误提示")]
    [Timeout(10000)]
    public async Task DuplicateRoleShouldShowErrorMessage()
    {
        await AddTemplateAsync(FirstTemplateId, expectedRoleCount: 1);
        RoleTemplateCardViewModel card = GetTemplateCard(FirstTemplateId);

        _viewModel.AddToSessionCommand.Execute(card);
        await WaitUntilAsync(() => _viewModel.HasAddToSessionErrorMessage);

        Assert.AreEqual($"角色“{FirstRoleName}”已在当前会话中。", _viewModel.AddToSessionErrorMessage);
    }

    private async Task AddTemplateAsync(string templateId, int expectedRoleCount)
    {
        RoleTemplateCardViewModel card = GetTemplateCard(templateId);
        _viewModel.AddToSessionCommand.Execute(card);

        await WaitUntilAsync(() => _chatRoomService.CurrentManager!.Roles.Count == expectedRoleCount);
        await WaitUntilAsync(() => _viewModel.AddToSessionCommand.CanExecute(card));
    }

    private RoleTemplateCardViewModel GetTemplateCard(string templateId)
    {
        return _viewModel.Templates.Single(card => card.TemplateId == templateId);
    }

    private static RoleTemplate CreateTemplate(string templateId, string roleName)
    {
        var now = DateTimeOffset.Now;
        return new RoleTemplate
        {
            TemplateId = templateId,
            Name = roleName,
            Description = $"{roleName}的模板",
            Category = "测试",
            CreatedAt = now,
            UpdatedAt = now,
            Definition = new ChatRoomRoleDefinition
            {
                RoleId = $"{templateId}-source-role",
                RoleName = roleName,
                IsHuman = true,
            },
        };
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed >= TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException("等待角色大厅异步操作完成超时。");
            }

            await Task.Delay(10);
        }
    }
}
