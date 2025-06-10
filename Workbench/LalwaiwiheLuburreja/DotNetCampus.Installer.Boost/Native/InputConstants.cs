using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Boost;

public static partial class Win32
{
    [Flags]
    public enum KeyModifierFlags
    {
        /// <summary>
        ///     Either ALT key must be held down.
        /// </summary>
        MOD_ALT = 0x0001,

        /// <summary>
        ///     Either CTRL key must be held down.
        /// </summary>
        MOD_CONTROL = 0x0002,

        /// <summary>
        ///     Changes the hotkey behavior so that the keyboard auto-repeat does not yield multiple hotkey notifications.
        ///     Windows Vista:  This flag is not supported.
        /// </summary>
        MOD_NOREPEAT = 0x4000,

        /// <summary>
        ///     Either SHIFT key must be held down.
        /// </summary>
        MOD_SHIFT = 0x0004,

        /// <summary>
        ///     Either WINDOWS key was held down. These keys are labeled with the Windows logo. Keyboard shortcuts that involve the
        ///     WINDOWS key are reserved for use by the operating system.
        /// </summary>
        MOD_WIN = 0x0008
    }

    [Flags]
    public enum MouseInputKeyStateFlags
    {
        /// <summary>
        ///     The CTRL key is down.
        /// </summary>
        MK_CONTROL = 0x0008,

        /// <summary>
        ///     The left mouse button is down.
        /// </summary>
        MK_LBUTTON = 0x0001,

        /// <summary>
        ///     The middle mouse button is down.
        /// </summary>
        MK_MBUTTON = 0x0010,

        /// <summary>
        ///     The right mouse button is down.
        /// </summary>
        MK_RBUTTON = 0x0002,

        /// <summary>
        ///     The SHIFT key is down.
        /// </summary>
        MK_SHIFT = 0x0004,

        /// <summary>
        ///     The first X button is down.
        /// </summary>
        MK_XBUTTON1 = 0x0020,

        /// <summary>
        ///     The second X button is down.
        /// </summary>
        MK_XBUTTON2 = 0x0040
    }

    [Flags]
    public enum HotKeyInputState
    {
        /// <summary>
        ///     Either ALT key was held down.
        /// </summary>
        MOD_ALT = 0x0001,

        /// <summary>
        ///     Either CTRL key was held down.
        /// </summary>
        MOD_CONTROL = 0x0002,

        /// <summary>
        ///     Either SHIFT key was held down.
        /// </summary>
        MOD_SHIFT = 0x0004,

        /// <summary>
        ///     Either WINDOWS key was held down. These keys are labeled with the Windows logo. Hotkeys that involve the Windows
        ///     key are reserved for use by the operating system.
        /// </summary>
        MOD_WIN = 0x0008
    }

    [Flags]
    public enum MouseInputXButtonFlag
    {
        /// <summary>
        ///     The first X button was clicked.
        /// </summary>
        XBUTTON1 = 0x0001,

        /// <summary>
        ///     The second X button was clicked.
        /// </summary>
        XBUTTON2 = 0x0002
    }

    [Flags]
    public enum MouseInputFlags
    {
        /// <summary>
        ///     The dx and dy members contain normalized absolute coordinates. If the flag is not set, dxand dy contain relative
        ///     data (the change in position since the last reported position). This flag can be set, or not set, regardless of
        ///     what kind of mouse or other pointing device, if any, is connected to the system. For further information about
        ///     relative mouse motion, see the following Remarks section.
        /// </summary>
        MOUSEEVENTF_ABSOLUTE = 0x8000,

        /// <summary>
        ///     The wheel was moved horizontally, if the mouse has a wheel. The amount of movement is specified in mouseData.
        ///     Windows XP/2000:  This value is not supported.
        /// </summary>
        MOUSEEVENTF_HWHEEL = 0x01000,

        /// <summary>
        ///     Movement occurred.
        /// </summary>
        MOUSEEVENTF_MOVE = 0x0001,

        /// <summary>
        ///     The WM_MOUSEMOVE messages will not be coalesced. The default behavior is to coalesce WM_MOUSEMOVE messages.
        ///     Windows XP/2000:  This value is not supported.
        /// </summary>
        MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,

        /// <summary>
        ///     The left button was pressed.
        /// </summary>
        MOUSEEVENTF_LEFTDOWN = 0x0002,

        /// <summary>
        ///     The left button was released.
        /// </summary>
        MOUSEEVENTF_LEFTUP = 0x0004,

        /// <summary>
        ///     The right button was pressed.
        /// </summary>
        MOUSEEVENTF_RIGHTDOWN = 0x0008,

        /// <summary>
        ///     The right button was released.
        /// </summary>
        MOUSEEVENTF_RIGHTUP = 0x0010,

        /// <summary>
        ///     The middle button was pressed.
        /// </summary>
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,

        /// <summary>
        ///     The middle button was released.
        /// </summary>
        MOUSEEVENTF_MIDDLEUP = 0x0040,

        /// <summary>
        ///     Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
        /// </summary>
        MOUSEEVENTF_VIRTUALDESK = 0x4000,

        /// <summary>
        ///     The wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData.
        /// </summary>
        MOUSEEVENTF_WHEEL = 0x0800,

