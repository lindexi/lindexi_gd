using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document
{
    /// <summary>
    /// 仅布局支持的文本字符属性
    /// </summary>
    // todo 考虑属性系统支持设置是否影响布局，不影响布局的，例如改个颜色，可以不重新布局
    public class LayoutOnlyRunProperty : IReadOnlyRunProperty
    {
        /// <summary>
        /// 创建仅布局支持的文本字符属性
        /// </summary>
        public LayoutOnlyRunProperty()
        {
        }

        /// <summary>
        /// 创建仅布局支持的文本字符属性
        /// </summary>
        /// <param name="styleRunProperty"></param>
        public LayoutOnlyRunProperty(LayoutOnlyRunProperty? styleRunProperty)
        {
            StyleRunProperty = styleRunProperty;
        }

        /// <summary>
        /// 属性继承的深度
        /// </summary>
        public int InheritDeepCount
        {
            get
            {
                var currentStyle = StyleRunProperty;
                var count = 0;
                while (currentStyle is not null)
                {
                    currentStyle = currentStyle.StyleRunProperty;

                    count++;
                }

                return count;
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// 额外的属性信息
        /// </summary>
        /// 这是在开始设计的时候，以为可以让此类型支持到业务端，然而实际使用却发现无法这么做。也许继续开发发现这个属性确实没有用时，可以考虑删除
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

        /// <summary>
        /// 是否存在任何的附加属性
        /// </summary>
        /// 这个属性用来提升性能，没有附加属性就不需要执行额外的判断逻辑
        private bool ExistsAnyAdditionalProperty
        {
            get
            {
                if (AdditionalPropertyDictionary != null)
                {
                    return true;
                }

                if (StyleRunProperty?.ExistsAnyAdditionalProperty is true)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 判断相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(LayoutOnlyRunProperty? other)
        {
            if(other is null) return false;

            // 先判断一定存在的属性，再判断业务端注入的属性
            if 
            (
                FontSize.Equals(other.FontSize)
                && FontName.Equals(other.FontName)
            )
            {
                if (ExistsAnyAdditionalProperty != other.ExistsAnyAdditionalProperty)
                {
                    // 如果一个存在附加属性，一个不存在，那就是不相等
                    return false;
                }

                if (!ExistsAnyAdditionalProperty)
                {
                    // 如果都不存在附加属性，那就不需要判断附加属性
                    return true;
                }

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

        /// <inheritdoc />
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