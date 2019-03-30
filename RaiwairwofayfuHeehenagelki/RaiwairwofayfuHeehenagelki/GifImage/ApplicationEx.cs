using System.Collections.Generic;
using System.Linq;

namespace RaiwairwofayfuHeehenagelki.GifImage
{

    /// <summary>
    ///     应用程序扩展(Application Extension)-这是提供给应用程序自己使用的
    ///     （需要89a版本），应用程序可以在这里定义自己的标识、信息等
    /// </summary>
    internal struct ApplicationEx
    {
        #region 结构字段  

        /// <summary>
        ///     Block Size - 块大小，固定值11
        /// </summary>
        internal const byte BlockSize = 0X0B;

        /// <summary>
        ///     Application Identifier - 用来鉴别应用程序自身的标识(8个连续ASCII字符)
        /// </summary>
        internal char[] ApplicationIdentifier;

        /// <summary>
        ///     Application Authentication Code - 应用程序定义的特殊标识码(3个连续ASCII字符)
        /// </summary>
        internal char[] ApplicationAuthenticationCode;

        /// <summary>
        ///     应用程序自定义数据块 - 一个或多个数据块组成，保存应用程序自己定义的数据
        /// </summary>
        internal List<DataStruct> Data;

        #endregion

        #region 方法函数

        /// <summary>
        ///     获取应用程序扩展的字节数组
        /// </summary>
        /// <returns></returns>
        internal byte[] GetBuffer()
        {
            var list = new List<byte>
            {
                GifExtensions.ExtensionIntroducer, GifExtensions.ApplicationExtensionLabel, BlockSize
            };
            if (ApplicationIdentifier == null)
            {
                ApplicationIdentifier = "NETSCAPE".ToCharArray();
            }

            list.AddRange(ApplicationIdentifier.Select(c => (byte) c));

            if (ApplicationAuthenticationCode == null)
            {
                ApplicationAuthenticationCode = "2.0".ToCharArray();
            }

            list.AddRange(ApplicationAuthenticationCode.Select(c => (byte) c));

            if (Data != null)
            {
                foreach (var ds in Data)
                {
                    list.AddRange(ds.GetBuffer());
                }
            }

            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }

        #endregion
    }

}