using DocumentFormat.OpenXml;
using dotnetCampus.OpenXMLUnitConverter;

namespace GakagaycalhechemNerehejejairairway
{
    /// <summary>
    /// 单位转换
    /// </summary>
    public static class OpenXMLUnitConverter
    {
        /// <summary>
        /// 转换为 EMU 格式
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Emu ToEmu(this Int64Value value, in Emu defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            return new Emu(value.Value);
        }

        /// <summary>
        /// 转换为 EMU 格式
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Emu ToEmu(this Int32Value value)
            => ToEmu(value, Emu.Zero);

        /// <summary>
        /// 转换为 EMU 格式
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Emu ToEmu(this Int32Value value, in Emu defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            return new Emu(value.Value);
        }
    }
}