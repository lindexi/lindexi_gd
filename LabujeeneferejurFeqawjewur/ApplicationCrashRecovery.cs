using System;
using System.Runtime.InteropServices;

namespace LabujeeneferejurFeqawjewur
{
    public class ApplicationCrashRecovery
    {
        #region Delegates & Events
        private delegate int ApplicationRecoveryCallback(IntPtr pvParameter);
        public delegate void ApplicationCrashHandler();

        /// <summary>
        /// 监测到发生异常(包括崩溃、卡顿无响应等异常)
        /// </summary>
        public static event ApplicationCrashHandler OnApplicationCrashed;
        #endregion

        private static ApplicationRecoveryCallback _recoverApplication;

        /// <summary>
        /// 向WER注册应用程序重启机制
        /// </summary>
        /// <param name="pwsCommandLine">重启命令行参数</param>
        /// <param name="restartFlag">重启标志，参考RestartFlags</param>
        /// <returns>0表示成功</returns>
        public static int RegisterApplicationRestart(string pwsCommandLine, CrashRecoveryApi.RestartFlags restartFlag)
        {
            if (!IsWindowsVistaOrLater)
            {
                return -1;
            }
            var result = CrashRecoveryApi.RegisterApplicationRestart(pwsCommandLine, restartFlag);
            return result;
        }

        /// <summary>
        /// Registers the application for notification by windows of a failure.
        /// </summary>
        /// <returns>0 if successfully registered for restart notification</returns>
        public static uint RegisterApplicationRecoveryCallback()
        {
            if (!IsWindowsVistaOrLater)
            {
                return 1;
            }
            //  By default, the interval is 5 seconds (RECOVERY_DEFAULT_PING_INTERVAL). The maximum interval is 5 minutes. 
            _recoverApplication = new ApplicationRecoveryCallback(HandleApplicationCrash);
            IntPtr ptrOnApplicationCrash = Marshal.GetFunctionPointerForDelegate(_recoverApplication);
            var result = CrashRecoveryApi.RegisterApplicationRecoveryCallback(ptrOnApplicationCrash, IntPtr.Zero, (int)TimeSpan.FromMinutes(5).TotalMilliseconds,
                0);
            return result;
        }

        /// <returns>0 if successfully unRegistered for restart notification</returns>  
        public static uint UnRegisterForRestart()
        {
            if (!IsWindowsVistaOrLater)
            {
                return 1;
            }
            CrashRecoveryApi.UnregisterApplicationRecoveryCallback();
            var result = CrashRecoveryApi.UnregisterApplicationRestart();
            return result;
        }

        #region Data Persistance Methods
        private static bool IsWindowsVistaOrLater => Environment.OSVersion.Version.Major >= 6;

        /// <summary>
        /// This is the callback function that is executed in the event of the application crashing.
        /// </summary>
        /// <param name="pvParameter"></param>
        /// <returns></returns>
        private static int HandleApplicationCrash(IntPtr pvParameter)
        {
            //Allow the user to cancel the recovery.  The timer polls for that cancel.
            using (System.Threading.Timer t = new System.Threading.Timer(CheckForRecoveryCancel, null, 1000, 1000))
            {
                //Handle this event in your own code
                OnApplicationCrashed?.Invoke();

                CrashRecoveryApi.ApplicationRecoveryFinished(true);
            }

            return 0;
        }

        /// <summary>
        /// Checks to see if the user has cancelled the recovery.
        /// </summary>
        /// <param name="o"></param>
        private static void CheckForRecoveryCancel(object o)
        {
            CrashRecoveryApi.ApplicationRecoveryInProgress(out var userCancelled);

            if (userCancelled)
            {
                Environment.FailFast("User cancelled application recovery");
            }
        }
        #endregion
    }

    public class CrashRecoveryApi
    {
        [Flags]
        public enum RestartFlags
        {
            NONE = 0,
            RESTART_NO_CRASH = 1,
            RESTART_NO_HANG = 2,
            RESTART_NO_PATCH = 4,
            RESTART_NO_REBOOT = 8
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterApplicationRestart(string pwsCommandLine, RestartFlags dwFlags);

        [DllImport("kernel32.dll")]
        internal static extern uint RegisterApplicationRecoveryCallback(IntPtr pRecoveryCallback, IntPtr pvParameter, int dwPingInterval, int dwFlags);

        [DllImport("kernel32.dll")]
        internal static extern uint ApplicationRecoveryInProgress(out bool pbCancelled);

        [DllImport("kernel32.dll")]
        internal static extern uint ApplicationRecoveryFinished(bool bSuccess);

        [DllImport("kernel32.dll")]
        internal static extern uint UnregisterApplicationRecoveryCallback();

        [DllImport("kernel32.dll")]
        internal static extern uint UnregisterApplicationRestart();
    }
}