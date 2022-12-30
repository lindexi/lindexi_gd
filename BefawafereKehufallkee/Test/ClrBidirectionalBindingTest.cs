using System.ComponentModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

namespace BefawafereKehufallkee;

[TestClass]
public class ClrBidirectionalBindingTest
{
    [ContractTestCase]
    public void BindingTest()
    {
        "设置 A 和 B 的单向绑定，设置 TargetToSource 初始化，创建绑定完成，即将 B 属性的值赋值给到 A 上".Test(() =>
        {
            // 先给 B 一个初始值，即将 B 属性的值赋值给到 A 上
            var a = new A();

            var b = new B()
            {
                BProperty1 = Guid.NewGuid().ToString(),
            };

            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                BindingDirection.OneWay,
                initMode: BindingInitMode.TargetToSource
            );

            Assert.AreEqual(a.AProperty1, b.BProperty1);
        });

        "设置 A 和 B 的双向绑定，设置 TargetToSource 初始化，创建绑定完成，即将 B 属性的值赋值给到 A 上".Test(() =>
        {
            // 先给 B 一个初始值，即将 B 属性的值赋值给到 A 上
            var a = new A();

            var b = new B()
            {
                BProperty1 = Guid.NewGuid().ToString(),
            };

            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                initMode: BindingInitMode.TargetToSource
            );

            Assert.AreEqual(a.AProperty1, b.BProperty1);
        });

        "设置 A 和 B 的单向绑定，创建绑定完成，即将 A 属性的值赋值给到 B 上".Test(() =>
        {
            // 先给 A 一个初始值，用来测试是否 A 属性的值赋值给到 B 上
            var a = new A()
            {
                AProperty1 = Guid.NewGuid().ToString()
            };

            var b = new B();

            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                BindingDirection.OneWay
            );

            Assert.AreEqual(a.AProperty1, b.BProperty1);
        });

        "设置 A 和 B 的双向绑定，创建绑定完成，即将 A 属性的值赋值给到 B 上".Test(() =>
        {
            // 先给 A 一个初始值，用来测试是否 A 属性的值赋值给到 B 上
            var a = new A()
            {
                AProperty1 = Guid.NewGuid().ToString()
            };

            var b = new B();

            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1))
            );

            Assert.AreEqual(a.AProperty1, b.BProperty1);
        });

        "设置 A 和 B 的单向绑定，如果 B 绑定的属性没有 Set 方法，抛出 ArgumentNullException 异常".Test(() =>
        {
            var a = new A();
            var b = new B();

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                _ = new ClrBidirectionalBinding
                (
                    source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                    target: new ClrBindingPropertyContext(b, nameof(b.BPropertyWithoutSet)),
                    BindingDirection.OneWay
                );
            });
        });

        "设置 A 和 B 的单向绑定，如果 A 绑定的属性没有 Get 方法，抛出 ArgumentNullException 异常".Test(() =>
        {
            var a = new A();
            var b = new B();

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                _ = new ClrBidirectionalBinding
                (
                    source: new ClrBindingPropertyContext(a, nameof(a.APropertyWithoutGet)),
                    target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                    BindingDirection.OneWay
                );
            });
        });

        "设置 A 和 C 的双向绑定，如 C 不继承 INotifyPropertyChanged 接口，将抛出 ArgumentException 异常".Test(() =>
        {
            // 设置 A 和 C 的单向绑定
            var a = new A();
            var c = new C();

            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new ClrBidirectionalBinding
                (
                    source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                    target: new ClrBindingPropertyContext(c, nameof(c.CProperty1)),
                    BindingDirection.TwoWay
                );
            });
        });

        "设置 C 和 A 的单向绑定，如 C 不继承 INotifyPropertyChanged 接口，将抛出 ArgumentException 异常".Test(() =>
        {
            // 设置 C 和 A 的单向绑定
            var c = new C();
            var a = new A();

            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new ClrBidirectionalBinding
                (
                    source: new ClrBindingPropertyContext(c, nameof(c.CProperty1)),
                    target: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                    BindingDirection.OneWay
                );
            });
        });

        "设置 A 和 C 的单向绑定，可以让 C 不继承 INotifyPropertyChanged 接口".Test(() =>
        {
            // 设置 A 和 C 的单向绑定
            var a = new A();
            var c = new C();

            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(c, nameof(c.CProperty1)),
                BindingDirection.OneWay
            );

            // 可以让 C 不继承 INotifyPropertyChanged 接口
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, c.CProperty1);
        });

        "设置 A 和 B 的两个属性绑定，两个属性之间的更新互不影响".Test(() =>
        {
            // 设置 A 和 B 两个属性绑定
            var a = new A();
            var b = new B();
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1))
            );
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty2)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty2))
            );

            // 两个属性之间的更新互不影响
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);

            a.AProperty2 = 10;
            Assert.AreEqual(a.AProperty2, b.BProperty2);

            value = Guid.NewGuid().ToString();
            b.BProperty1 = value;
            Assert.AreEqual(value, a.AProperty1);

            b.BProperty2 = 100;
            Assert.AreEqual(b.BProperty2, a.AProperty2);
        });

        "设置 A 和 B 属性绑定，更改非绑定属性，不会影响原有的属性".Test(() =>
        {
            // 设置 A 和 B 属性绑定
            var a = new A();
            var b = new B();
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1))
            );

            // 更改非绑定属性，不会影响原有的属性
            a.AProperty2 = 10;

            Assert.AreEqual(string.Empty, b.BProperty1);
            Assert.AreEqual(0, b.BProperty2);

            // 更新属性之后，依然也是不会影响
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;
            Assert.AreEqual(value, b.BProperty1);

            a.AProperty2 = 20;
            Assert.AreEqual(value, b.BProperty1);
            Assert.AreEqual(0, b.BProperty2);
        });

        "给 A 和 B 两个对象设置双向绑定，无论哪个对象更改，都能更新".Test(() =>
        {
            // 给 A 和 B 两个对象设置双向绑定
            var a = new A();
            var b = new B();
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                BindingDirection.TwoWay
            );

            // 无论哪个对象更改，都能更新
            // 先设置 A 的属性，再测试设置 B 的属性
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);

            // 多次设置
            value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);

            value = Guid.NewGuid().ToString();
            b.BProperty1 = value;
            Assert.AreEqual(value, a.AProperty1);

            // 多次设置
            value = Guid.NewGuid().ToString();
            b.BProperty1 = value;
            Assert.AreEqual(value, a.AProperty1);

            // 在 B 设置完成之后，再次设置 A 的属性
            value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);
        });

        "给 A 和 B 两个对象设置绑定，没有给定默认的赋值和获取值委托，可以自动生成".Test(() =>
        {
            // 给 A 和 B 两个对象设置绑定，没有给定默认的赋值和获取值委托
            var a = new A();
            var b = new B();

            // 给 A 和 B 两个对象设置单向绑定
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1)),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1)),
                BindingDirection.OneWay
            );

            // 可以自动生成，等于绑定生效
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);
        });

        "给 A 和 B 两个对象设置单向绑定，可以在 A 更新时，给 B 赋值；在 B 更新时，啥都不做".Test(() =>
        {
            var a = new A();
            var b = new B();

            // 给 A 和 B 两个对象设置单向绑定
            _ = new ClrBidirectionalBinding
            (
                source: new ClrBindingPropertyContext(a, nameof(a.AProperty1),
                    propertyGetter: bindableObject => ((A) bindableObject).AProperty1),
                target: new ClrBindingPropertyContext(b, nameof(b.BProperty1),
                    propertySetter: (bindableObject, value) => ((B) bindableObject).BProperty1 = (string) value!),
                BindingDirection.OneWay
            );

            // 可以在 A 更新时，给 B 赋值
            var value = Guid.NewGuid().ToString();
            a.AProperty1 = value;

            Assert.AreEqual(value, b.BProperty1);

            // 在 B 更新时，啥都不做
            b.BProperty1 = Guid.NewGuid().ToString();
            Assert.AreEqual(value, a.AProperty1);
        });
    }
}