using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif PBTCOMPILER
namespace MS.Utility
#elif AUTOMATION
namespace MS.Internal.Automation
#elif REACHFRAMEWORK
namespace System.Windows.Xps
#elif PRESENTATIONFRAMEWORK
namespace System.Windows
#elif PRESENTATIONUI
namespace System.Windows.TrustUI
#elif WINDOWSFORMSINTEGRATION
namespace System.Windows
#elif RIBBON_IN_FRAMEWORK
namespace Microsoft.Windows.Controls
#else
namespace System
#endif
{
    internal partial class SR :
#if WINDOWS_BASE
         global::WindowsBase.Resources.Strings
#elif SYSTEM_XAML
         System.Xaml.Resources.Strings
#endif
    {
    }
}