        /// <summary>
        ///     An X button was pressed.
        /// </summary>
        MOUSEEVENTF_XDOWN = 0x0080,

        /// <summary>
        ///     An X button was released.
        /// </summary>
        MOUSEEVENTF_XUP = 0x0100
    }

    [Flags]
    public enum KeyboardInputFlags
    {
        /// <summary>
        ///     If specified, the scan code was preceded by a prefix byte that has the value 0xE0 (224).
        /// </summary>
        KEYEVENTF_EXTENDEDKEY = 0x0001,

        /// <summary>
        ///     If specified, the key is being released. If not specified, the key is being pressed.
        /// </summary>
        KEYEVENTF_KEYUP = 0x0002,

        /// <summary>
        ///     If specified, wScan identifies the key and wVk is ignored.
        /// </summary>
        KEYEVENTF_SCANCODE = 0x0008,

        /// <summary>
        ///     If specified, the system synthesizes a VK_PACKET keystroke. The wVk parameter must be zero. This flag can only be
        ///     combined with the KEYEVENTF_KEYUP flag. For more information, see the Remarks section.
        /// </summary>
        KEYEVENTF_UNICODE = 0x0004
    }

    [Flags]
    public enum CursorInfoFlags
    {
        /// <summary>
        ///     The cursor is showing.
        /// </summary>
        CURSOR_SHOWING = 0x00000001,

        /// <summary>
        ///     Windows 8: The cursor is suppressed. This flag indicates that the system is not drawing the cursor because the user
        ///     is providing input through touch or pen instead of the mouse.
        /// </summary>
        CURSOR_SUPPRESSED = 0x00000002
    }

    public enum InputType
    {
        /// <summary>
        ///     The event is a mouse event. Use the mi structure of the union.
        /// </summary>
        INPUT_MOUSE = 0,

        /// <summary>
        ///     The event is a keyboard event. Use the ki structure of the union.
        /// </summary>
        INPUT_KEYBOARD = 1,

        /// <summary>
        ///     The event is a hardware event. Use the hi structure of the union.
        /// </summary>
        INPUT_HARDWARE = 2
    }

    public enum VirtualKeyMapType
    {
        /// <summary>
        ///     The uCode parameter is a virtual-key code and is translated into an unshifted character value in the low order word
        ///     of the return value. Dead keys (diacritics) are indicated by setting the top bit of the return value. If there is
        ///     no translation, the function returns 0.
        /// </summary>
        MAPVK_VK_TO_CHAR = 2,

        /// <summary>
        ///     The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does
        ///     not distinguish between left- and right-hand keys, the left-hand scan code is returned. If there is no translation,
        ///     the function returns 0.
        /// </summary>
        MAPVK_VK_TO_VSC = 0,

        /// <summary>
        ///     The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does
        ///     not distinguish between left- and right-hand keys, the left-hand scan code is returned. If the scan code is an
        ///     extended scan code, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan
        ///     code. If there is no translation, the function returns 0.
        /// </summary>
        MAPVK_VK_TO_VSC_EX = 4,

        /// <summary>
        ///     The uCode parameter is a scan code and is translated into a virtual-key code that does not distinguish between
        ///     left- and right-hand keys. If there is no translation, the function returns 0.
        /// </summary>
        MAPVK_VSC_TO_VK = 1,

        /// <summary>
        ///     The uCode parameter is a scan code and is translated into a virtual-key code that distinguishes between left- and
        ///     right-hand keys. If there is no translation, the function returns 0.
        /// </summary>
        MAPVK_VSC_TO_VK_EX = 3
    }

    /// <summary>
    /// Copy from https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Keys.cs
    /// </summary>
    public enum VirtualKey
    {
        /// <summary>
        ///    <para>
        ///       The bit mask to extract a key code from a key value.
        ///       
        ///    </para>
        /// </summary>
        KeyCode = 0x0000FFFF,


        /// <summary>
        ///    <para>
        ///       The bit mask to extract modifiers from a key value.
        ///       
        ///    </para>
        /// </summary>
        Modifiers = unchecked((int) 0xFFFF0000),


        /// <summary>
        ///    <para>
        ///       No key pressed.
        ///    </para>
        /// </summary>
        None = 0x00,


        /// <summary>
        ///    <para>
        ///       The left mouse button.
        ///       
        ///    </para>
        /// </summary>
        LButton = 0x01,

        /// <summary>
        ///    <para>
        ///       The right mouse button.
        ///    </para>
        /// </summary>
        RButton = 0x02,

        /// <summary>
        ///    <para>
        ///       The CANCEL key.
        ///    </para>
        /// </summary>
        Cancel = 0x03,

        /// <summary>
        ///    <para>
        ///       The middle mouse button (three-button mouse).
        ///    </para>
        /// </summary>
        MButton = 0x04,

        /// <summary>
        ///    <para>
        ///       The first x mouse button (five-button mouse).
        ///    </para>
        /// </summary>
        XButton1 = 0x05,

        /// <summary>
        ///    <para>
        ///       The second x mouse button (five-button mouse).
        ///    </para>
        /// </summary>
        XButton2 = 0x06,

        /// <summary>
        ///    <para>
        ///       The BACKSPACE key.
        ///    </para>
        /// </summary>
        Back = 0x08,

