using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 只读的文本字符属性
    /// </summary>
    public interface IReadOnlyRunProperty : IEquatable<IReadOnlyRunProperty>
    {
        /// <summary>
        /// 字体大小
        /// </summary>
        /// 没有明确的属性，交给文本业务层。有些使用像素、有些使用磅
        double FontSize { get; }

        /// <summary>
        /// 用户设置的字体名
        /// </summary>
        /// 非底层找不到字体而进行回滚的字体
        /// 
        /// 在 Word 里面，可以同时设置一个文本 Run 的中文使用一个字体，西文使用一个字体
        /// 虽然 Word 这么做看起来不错，但是也存在设计无解的问题，例如西文字体的行高比中文字体的高
        /// 此时用户在输入中文，输入法先输入的是拼音，使用西文字体，此时行高变更，接着用户完成打字
        /// 输入法修改输入为中文，使用中文字体，于是行高再次变更，可以看到行高就在跳动
        /// 大部分的中文字体都有带英文字符，那不如就依然是单个字体
        FontName FontName { get; }

        ///// <summary>
        ///// 斜体表示，默认值为Normal
        ///// </summary>
        //FontStyle FontStyle { get; }

        ///// <summary>
        ///// 字的粗细度，默认值为Normal
        ///// </summary>
        //FontWeight FontWeight { get; }

        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IImmutableRunPropertyValue? value);
    }

    /// <summary>
    /// 平台相关的字符属性，用于给各个平台继承，实现其特定属性。要求此属性是不可变的
    /// </summary>
    public class PlatformImmutableRunProperty<T> : LayoutOnlyRunProperty
        where T : PlatformImmutableRunProperty<T>
    {
    }

    /// <summary>
    /// 用来限制某个类型不能被其他程序集继承
    /// </summary>
    /// [C# 如何写出一个不能被其他程序集继承的抽象类](https://blog.lindexi.com/post/C-%E5%A6%82%E4%BD%95%E5%86%99%E5%87%BA%E4%B8%80%E4%B8%AA%E4%B8%8D%E8%83%BD%E8%A2%AB%E5%85%B6%E4%BB%96%E7%A8%8B%E5%BA%8F%E9%9B%86%E7%BB%A7%E6%89%BF%E7%9A%84%E6%8A%BD%E8%B1%A1%E7%B1%BB.html )
    internal interface ICanNotInheritance
    {
    }

    // todo 考虑属性系统支持设置是否影响布局，不影响布局的，例如改个颜色，可以不重新布局
    public class LayoutOnlyRunProperty : IReadOnlyRunProperty
    {
        public LayoutOnlyRunProperty()
        {
        }

        public LayoutOnlyRunProperty(LayoutOnlyRunProperty? styleRunProperty)
        {
            StyleRunProperty = styleRunProperty;
        }

        public double FontSize
        {
            set
            {
                var valueToSet = value.CoerceValue(1, 65536);
                _fontSize = valueToSet;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontSize ?? StyleRunProperty?.FontSize ?? DefaultFontSize;
        }

        private double? _fontSize;

        private const double DefaultFontSize = 15;

        public FontName FontName
        {
            set
            {
                _fontFamily = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontFamily ?? StyleRunProperty?.FontName ?? FontName.DefaultNotDefineFontName;
        }

        private FontName? _fontFamily;

        ///// <summary>
        ///// 斜体表示，默认值为Normal
        ///// </summary>
        //public FontStyle FontStyle
        //{
        //    set
        //    {
        //        _fontStyle = value;
        //        RaiseOnTextRunPropertyChanged();
        //    }
        //    get => _fontStyle ?? StyleRunProperty?.FontStyle ?? FontStyle.DefaultNotDefine;
        //}

        //private FontStyle? _fontStyle;

        ///// <summary>
        ///// 字的粗细度，默认值为Normal
        ///// </summary>
        //public FontWeight FontWeight
        //{
        //    set
        //    {
        //        _fontWeight = value;
        //        RaiseOnTextRunPropertyChanged();
        //    }
        //    get => _fontWeight ?? StyleRunProperty?.FontWeight ?? FontWeight.DefaultNotDefine;
        //}

        //private FontWeight? _fontWeight;

        #region 附加属性

        /// <summary>
        /// 推荐放入的对象应该是不可变对象
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void SetProperty(string propertyName, IImmutableRunPropertyValue value)
        {
            AdditionalPropertyDictionary ??= new Dictionary<string, IImmutableRunPropertyValue>();

            AdditionalPropertyDictionary[propertyName] = value;
        }

        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IImmutableRunPropertyValue? value)
        {
            if (AdditionalPropertyDictionary?.TryGetValue(propertyName, out value!) is true)
            {
                return true;
            }

            if (StyleRunProperty?.TryGetProperty(propertyName, out value) is true)
            {
                return true;
            }

            value = default!;
            return false;
        }

        private Dictionary<string, IImmutableRunPropertyValue>? AdditionalPropertyDictionary { set; get; }

        #endregion

        #region 框架

        /// <summary>
        /// 继承样式里的属性
        /// </summary>
        private LayoutOnlyRunProperty? StyleRunProperty { get; }

        private void RaiseOnTextRunPropertyChanged()
        {
        }

        #endregion

        private HashSet<string> GetAdditionalPropertyKeyList()
        {
            var hashSet = new HashSet<string>();

            GetAdditionalPropertyKeyList(hashSet);

            return hashSet;
        }

        private void GetAdditionalPropertyKeyList(ISet<string> hashSet)
        {
            if (AdditionalPropertyDictionary != null)
            {
                foreach (var key in AdditionalPropertyDictionary.Keys)
                {
                    hashSet.Add(key);
                }
            }

            if (StyleRunProperty is not null)
            {
                // 不优化为一句话，方便打断点
                StyleRunProperty.GetAdditionalPropertyKeyList(hashSet);
            }
        }

        public bool Equals(LayoutOnlyRunProperty other)
        {
            // 先判断一定存在的属性，再判断业务端注入的属性
            if 
            (
                Equals(FontSize, other.FontSize)
                && Equals(FontName, other.FontName)

                //&& Equals(FontStyle, other.FontStyle) 
                //&& Equals(FontWeight, other.FontWeight)
            )
            {
                var thisAdditionalPropertyKeyList = GetAdditionalPropertyKeyList();
                var otherAdditionalPropertyKeyList = other.GetAdditionalPropertyKeyList();

                if (thisAdditionalPropertyKeyList.Count != otherAdditionalPropertyKeyList.Count
                    || !thisAdditionalPropertyKeyList.SetEquals(otherAdditionalPropertyKeyList))
                {
                    // 如果属性数量等都不同，那就返回不相同
                    return false;
                }

                foreach (var key in thisAdditionalPropertyKeyList)
                {
                    if (!TryGetProperty(key, out var thisValue))
                    {
                        // 理论上不可能
                        return false;
                    }

                    if (!other.TryGetProperty(key, out var otherValue))
                    {
                        // 理论上不可能
                        return false;
                    }

                    if (!Equals(thisValue, otherValue))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public virtual bool Equals(IReadOnlyRunProperty? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other is LayoutOnlyRunProperty runProperty)
            {
                return Equals(runProperty);
            }
            else
            {
                return false;
            }
        }
    }
}