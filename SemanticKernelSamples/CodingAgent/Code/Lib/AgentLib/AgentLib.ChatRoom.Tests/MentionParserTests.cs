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
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseMentions_NoMention_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("hello world", roles);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseMentions_SingleMention_ReturnsRoleId()
    {
        var roles = new[] { CreateRole("expert1", "代码专家") };
        var result = MentionParser.ParseMentions("@代码专家 帮我看看", roles);
        Assert.HasCount(1, result);
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
        Assert.HasCount(2, result);
        Assert.AreEqual("r1", result[0]);
        Assert.AreEqual("r2", result[1]);
    }

    [TestMethod]
    public void ParseMentions_DuplicateMention_ReturnsOnce()
    {
        var roles = new[] { CreateRole("r1", "专家") };
        var result = MentionParser.ParseMentions("@专家 和 @专家 再确认", roles);
        Assert.HasCount(1, result);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_CaseInsensitive()
    {
        var roles = new[] { CreateRole("r1", "Expert") };
        var result = MentionParser.ParseMentions("@expert help", roles);
        Assert.HasCount(1, result);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_BracketFormat_ReturnsRoleId()
    {
        var roles = new[] { CreateRole("r1", "Code Expert") };
        var result = MentionParser.ParseMentions("@[Code Expert] help", roles);
        Assert.HasCount(1, result);
        Assert.AreEqual("r1", result[0]);
    }

    [TestMethod]
    public void ParseMentions_UnknownRole_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("@Nobody here", roles);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseMentions_EmptyRoles_ReturnsEmpty()
    {
        var result = MentionParser.ParseMentions("@Hello", Array.Empty<ChatRoomRole>());
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ParseMentions_AtSymbolWithoutName_ReturnsEmpty()
    {
        var roles = new[] { CreateRole("r1", "Helper") };
        var result = MentionParser.ParseMentions("email@example.com test", roles);
        Assert.IsEmpty(result);
    }

    [TestMethod(DisplayName = "CanParseRoleName 正常中文名返回 true")]
    public void CanParseRoleName_ChineseName_ReturnsTrue()
    {
        Assert.IsTrue(MentionParser.CanParseRoleName("代码专家"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 正常英文名返回 true")]
    public void CanParseRoleName_EnglishName_ReturnsTrue()
    {
        Assert.IsTrue(MentionParser.CanParseRoleName("Helper"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 含数字名称返回 true")]
    public void CanParseRoleName_WithDigits_ReturnsTrue()
    {
        Assert.IsTrue(MentionParser.CanParseRoleName("Expert1"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 含连字符名称返回 true")]
    public void CanParseRoleName_WithHyphen_ReturnsTrue()
    {
        Assert.IsTrue(MentionParser.CanParseRoleName("Code-Expert"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 含下划线名称返回 true")]
    public void CanParseRoleName_WithUnderscore_ReturnsTrue()
    {
        Assert.IsTrue(MentionParser.CanParseRoleName("Code_Expert"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 含中点名称返回 true")]
    public void CanParseRoleName_WithMiddleDot_ReturnsTrue()
    {
        // 正则 \S 可匹配 · (U+00B7)，非贪婪 \S+? 会扩展到完整名称
        Assert.IsTrue(MentionParser.CanParseRoleName("白帽·信息员"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 含空格名称返回 false")]
    public void CanParseRoleName_WithSpace_ReturnsFalse()
    {
        // @Code Expert  中 \S+? 只匹配到 "Code" 就遇到空格，提取出 "Code" 而非 "Code Expert"
        Assert.IsFalse(MentionParser.CanParseRoleName("Code Expert"));
    }

    [TestMethod(DisplayName = "CanParseRoleName 空字符串抛出 ArgumentException")]
    public void CanParseRoleName_EmptyString_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => MentionParser.CanParseRoleName(""));
    }

    [TestMethod(DisplayName = "CanParseRoleName 空白字符串抛出 ArgumentException")]
    public void CanParseRoleName_WhitespaceString_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => MentionParser.CanParseRoleName("   "));
    }

    [TestMethod(DisplayName = "CanParseRoleName null 抛出 ArgumentNullException")]
    public void CanParseRoleName_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => MentionParser.CanParseRoleName(null!));
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