        /// <summary>
        ///    <para>
        ///       The TAB key.
        ///    </para>
        /// </summary>
        Tab = 0x09,

        /// <summary>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </summary>
        LineFeed = 0x0A,

        /// <summary>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </summary>
        Clear = 0x0C,

        /// <summary>
        ///    <para>
        ///       The RETURN key.
        ///
        ///    </para>
        /// </summary>
        Return = 0x0D,

        /// <summary>
        ///    <para>
        ///       The ENTER key.
        ///       
        ///    </para>
        /// </summary>
        Enter = Return,

        /// <summary>
        ///    <para>
        ///       The SHIFT key.
        ///    </para>
        /// </summary>
        ShiftKey = 0x10,

        /// <summary>
        ///    <para>
        ///       The CTRL key.
        ///    </para>
        /// </summary>
        ControlKey = 0x11,

        /// <summary>
        ///    <para>
        ///       The ALT key.
        ///    </para>
        /// </summary>
        Menu = 0x12,

        /// <summary>
        ///    <para>
        ///       The PAUSE key.
        ///    </para>
        /// </summary>
        Pause = 0x13,

        /// <summary>
        ///    <para>
        ///       The CAPS LOCK key.
        ///
        ///    </para>
        /// </summary>
        Capital = 0x14,

        /// <summary>
        ///    <para>
        ///       The CAPS LOCK key.
        ///    </para>
        /// </summary>
        CapsLock = 0x14,

        /// <summary>
        ///    <para>
        ///       The IME Kana mode key.
        ///    </para>
        /// </summary>
        KanaMode = 0x15,

        /// <summary>
        ///    <para>
        ///       The IME Hanguel mode key.
        ///    </para>
        /// </summary>
        HanguelMode = 0x15,

        /// <summary>
        ///    <para>
        ///       The IME Hangul mode key.
        ///    </para>
        /// </summary>
        HangulMode = 0x15,

        /// <summary>
        ///    <para>
        ///       The IME Junja mode key.
        ///    </para>
        /// </summary>
        JunjaMode = 0x17,

        /// <summary>
        ///    <para>
        ///       The IME Final mode key.
        ///    </para>
        /// </summary>
        FinalMode = 0x18,

        /// <summary>
        ///    <para>
        ///       The IME Hanja mode key.
        ///    </para>
        /// </summary>
        HanjaMode = 0x19,

        /// <summary>
        ///    <para>
        ///       The IME Kanji mode key.
        ///    </para>
        /// </summary>
        KanjiMode = 0x19,

        /// <summary>
        ///    <para>
        ///       The ESC key.
        ///    </para>
        /// </summary>
        Escape = 0x1B,

        /// <summary>
        ///    <para>
        ///       The IME Convert key.
        ///    </para>
        /// </summary>
        IMEConvert = 0x1C,

        /// <summary>
        ///    <para>
        ///       The IME NonConvert key.
        ///    </para>
        /// </summary>
        IMENonconvert = 0x1D,

        /// <summary>
        ///    <para>
        ///       The IME Accept key.
        ///    </para>
        /// </summary>
        IMEAccept = 0x1E,

        /// <summary>
        ///    <para>
        ///       The IME Accept key.
        ///    </para>
        /// </summary>
        IMEAceept = IMEAccept,

        /// <summary>
        ///    <para>
        ///       The IME Mode change request.
        ///    </para>
        /// </summary>
        IMEModeChange = 0x1F,

        /// <summary>
        ///    <para>
        ///       The SPACEBAR key.
        ///    </para>
        /// </summary>
        Space = 0x20,

        /// <summary>
        ///    <para>
        ///       The PAGE UP key.
        ///    </para>
        /// </summary>
        Prior = 0x21,

        /// <summary>
        ///    <para>
        ///       The PAGE UP key.
        ///    </para>
        /// </summary>
        PageUp = Prior,

        /// <summary>
        ///    <para>
        ///       The PAGE DOWN key.
        ///    </para>
        /// </summary>
        Next = 0x22,

        /// <summary>
        ///    <para>
        ///       The PAGE DOWN key.
        ///    </para>
        /// </summary>
        PageDown = Next,

        /// <summary>
        ///    <para>
        ///       The END key.
        ///    </para>
        /// </summary>
        End = 0x23,

        /// <summary>
        ///    <para>
        ///       The HOME key.
        ///    </para>
        /// </summary>
        Home = 0x24,

        /// <summary>
        ///    <para>
        ///       The LEFT ARROW key.
        ///    </para>
        /// </summary>
        Left = 0x25,

        /// <summary>
        ///    <para>
        ///       The UP ARROW key.
        ///    </para>
        /// </summary>
        Up = 0x26,

        /// <summary>
        ///    <para>
        ///       The RIGHT ARROW key.
        ///    </para>
        /// </summary>
        Right = 0x27,

        /// <summary>
        ///    <para>
        ///       The DOWN ARROW key.
        ///    </para>
        /// </summary>
        Down = 0x28,

        /// <summary>
        ///    <para>
        ///       The SELECT key.
        ///    </para>
        /// </summary>
        Select = 0x29,

