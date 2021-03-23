using System;
using System.Reflection;
using System.Windows.Input;

namespace DernijacallqaNaycerejerlal
{
    /// <summary>
    /// Based on https://msdn.microsoft.com/en-us/library/vstudio/dd901337(v=vs.90).aspx
    /// </summary>
    public static class TabletHelper
    {
        public static bool HasRemovedDevices { get; private set; }

        public static void DisableWPFTabletSupport(IntPtr hWnd)
        {
            var inputManagerType = typeof(InputManager);

            var stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, InputManager.Current, null);

            Type stylusLogicType;
            FieldInfo countField452;
            if (stylusLogic != null)
            {
                stylusLogicType = stylusLogic.GetType();
                countField452 = stylusLogicType.GetField("_lastSeenDeviceCount",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            }
            else
            {
                return;
            }

            while (Tablet.TabletDevices.Count > 0)
            {
                // Only in .Net Framework 4.5.2 - see https://connect.microsoft.com/VisualStudio/Feedback/Details/1016534
                if (countField452 != null)
                {
                    countField452.SetValue(stylusLogic, 1 + (int) countField452.GetValue(stylusLogic));
                }

                int index = Tablet.TabletDevices.Count - 1;

                stylusLogicType.InvokeMember("OnTabletRemoved",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, stylusLogic, new object[] { (uint) index });

                HasRemovedDevices = true;
            }
        }
    }
}