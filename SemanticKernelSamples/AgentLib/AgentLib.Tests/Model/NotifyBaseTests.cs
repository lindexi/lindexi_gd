using AgentLib.Model;

using System.ComponentModel;

namespace AgentLib.Tests.Model;

[TestClass]
public class NotifyBaseTests
{
    [TestMethod]
[Microsoft.VisualStudio.TestTools.UnitTesting.Description("设置新值时应更新字段并触发属性变更通知")]
    public void SetField_WhenValueChanges_UpdatesFieldAndRaisesPropertyChanged()
    {
        var model = new TestNotifyModel();
        PropertyChangedEventArgs? eventArgs = null;
        model.PropertyChanged += (_, args) => eventArgs = args;

        bool changed = model.SetValue("new-value");

        Assert.IsTrue(changed);
        Assert.AreEqual("new-value", model.Value);
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(nameof(TestNotifyModel.Value), eventArgs.PropertyName);
    }

    [TestMethod]
[Microsoft.VisualStudio.TestTools.UnitTesting.Description("设置相同值时不应触发属性变更通知")]
    public void SetField_WhenValueDoesNotChange_DoesNotRaisePropertyChanged()
    {
        var model = new TestNotifyModel { Value = "same" };
        bool raised = false;
        model.PropertyChanged += (_, _) => raised = true;

        bool changed = model.SetValue("same");

        Assert.IsFalse(changed);
        Assert.IsFalse(raised);
    }

    private sealed class TestNotifyModel : NotifyBase
    {
        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set => _value = value;
        }

        public bool SetValue(string value)
        {
            return SetField(ref _value, value, nameof(Value));
        }
    }
}