        /// <summary>
        ///    <para>
        ///       The PRINT key.
        ///    </para>
        /// </summary>
        Print = 0x2A,

        /// <summary>
        ///    <para>
        ///       The EXECUTE key.
        ///    </para>
        /// </summary>
        Execute = 0x2B,

        /// <summary>
        ///    <para>
        ///       The PRINT SCREEN key.
        ///
        ///    </para>
        /// </summary>
        Snapshot = 0x2C,

        /// <summary>
        ///    <para>
        ///       The PRINT SCREEN key.
        ///    </para>
        /// </summary>
        PrintScreen = Snapshot,

        /// <summary>
        ///    <para>
        ///       The INS key.
        ///    </para>
        /// </summary>
        Insert = 0x2D,

        /// <summary>
        ///    <para>
        ///       The DEL key.
        ///    </para>
        /// </summary>
        Delete = 0x2E,

        /// <summary>
        ///    <para>
        ///       The HELP key.
        ///    </para>
        /// </summary>
        Help = 0x2F,

        /// <summary>
        ///    <para>
        ///       The 0 key.
        ///    </para>
        /// </summary>
        D0 = 0x30, // 0

        /// <summary>
        ///    <para>
        ///       The 1 key.
        ///    </para>
        /// </summary>
        D1 = 0x31, // 1

        /// <summary>
        ///    <para>
        ///       The 2 key.
        ///    </para>
        /// </summary>
        D2 = 0x32, // 2

        /// <summary>
        ///    <para>
        ///       The 3 key.
        ///    </para>
        /// </summary>
        D3 = 0x33, // 3

        /// <summary>
        ///    <para>
        ///       The 4 key.
        ///    </para>
        /// </summary>
        D4 = 0x34, // 4

        /// <summary>
        ///    <para>
        ///       The 5 key.
        ///    </para>
        /// </summary>
        D5 = 0x35, // 5

        /// <summary>
        ///    <para>
        ///       The 6 key.
        ///    </para>
        /// </summary>
        D6 = 0x36, // 6

        /// <summary>
        ///    <para>
        ///       The 7 key.
        ///    </para>
        /// </summary>
        D7 = 0x37, // 7

        /// <summary>
        ///    <para>
        ///       The 8 key.
        ///    </para>
        /// </summary>
        D8 = 0x38, // 8

        /// <summary>
        ///    <para>
        ///       The 9 key.
        ///    </para>
        /// </summary>
        D9 = 0x39, // 9

        /// <summary>
        ///    <para>
        ///       The A key.
        ///    </para>
        /// </summary>
        A = 0x41,

        /// <summary>
        ///    <para>
        ///       The B key.
        ///    </para>
        /// </summary>
        B = 0x42,

        /// <summary>
        ///    <para>
        ///       The C key.
        ///    </para>
        /// </summary>
        C = 0x43,

        /// <summary>
        ///    <para>
        ///       The D key.
        ///    </para>
        /// </summary>
        D = 0x44,

        /// <summary>
        ///    <para>
        ///       The E key.
        ///    </para>
        /// </summary>
        E = 0x45,

        /// <summary>
        ///    <para>
        ///       The F key.
        ///    </para>
        /// </summary>
        F = 0x46,

        /// <summary>
        ///    <para>
        ///       The G key.
        ///    </para>
        /// </summary>
        G = 0x47,

        /// <summary>
        ///    <para>
        ///       The H key.
        ///    </para>
        /// </summary>
        H = 0x48,

        /// <summary>
        ///    <para>
        ///       The I key.
        ///    </para>
        /// </summary>
        I = 0x49,

        /// <summary>
        ///    <para>
        ///       The J key.
        ///    </para>
        /// </summary>
        J = 0x4A,

        /// <summary>
        ///    <para>
        ///       The K key.
        ///    </para>
        /// </summary>
        K = 0x4B,

        /// <summary>
        ///    <para>
        ///       The L key.
        ///    </para>
        /// </summary>
        L = 0x4C,

        /// <summary>
        ///    <para>
        ///       The M key.
        ///    </para>
        /// </summary>
        M = 0x4D,

        /// <summary>
        ///    <para>
        ///       The N key.
        ///    </para>
        /// </summary>
        N = 0x4E,

        /// <summary>
        ///    <para>
        ///       The O key.
        ///    </para>
        /// </summary>
        O = 0x4F,

        /// <summary>
        ///    <para>
        ///       The P key.
        ///    </para>
        /// </summary>
        P = 0x50,

        /// <summary>
        ///    <para>
        ///       The Q key.
        ///    </para>
        /// </summary>
        Q = 0x51,

        /// <summary>
        ///    <para>
        ///       The R key.
        ///    </para>
        /// </summary>
        R = 0x52,

        /// <summary>
        ///    <para>
        ///       The S key.
        ///    </para>
        /// </summary>
        S = 0x53,

        /// <summary>
        ///    <para>
        ///       The T key.
        ///    </para>
        /// </summary>
        T = 0x54,

        /// <summary>
        ///    <para>
        ///       The U key.
        ///    </para>
        /// </summary>
        U = 0x55,

        /// <summary>
        ///    <para>
        ///       The V key.
        ///    </para>
        /// </summary>
        V = 0x56,

