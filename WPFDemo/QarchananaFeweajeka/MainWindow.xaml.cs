using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Forms.Clipboard;

namespace QarchananaFeweajeka;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();

        var keyboardHook = new KeyboardHook();
        _keyboardHook = keyboardHook;

        //Loaded += MainWindow_Loaded;
    }

    private readonly KeyboardHook _keyboardHook;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _keyboardHook.KeyUpEvent += KeyboardHook_KeyUpEvent;
        _keyboardHook.Start();
    }

    private void KeyboardHook_KeyUpEvent(object? sender, Keys e)
    {
        if (e == Keys.Enter)
        {
            _keyboardHook.Stop();
            SendKeys.SendWait("%=");
            SendKeys.SendWait("^v");
            SendKeys.SendWait("{Enter}");
            _keyboardHook.Start();
        }
    }

    private async void SendButton_OnClick(object sender, RoutedEventArgs e)
    {
        await Task.Delay(3000);
        SendKeys.SendWait("%=");
        Clipboard.SetText("a^2+b^2=c^2");
        SendKeys.SendWait("^v");
        SendKeys.SendWait("{Enter}");
    }
}

class KeyboardHook
    {
        #region win32
        public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        static int hKeyboardHook = 0; //声明键盘钩子处理的初始值
        public const int WH_KEYBOARD_LL = 13;   //线程键盘钩子监听鼠标消息设为2，全局键盘监听鼠标消息设为13
        HookProc KeyboardHookProcedure; //声明KeyboardHookProcedure作为HookProc类型

        //键盘结构
        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            public int vkCode;  //定一个虚拟键码。该代码必须有一个价值的范围1至254
            public int scanCode; // 指定的硬件扫描码的关键
            public int flags;  // 键标志
            public int time; // 指定的时间戳记的这个讯息
            public int dwExtraInfo; // 指定额外信息相关的信息
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public InputType dwType;
            public KEYBDINPUT ki;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct KEYBDINPUT
        {
            public Int16 wVk;
            public Int16 wScan;
            public KEYEVENTF dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        public enum KEYEVENTF
        {
            EXTENDEDKEY = 1,
            KEYUP = 2,
            UNICODE = 4,
            SCANCODE = 8,
        }

        //使用此功能，安装了一个钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);


        //调用此函数卸载钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);


        //使用此功能，通过信息钩子继续下一个钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);


        // 取得当前线程编号（线程钩子需要用到）
        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();


        //使用WINDOWS API函数代替获取当前实例的函数,防止钩子失效
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);




        private const int WM_KEYDOWN = 0x100;//KEYDOWN
        private const int WM_KEYUP = 0x101;//KEYUP
        private const int WM_SYSKEYDOWN = 0x104;//SYSKEYDOWN
        private const int WM_SYSKEYUP = 0x105;//SYSKEYUP

        //ToAscii职能的转换指定的虚拟键码和键盘状态的相应字符或字符
        [DllImport("user32")]
        public static extern int ToAscii(int uVirtKey, //[in] 指定虚拟关键代码进行翻译。
            int uScanCode, // [in] 指定的硬件扫描码的关键须翻译成英文。高阶位的这个值设定的关键，如果是（不压）
            byte[] lpbKeyState, // [in] 指针，以256字节数组，包含当前键盘的状态。每个元素（字节）的数组包含状态的一个关键。如果高阶位的字节是一套，关键是下跌（按下）。在低比特，如果设置表明，关键是对切换。在此功能，只有肘位的CAPS LOCK键是相关的。在切换状态的NUM个锁和滚动锁定键被忽略。
            byte[] lpwTransKey, // [out] 指针的缓冲区收到翻译字符或字符。
            int fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise.

        //获取按键的状态
        [DllImport("user32")]
        public static extern int GetKeyboardState(byte[] pbKeyState);


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs,
            [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
            int cbSize);

        #endregion


        public event EventHandler<Keys> KeyUpEvent;
        public event Action<int> OnSpaced;
        public event Action OnBacked;
        public event Action<int> OnPaged;

        public void Start()
        {
            // 安装键盘钩子
            if (hKeyboardHook == 0)
            {
                KeyboardHookProcedure = new HookProc(KeyboardHookProc);

                hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProcedure, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);

                //如果SetWindowsHookEx失败
                if (hKeyboardHook == 0)
                {
                    Stop();
                    throw new Exception("安装键盘钩子失败");
                }
            }
        }
        public void Stop()
        {
            bool retKeyboard = true;


            if (hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
            }

            if (!retKeyboard)
            {
                throw new Exception("卸载钩子失败！");
            }
        }

        public void Send(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                Stop();
                SendKeys.SendWait(msg);
                Start();
            }
        }

        bool isLocked = true;

        public bool IsStarted { set; get; }

        /// <summary>
        /// 按键处理
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns>如果返回1，则结束消息，这个消息到此为止，不再传递。如果返回0或调用CallNextHookEx函数则消息出了这个钩子继续往下传递，也就是传给消息真正的接受者</returns>
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            // 侦听键盘事件
            if (nCode >= 0 && wParam == 0x0100)
            {
                KeyboardHookStruct myKeyboardHookStruct = (KeyboardHookStruct) Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

                if (myKeyboardHookStruct.vkCode == 27)
                {
                    Environment.Exit(1);
                }

                #region
                if (isLocked)
                {
                    #region page
                    if (myKeyboardHookStruct.vkCode == 33)
                    {
                        OnPaged?.Invoke(-1);
                    }
                    if (myKeyboardHookStruct.vkCode == 34)
                    {
                        OnPaged?.Invoke(1);
                    }
                    #endregion

                    if (IsStarted && myKeyboardHookStruct.vkCode >= 48 && myKeyboardHookStruct.vkCode <= 57)
                    {
                        var c = int.Parse(((char) myKeyboardHookStruct.vkCode).ToString());
                        OnSpaced?.Invoke(c);
                        IsStarted = false;
                        return 1;
                    }
                    if (IsStarted && myKeyboardHookStruct.vkCode == 8)
                    {
                        OnBacked?.Invoke();

                        return IsStarted ? 1 : 0;
                    }
                    if ((myKeyboardHookStruct.vkCode >= 65 && myKeyboardHookStruct.vkCode <= 90) || myKeyboardHookStruct.vkCode == 32)
                    {
                        if (myKeyboardHookStruct.vkCode >= 65 && myKeyboardHookStruct.vkCode <= 90)
                        {
                            Keys keyData = (Keys) myKeyboardHookStruct.vkCode;
                            KeyUpEvent?.Invoke(this, keyData);
                            IsStarted = true;
                        }
                        if (myKeyboardHookStruct.vkCode == 32)
                        {
                            IsStarted = true;
                            OnSpaced?.Invoke(0);
                        }
                        return IsStarted ? 1 : 0;
                    }
                    else
                    {
                        return 0;
                    }
                }
                #endregion
            }
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }
    }

