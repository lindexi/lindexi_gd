using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class MentionParserTests
{
    [TestMethod]
    public void ParseMentions_EmptyContent_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Role1") };
        var result = MentionParser.ParseMentions("", roles);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ParseMentions_NoMention_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("hello world", roles);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ParseMentions_SingleMention_ReturnsRoleId()
    {
        var roles = new[] { CreateRole("expert1", "代码专家") };
        var result = MentionParser.ParseMentions("@代码专家 帮我看看", roles);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("expert1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_MultipleMentions_ReturnsByOrder()
    {
        var roles = new[]
        {
            CreateRole("r1", "专家A"),
            CreateRole("r2", "专家B"),
        };

        var result = MentionParser.ParseMentions("@专家A @专家B 一起看看", roles);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("r1", result[0]);
        Assert.AreEqual("r2", result[1]);
    }

    [TestMethod]
    public void ParseMentions_DuplicateMention_ReturnsOnce()
    {
        var roles = new[] { CreateRole("r1", "专家") };
        var result = MentionParser.ParseMentions("@专家 和 @专家 再确认", roles);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_CaseInsensitive()
    {
        var roles = new[] { CreateRole("r1", "Expert") };
        var result = MentionParser.ParseMentions("@expert help", roles);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_BracketFormat_ReturnsRoleId()
    {
        var roles = new[] { CreateRole("r1", "Code Expert") };
        var result = MentionParser.ParseMentions("@[Code Expert] help", roles);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_UnknownRole_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("@Nobody here", roles);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ParseMentions_EmptyRoles_ReturnsEmpty()
    {
        var result = MentionParser.ParseMentions("@Hello", Array.Empty<ChatRoomRole>());
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ParseMentions_AtSymbolWithoutName_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("email@example.com test", roles);
        Assert.AreEqual(0, result.Count);
    }

    private static ChatRoomRole CreateRole(string roleId, string roleName)
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = roleId,
            RoleName = roleName,
            IsHuman = false,
        };

        return new ChatRoomRole(definition);
    }
}