        /// <summary>
        ///    <para>
        ///       The W key.
        ///    </para>
        /// </summary>
        W = 0x57,

        /// <summary>
        ///    <para>
        ///       The X key.
        ///    </para>
        /// </summary>
        X = 0x58,

        /// <summary>
        ///    <para>
        ///       The Y key.
        ///    </para>
        /// </summary>
        Y = 0x59,

        /// <summary>
        ///    <para>
        ///       The Z key.
        ///    </para>
        /// </summary>
        Z = 0x5A,

        /// <summary>
        ///    <para>
        ///       The left Windows logo key (Microsoft Natural Keyboard).
        ///    </para>
        /// </summary>
        LWin = 0x5B,

        /// <summary>
        ///    <para>
        ///       The right Windows logo key (Microsoft Natural Keyboard).
        ///    </para>
        /// </summary>
        RWin = 0x5C,

        /// <summary>
        ///    <para>
        ///       The Application key (Microsoft Natural Keyboard).
        ///    </para>
        /// </summary>
        Apps = 0x5D,

        /// <summary>
        ///    <para>
        ///       The Computer Sleep key.
        ///    </para>
        /// </summary>
        Sleep = 0x5F,

        /// <summary>
        ///    <para>
        ///       The 0 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad0 = 0x60,

        /// <summary>
        ///    <para>
        ///       The 1 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad1 = 0x61,

        /// <summary>
        ///    <para>
        ///       The 2 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad2 = 0x62,

        /// <summary>
        ///    <para>
        ///       The 3 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad3 = 0x63,

        /// <summary>
        ///    <para>
        ///       The 4 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad4 = 0x64,

        /// <summary>
        ///    <para>
        ///       The 5 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad5 = 0x65,

        /// <summary>
        ///    <para>
        ///       The 6 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad6 = 0x66,

        /// <summary>
        ///    <para>
        ///       The 7 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad7 = 0x67,

        /// <summary>
        ///    <para>
        ///       The 8 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad8 = 0x68,

        /// <summary>
        ///    <para>
        ///       The 9 key on the numeric keypad.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad9 = 0x69,

        /// <summary>
        ///    <para>
        ///       The Multiply key.
        ///    </para>
        /// </summary>
        Multiply = 0x6A,

        /// <summary>
        ///    <para>
        ///       The Add key.
        ///    </para>
        /// </summary>
        Add = 0x6B,

        /// <summary>
        ///    <para>
        ///       The Separator key.
        ///    </para>
        /// </summary>
        Separator = 0x6C,

        /// <summary>
        ///    <para>
        ///       The Subtract key.
        ///    </para>
        /// </summary>
        Subtract = 0x6D,

        /// <summary>
        ///    <para>
        ///       The Decimal key.
        ///    </para>
        /// </summary>
        Decimal = 0x6E,

        /// <summary>
        ///    <para>
        ///       The Divide key.
        ///    </para>
        /// </summary>
        Divide = 0x6F,

        /// <summary>
        ///    <para>
        ///       The F1 key.
        ///    </para>
        /// </summary>
        F1 = 0x70,

        /// <summary>
        ///    <para>
        ///       The F2 key.
        ///    </para>
        /// </summary>
        F2 = 0x71,

        /// <summary>
        ///    <para>
        ///       The F3 key.
        ///    </para>
        /// </summary>
        F3 = 0x72,

        /// <summary>
        ///    <para>
        ///       The F4 key.
        ///    </para>
        /// </summary>
        F4 = 0x73,

        /// <summary>
        ///    <para>
        ///       The F5 key.
        ///    </para>
        /// </summary>
        F5 = 0x74,

        /// <summary>
        ///    <para>
        ///       The F6 key.
        ///    </para>
        /// </summary>
        F6 = 0x75,

        /// <summary>
        ///    <para>
        ///       The F7 key.
        ///    </para>
        /// </summary>
        F7 = 0x76,

        /// <summary>
        ///    <para>
        ///       The F8 key.
        ///    </para>
        /// </summary>
        F8 = 0x77,

        /// <summary>
        ///    <para>
        ///       The F9 key.
        ///    </para>
        /// </summary>
        F9 = 0x78,

        /// <summary>
        ///    <para>
        ///       The F10 key.
        ///    </para>
        /// </summary>
        F10 = 0x79,

        /// <summary>
        ///    <para>
        ///       The F11 key.
        ///    </para>
        /// </summary>
        F11 = 0x7A,

        /// <summary>
        ///    <para>
        ///       The F12 key.
        ///    </para>
        /// </summary>
        F12 = 0x7B,

        /// <summary>
        ///    <para>
        ///       The F13 key.
        ///    </para>
        /// </summary>
        F13 = 0x7C,

        /// <summary>
        ///    <para>
        ///       The F14 key.
        ///    </para>
        /// </summary>
        F14 = 0x7D,

        /// <summary>
        ///    <para>
        ///       The F15 key.
        ///    </para>
        /// </summary>
        F15 = 0x7E,

        /// <summary>
        ///    <para>
        ///       The F16 key.
        ///    </para>
        /// </summary>
        F16 = 0x7F,

        /// <summary>
        ///    <para>
        ///       The F17 key.
        ///    </para>
        /// </summary>
        F17 = 0x80,

