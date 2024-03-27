using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlankX11App.X11;
internal enum NotifyMode
{
    NotifyNormal = 0,
    NotifyGrab = 1,
    NotifyUngrab = 2
}

internal enum NotifyDetail
{
    NotifyAncestor = 0,
    NotifyVirtual = 1,
    NotifyInferior = 2,
    NotifyNonlinear = 3,
    NotifyNonlinearVirtual = 4,
    NotifyPointer = 5,
    NotifyPointerRoot = 6,
    NotifyDetailNone = 7
}

internal enum Gravity
{
    ForgetGravity = 0,
    NorthWestGravity = 1,
    NorthGravity = 2,
    NorthEastGravity = 3,
    WestGravity = 4,
    CenterGravity = 5,
    EastGravity = 6,
    SouthWestGravity = 7,
    SouthGravity = 8,
    SouthEastGravity = 9,
    StaticGravity = 10
}

internal enum CreateWindowArgs
{
    CopyFromParent = 0,
    ParentRelative = 1,
    InputOutput = 1,
    InputOnly = 2
}

[Flags]
internal enum SetWindowValuemask
{
    Nothing = 0,
    BackPixmap = 1,
    BackPixel = 2,
    BorderPixmap = 4,
    BorderPixel = 8,
    BitGravity = 16,
    WinGravity = 32,
    BackingStore = 64,
    BackingPlanes = 128,
    BackingPixel = 256,
    OverrideRedirect = 512,
    SaveUnder = 1024,
    EventMask = 2048,
    DontPropagate = 4096,
    ColorMap = 8192,
    Cursor = 16384
}

[Flags]
internal enum XModifierMask
{
    ShiftMask = (1 << 0),
    LockMask = (1 << 1),
    ControlMask = (1 << 2),
    Mod1Mask = (1 << 3),
    Mod2Mask = (1 << 4),
    Mod3Mask = (1 << 5),
    Mod4Mask = (1 << 6),
    Mod5Mask = (1 << 7),
    Button1Mask = (1 << 8),
    Button2Mask = (1 << 9),
    Button3Mask = (1 << 10),
    Button4Mask = (1 << 11),
    Button5Mask = (1 << 12),
    AnyModifier = (1 << 15)
}
