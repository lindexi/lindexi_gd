using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    public interface IReadOnlyRunProperty : IEquatable<IReadOnlyRunProperty>
    {
        double FontSize { get; }
        FontName FontFamily { get; }

        /// <summary>
        /// 斜体表示，默认值为Normal
        /// </summary>
        FontStyle FontStyle { get; }

        /// <summary>
        /// 字的粗细度，默认值为Normal
        /// </summary>
        FontWeight FontWeight { get; }

        bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IImmutableRunPropertyValue? value);

        /// <summary>
        /// 构建新的属性
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IReadOnlyRunProperty BuildNewProperty(Action<RunProperty> action);
    }

    /// <summary>
    /// 表示一个不可变的对象值
    /// </summary>
    public interface IImmutableRunPropertyValue
    {
    }

    // todo 考虑属性系统支持设置是否影响布局，不影响布局的，例如改个颜色，可以不重新布局
    public class RunProperty : IReadOnlyRunProperty
    {
        public RunProperty()
        {
        }

        private RunProperty(RunProperty? styleRunProperty)
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

        public FontName FontFamily
        {
            set
            {
                _fontFamily = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontFamily ?? StyleRunProperty?.FontFamily ?? FontName.DefaultNotDefineFontName;
        }

        private FontName? _fontFamily;

        /// <summary>
        /// 斜体表示，默认值为Normal
        /// </summary>
        public FontStyle FontStyle
        {
            set
            {
                _fontStyle = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontStyle ?? StyleRunProperty?.FontStyle ?? FontStyle.DefaultNotDefine;
        }

        private FontStyle? _fontStyle;

        /// <summary>
        /// 字的粗细度，默认值为Normal
        /// </summary>
        public FontWeight FontWeight
        {
            set
            {
                _fontWeight = value;
                RaiseOnTextRunPropertyChanged();
            }
            get => _fontWeight ?? StyleRunProperty?.FontWeight ?? FontWeight.DefaultNotDefine;
        }

        private FontWeight? _fontWeight;

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
        private RunProperty? StyleRunProperty { get; }

        /// <summary>
        /// 构建新的属性
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IReadOnlyRunProperty BuildNewProperty(Action<RunProperty> action)
        {
            var runProperty = new RunProperty(this);
            action(runProperty);
            return runProperty;
        }

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

        public bool Equals(RunProperty other)
        {
            // 先判断一定存在的属性，再判断业务端注入的属性
            if (Equals(FontSize, other.FontSize) && Equals(FontFamily, other.FontFamily) &&
                Equals(FontStyle, other.FontStyle) && Equals(FontWeight, other.FontWeight))
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

        public bool Equals(IReadOnlyRunProperty? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other is RunProperty runProperty)
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