        /// <summary>
        ///    <para>
        ///       The F18 key.
        ///    </para>
        /// </summary>
        F18 = 0x81,

        /// <summary>
        ///    <para>
        ///       The F19 key.
        ///    </para>
        /// </summary>
        F19 = 0x82,

        /// <summary>
        ///    <para>
        ///       The F20 key.
        ///    </para>
        /// </summary>
        F20 = 0x83,

        /// <summary>
        ///    <para>
        ///       The F21 key.
        ///    </para>
        /// </summary>
        F21 = 0x84,

        /// <summary>
        ///    <para>
        ///       The F22 key.
        ///    </para>
        /// </summary>
        F22 = 0x85,

        /// <summary>
        ///    <para>
        ///       The F23 key.
        ///    </para>
        /// </summary>
        F23 = 0x86,

        /// <summary>
        ///    <para>
        ///       The F24 key.
        ///    </para>
        /// </summary>
        F24 = 0x87,

        /// <summary>
        ///    <para>
        ///       The NUM LOCK key.
        ///    </para>
        /// </summary>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumLock = 0x90,

        /// <summary>
        ///    <para>
        ///       The SCROLL LOCK key.
        ///    </para>
        /// </summary>
        Scroll = 0x91,

        /// <summary>
        ///    <para>
        ///       The left SHIFT key.
        ///    </para>
        /// </summary>
        LShiftKey = 0xA0,

        /// <summary>
        ///    <para>
        ///       The right SHIFT key.
        ///    </para>
        /// </summary>
        RShiftKey = 0xA1,

        /// <summary>
        ///    <para>
        ///       The left CTRL key.
        ///    </para>
        /// </summary>
        LControlKey = 0xA2,

        /// <summary>
        ///    <para>
        ///       The right CTRL key.
        ///    </para>
        /// </summary>
        RControlKey = 0xA3,

        /// <summary>
        ///    <para>
        ///       The left ALT key.
        ///    </para>
        /// </summary>
        LMenu = 0xA4,

        /// <summary>
        ///    <para>
        ///       The right ALT key.
        ///    </para>
        /// </summary>
        RMenu = 0xA5,

        /// <summary>
        ///    <para>
        ///       The Browser Back key.
        ///    </para>
        /// </summary>
        BrowserBack = 0xA6,

        /// <summary>
        ///    <para>
        ///       The Browser Forward key.
        ///    </para>
        /// </summary>
        BrowserForward = 0xA7,

        /// <summary>
        ///    <para>
        ///       The Browser Refresh key.
        ///    </para>
        /// </summary>
        BrowserRefresh = 0xA8,

        /// <summary>
        ///    <para>
        ///       The Browser Stop key.
        ///    </para>
        /// </summary>
        BrowserStop = 0xA9,

        /// <summary>
        ///    <para>
        ///       The Browser Search key.
        ///    </para>
        /// </summary>
        BrowserSearch = 0xAA,

        /// <summary>
        ///    <para>
        ///       The Browser Favorites key.
        ///    </para>
        /// </summary>
        BrowserFavorites = 0xAB,

        /// <summary>
        ///    <para>
        ///       The Browser Home key.
        ///    </para>
        /// </summary>
        BrowserHome = 0xAC,

        /// <summary>
        ///    <para>
        ///       The Volume Mute key.
        ///    </para>
        /// </summary>
        VolumeMute = 0xAD,

        /// <summary>
        ///    <para>
        ///       The Volume Down key.
        ///    </para>
        /// </summary>
        VolumeDown = 0xAE,

        /// <summary>
        ///    <para>
        ///       The Volume Up key.
        ///    </para>
        /// </summary>
        VolumeUp = 0xAF,

        /// <summary>
        ///    <para>
        ///       The Media Next Track key.
        ///    </para>
        /// </summary>
        MediaNextTrack = 0xB0,

        /// <summary>
        ///    <para>
        ///       The Media Previous Track key.
        ///    </para>
        /// </summary>
        MediaPreviousTrack = 0xB1,

        /// <summary>
        ///    <para>
        ///       The Media Stop key.
        ///    </para>
        /// </summary>
        MediaStop = 0xB2,

        /// <summary>
        ///    <para>
        ///       The Media Play Pause key.
        ///    </para>
        /// </summary>
        MediaPlayPause = 0xB3,

        /// <summary>
        ///    <para>
        ///       The Launch Mail key.
        ///    </para>
        /// </summary>
        LaunchMail = 0xB4,

        /// <summary>
        ///    <para>
        ///       The Select Media key.
        ///    </para>
        /// </summary>
        SelectMedia = 0xB5,

        /// <summary>
        ///    <para>
        ///       The Launch Application1 key.
        ///    </para>
        /// </summary>
        LaunchApplication1 = 0xB6,

        /// <summary>
        ///    <para>
        ///       The Launch Application2 key.
        ///    </para>
        /// </summary>
        LaunchApplication2 = 0xB7,

        /// <summary>
        ///    <para>
        ///       The Oem Semicolon key.
        ///    </para>
        /// </summary>
        OemSemicolon = 0xBA,

        /// <summary>
        ///    <para>
        ///       The Oem 1 key.
        ///    </para>
        /// </summary>
        Oem1 = OemSemicolon,

        /// <summary>
        ///    <para>
        ///       The Oem plus key.
        ///    </para>
        /// </summary>
        Oemplus = 0xBB,

        /// <summary>
        ///    <para>
        ///       The Oem comma key.
        ///    </para>
        /// </summary>
        Oemcomma = 0xBC,

        /// <summary>
        ///    <para>
        ///       The Oem Minus key.
        ///    </para>
        /// </summary>
        OemMinus = 0xBD,

        /// <summary>
        ///    <para>
        ///       The Oem Period key.
        ///    </para>
        /// </summary>
        OemPeriod = 0xBE,

        /// <summary>
        ///    <para>
        ///       The Oem Question key.
        ///    </para>
        /// </summary>
        OemQuestion = 0xBF,

        /// <summary>
        ///    <para>
        ///       The Oem 2 key.
        ///    </para>
        /// </summary>
        Oem2 = OemQuestion,

        /// <summary>
        ///    <para>
        ///       The Oem tilde key.
        ///    </para>
        /// </summary>
        Oemtilde = 0xC0,

        /// <summary>
        ///    <para>
        ///       The Oem 3 key.
        ///    </para>
        /// </summary>
        Oem3 = Oemtilde,

        /// <summary>
        ///    <para>
        ///       The Oem Open Brackets key.
        ///    </para>
        /// </summary>
        OemOpenBrackets = 0xDB,

        /// <summary>
        ///    <para>
        ///       The Oem 4 key.
        ///    </para>
        /// </summary>
        Oem4 = OemOpenBrackets,

        /// <summary>
        ///    <para>
        ///       The Oem Pipe key.
        ///    </para>
        /// </summary>
        OemPipe = 0xDC,

        /// <summary>
        ///    <para>
        ///       The Oem 5 key.
        ///    </para>
        /// </summary>
        Oem5 = OemPipe,

        /// <summary>
        ///    <para>
        ///       The Oem Close Brackets key.
        ///    </para>
        /// </summary>
        OemCloseBrackets = 0xDD,

        /// <summary>
        ///    <para>
        ///       The Oem 6 key.
        ///    </para>
        /// </summary>
        Oem6 = OemCloseBrackets,

        /// <summary>
        ///    <para>
        ///       The Oem Quotes key.
        ///    </para>
        /// </summary>
        OemQuotes = 0xDE,

        /// <summary>
        ///    <para>
        ///       The Oem 7 key.
        ///    </para>
        /// </summary>
        Oem7 = OemQuotes,

        /// <summary>
        ///    <para>
        ///       The Oem8 key.
        ///    </para>
        /// </summary>
        Oem8 = 0xDF,

        /// <summary>
        ///    <para>
        ///       The Oem Backslash key.
        ///    </para>
        /// </summary>
        OemBackslash = 0xE2,

        /// <summary>
        ///    <para>
        ///       The Oem 102 key.
        ///    </para>
        /// </summary>
        Oem102 = OemBackslash,

        /// <summary>
        ///    <para>
        ///       The PROCESS KEY key.
        ///    </para>
        /// </summary>
        ProcessKey = 0xE5,

        /// <summary>
        ///    <para>
        ///       The Packet KEY key.
        ///    </para>
        /// </summary>
        Packet = 0xE7,

        /// <summary>
        ///    <para>
        ///       The ATTN key.
        ///    </para>
        /// </summary>
        Attn = 0xF6,

        /// <summary>
        ///    <para>
        ///       The CRSEL key.
        ///    </para>
        /// </summary>
        Crsel = 0xF7,

        /// <summary>
        ///    <para>
        ///       The EXSEL key.
        ///    </para>
        /// </summary>
        Exsel = 0xF8,

        /// <summary>
        ///    <para>
        ///       The ERASE EOF key.
        ///    </para>
        /// </summary>
        EraseEof = 0xF9,

        /// <summary>
        ///    <para>
        ///       The PLAY key.
        ///    </para>
        /// </summary>
        Play = 0xFA,

        /// <summary>
        ///    <para>
        ///       The ZOOM key.
        ///    </para>
        /// </summary>
        Zoom = 0xFB,

        /// <summary>
        ///    <para>
        ///       A constant reserved for future use.
        ///    </para>
        /// </summary>
        NoName = 0xFC,

        /// <summary>
        ///    <para>
        ///       The PA1 key.
        ///    </para>
        /// </summary>
        Pa1 = 0xFD,

        /// <summary>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </summary>
        OemClear = 0xFE,

        /// <summary>
        ///    <para>
        ///       The SHIFT modifier key.
        ///    </para>
        /// </summary>
        [Obsolete("ONLY USE IN WINFORM, NOT IN WIN32")]
        [Browsable(false)]
        Shift = 0x00010000,

        /// <summary>
        ///    <para>
        ///       The
        ///       CTRL modifier key.
        ///
        ///    </para>
        /// </summary>
        [Obsolete("ONLY USE IN WINFORM, NOT IN WIN32")]
        [Browsable(false)]
        Control = 0x00020000,

        /// <summary>
        ///    <para>
        ///       The ALT modifier key.
        ///
        ///    </para>
        /// </summary>
        [Obsolete("ONLY USE IN WINFORM, NOT IN WIN32")]
        [Browsable(false)]
        Alt = 0x00040000,
    }

    public enum ScanCodes
    {
        LBUTTON = 0,
        RBUTTON = 0,
        CANCEL = 70,
        MBUTTON = 0,
        XBUTTON1 = 0,
        XBUTTON2 = 0,
        BACK = 14,
        TAB = 15,
        CLEAR = 76,
        RETURN = 28,
        SHIFT = 42,
        CONTROL = 29,
        MENU = 56,
        PAUSE = 0,
        CAPITAL = 58,
        KANA = 0,
        HANGUL = 0,
        JUNJA = 0,
        FINAL = 0,
        HANJA = 0,
        KANJI = 0,
        ESCAPE = 1,
        CONVERT = 0,
        NONCONVERT = 0,
        ACCEPT = 0,
        MODECHANGE = 0,
        SPACE = 57,
        PRIOR = 73,
        NEXT = 81,
        END = 79,
        HOME = 71,
        LEFT = 75,
        UP = 72,
        RIGHT = 77,
        DOWN = 80,
        SELECT = 0,
        PRINT = 0,
        EXECUTE = 0,
        SNAPSHOT = 84,
        INSERT = 82,
        DELETE = 83,
        HELP = 99,
        KEY_0 = 11,
        KEY_1 = 2,
        KEY_2 = 3,
        KEY_3 = 4,
        KEY_4 = 5,
        KEY_5 = 6,
        KEY_6 = 7,
        KEY_7 = 8,
        KEY_8 = 9,
        KEY_9 = 10,
        KEY_A = 30,
        KEY_B = 48,
        KEY_C = 46,
        KEY_D = 32,
        KEY_E = 18,
        KEY_F = 33,
        KEY_G = 34,
        KEY_H = 35,
        KEY_I = 23,
        KEY_J = 36,
        KEY_K = 37,
        KEY_L = 38,
        KEY_M = 50,
        KEY_N = 49,
        KEY_O = 24,
        KEY_P = 25,
        KEY_Q = 16,
        KEY_R = 19,
        KEY_S = 31,
        KEY_T = 20,
        KEY_U = 22,
        KEY_V = 47,
        KEY_W = 17,
        KEY_X = 45,
        KEY_Y = 21,
        KEY_Z = 44,
        LWIN = 91,
        RWIN = 92,
        APPS = 93,
        SLEEP = 95,
        NUMPAD0 = 82,
        NUMPAD1 = 79,
        NUMPAD2 = 80,
        NUMPAD3 = 81,
        NUMPAD4 = 75,
        NUMPAD5 = 76,
        NUMPAD6 = 77,
        NUMPAD7 = 71,
        NUMPAD8 = 72,
        NUMPAD9 = 73,
        MULTIPLY = 55,
        ADD = 78,
        SEPARATOR = 0,
        SUBTRACT = 74,
        DECIMAL = 83,
        DIVIDE = 53,
        F1 = 59,
        F2 = 60,
        F3 = 61,
        F4 = 62,
        F5 = 63,
        F6 = 64,
        F7 = 65,
        F8 = 66,
        F9 = 67,
        F10 = 68,
        F11 = 87,
        F12 = 88,
        F13 = 100,
        F14 = 101,
        F15 = 102,
        F16 = 103,
        F17 = 104,
        F18 = 105,
        F19 = 106,
        F20 = 107,
        F21 = 108,
        F22 = 109,
        F23 = 110,
        F24 = 118,
        NUMLOCK = 69,
        SCROLL = 70,
        LSHIFT = 42,
        RSHIFT = 54,
        LCONTROL = 29,
        RCONTROL = 29,
        LMENU = 56,
        RMENU = 56,
        BROWSER_BACK = 106,
        BROWSER_FORWARD = 105,
        BROWSER_REFRESH = 103,
        BROWSER_STOP = 104,
        BROWSER_SEARCH = 101,
        BROWSER_FAVORITES = 102,
        BROWSER_HOME = 50,
        VOLUME_MUTE = 32,
        VOLUME_DOWN = 46,
        VOLUME_UP = 48,
        MEDIA_NEXT_TRACK = 25,
        MEDIA_PREV_TRACK = 16,
        MEDIA_STOP = 36,
        MEDIA_PLAY_PAUSE = 34,
        LAUNCH_MAIL = 108,
        LAUNCH_MEDIA_SELECT = 109,
        LAUNCH_APP1 = 107,
        LAUNCH_APP2 = 33,
        OEM_1 = 39,
        OEM_PLUS = 13,
        OEM_COMMA = 51,
        OEM_MINUS = 12,
        OEM_PERIOD = 52,
        OEM_2 = 53,
        OEM_3 = 41,
        OEM_4 = 26,
        OEM_5 = 43,
        OEM_6 = 27,
        OEM_7 = 40,
        OEM_8 = 0,
        OEM_102 = 86,
        PROCESSKEY = 0,
        PACKET = 0,
        ATTN = 0,
        CRSEL = 0,
        EXSEL = 0,
        EREOF = 93,
        PLAY = 0,
        ZOOM = 98,
        NONAME = 0,
        PA1 = 0,
        OEM_CLEAR = 0
    }
}