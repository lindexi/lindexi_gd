﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vector = System.Numerics.Vector;

namespace LereleweawaLinojairgeefonechal
{
    internal interface IAppDomainShutdownListener
    {
        void NotifyShutdown();
    }

    internal class AppDomainShutdownNotifier : WeakReference, IDisposable
    {
        public AppDomainShutdownNotifier(IAppDomainShutdownListener target) : base(target)
        {
            AppDomain.CurrentDomain.DomainUnload += OnShutdown;
            AppDomain.CurrentDomain.ProcessExit += OnShutdown;
        }

        private void OnShutdown(object sender, EventArgs e)
        {
            var listener = (IAppDomainShutdownListener) Target;
            listener?.NotifyShutdown();
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.DomainUnload -= OnShutdown;
            AppDomain.CurrentDomain.ProcessExit -= OnShutdown;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            var appDomain = AppDomain.CreateDomain("Foo");
            AppDomain.Unload(appDomain);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var propertyInfo = typeof(Dispatcher).GetProperty("Reserved0", BindingFlags.NonPublic | BindingFlags.Instance);
            var mediaContext = propertyInfo.GetMethod.Invoke(Dispatcher, null);
            var mediaContextType = typeof(Visual).Assembly.GetType("System.Windows.Media.MediaContext");
            var methodInfoGetChannels = mediaContextType.GetMethod("GetChannels", BindingFlags.NonPublic | BindingFlags.Instance);
            var channelSet = methodInfoGetChannels.Invoke(mediaContext, null);

            var typeChannelSet = typeof(Visual).Assembly.GetType("System.Windows.Media.Composition.DUCE+ChannelSet");
            var fieldInfoChannel = typeChannelSet.GetField("Channel", BindingFlags.NonPublic | BindingFlags.Instance);
            var channel = fieldInfoChannel.GetValue(channelSet);

            var typeChannel = typeof(Visual).Assembly.GetType("System.Windows.Media.Composition.DUCE+Channel");
            var hChannelFieldInfo = typeChannel.GetField("_hChannel", BindingFlags.NonPublic | BindingFlags.Instance);
            var hChannel = (IntPtr)hChannelFieldInfo.GetValue(channel);

            ResourceHandle handle = default;
            var result = MilResource_CreateOrAddRefOnChannel(hChannel, ResourceType.TYPE_BRUSH, ref handle);
        }

        [DllImport(MilCore)]
        internal static extern /*HRESULT*/ int MilResource_CreateOrAddRefOnChannel(
            IntPtr pChannel,
            ResourceType resourceType,
            ref ResourceHandle hResource
        );

        internal const string WCP_VERSION_SUFFIX = "_cor3";
        internal const string MilCore = "wpfgfx" + WCP_VERSION_SUFFIX + ".dll";

    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ResourceHandle
    {
        public static readonly ResourceHandle Null = new ResourceHandle(0);

        public static explicit operator uint(ResourceHandle r)
        {
            return r._handle;
        }

        [FieldOffset(0)]
        private UInt32 _handle;
        public ResourceHandle(UInt32 handle) { _handle = handle; }

        /// <summary>
        /// Checks if the handle is null.
        /// </summary>
        public bool IsNull { get { return (_handle == 0); } }
    }

    internal enum ResourceType
    {
        /* 0x00 */
        TYPE_NULL = 0,
        /* 0x01 */
        TYPE_MEDIAPLAYER = 1,
        /* 0x02 */
        TYPE_ROTATION3D = 2,
        /* 0x03 */
        TYPE_AXISANGLEROTATION3D = 3,
        /* 0x04 */
        TYPE_QUATERNIONROTATION3D = 4,
        /* 0x05 */
        TYPE_CAMERA = 5,
        /* 0x06 */
        TYPE_PROJECTIONCAMERA = 6,
        /* 0x07 */
        TYPE_PERSPECTIVECAMERA = 7,
        /* 0x08 */
        TYPE_ORTHOGRAPHICCAMERA = 8,
        /* 0x09 */
        TYPE_MATRIXCAMERA = 9,
        /* 0x0a */
        TYPE_MODEL3D = 10,
        /* 0x0b */
        TYPE_MODEL3DGROUP = 11,
        /* 0x0c */
        TYPE_LIGHT = 12,
        /* 0x0d */
        TYPE_AMBIENTLIGHT = 13,
        /* 0x0e */
        TYPE_DIRECTIONALLIGHT = 14,
        /* 0x0f */
        TYPE_POINTLIGHTBASE = 15,
        /* 0x10 */
        TYPE_POINTLIGHT = 16,
        /* 0x11 */
        TYPE_SPOTLIGHT = 17,
        /* 0x12 */
        TYPE_GEOMETRYMODEL3D = 18,
        /* 0x13 */
        TYPE_GEOMETRY3D = 19,
        /* 0x14 */
        TYPE_MESHGEOMETRY3D = 20,
        /* 0x15 */
        TYPE_MATERIAL = 21,
        /* 0x16 */
        TYPE_MATERIALGROUP = 22,
        /* 0x17 */
        TYPE_DIFFUSEMATERIAL = 23,
        /* 0x18 */
        TYPE_SPECULARMATERIAL = 24,
        /* 0x19 */
        TYPE_EMISSIVEMATERIAL = 25,
        /* 0x1a */
        TYPE_TRANSFORM3D = 26,
        /* 0x1b */
        TYPE_TRANSFORM3DGROUP = 27,
        /* 0x1c */
        TYPE_AFFINETRANSFORM3D = 28,
        /* 0x1d */
        TYPE_TRANSLATETRANSFORM3D = 29,
        /* 0x1e */
        TYPE_SCALETRANSFORM3D = 30,
        /* 0x1f */
        TYPE_ROTATETRANSFORM3D = 31,
        /* 0x20 */
        TYPE_MATRIXTRANSFORM3D = 32,
        /* 0x21 */
        TYPE_PIXELSHADER = 33,
        /* 0x22 */
        TYPE_IMPLICITINPUTBRUSH = 34,
        /* 0x23 */
        TYPE_EFFECT = 35,
        /* 0x24 */
        TYPE_BLUREFFECT = 36,
        /* 0x25 */
        TYPE_DROPSHADOWEFFECT = 37,
        /* 0x26 */
        TYPE_SHADEREFFECT = 38,
        /* 0x27 */
        TYPE_VISUAL = 39,
        /* 0x28 */
        TYPE_VIEWPORT3DVISUAL = 40,
        /* 0x29 */
        TYPE_VISUAL3D = 41,
        /* 0x2a */
        TYPE_GLYPHRUN = 42,
        /* 0x2b */
        TYPE_RENDERDATA = 43,
        /* 0x2c */
        TYPE_DRAWINGCONTEXT = 44,
        /* 0x2d */
        TYPE_RENDERTARGET = 45,
        /* 0x2e */
        TYPE_HWNDRENDERTARGET = 46,
        /* 0x2f */
        TYPE_GENERICRENDERTARGET = 47,
        /* 0x30 */
        TYPE_ETWEVENTRESOURCE = 48,
        /* 0x31 */
        TYPE_DOUBLERESOURCE = 49,
        /* 0x32 */
        TYPE_COLORRESOURCE = 50,
        /* 0x33 */
        TYPE_POINTRESOURCE = 51,
        /* 0x34 */
        TYPE_RECTRESOURCE = 52,
        /* 0x35 */
        TYPE_SIZERESOURCE = 53,
        /* 0x36 */
        TYPE_MATRIXRESOURCE = 54,
        /* 0x37 */
        TYPE_POINT3DRESOURCE = 55,
        /* 0x38 */
        TYPE_VECTOR3DRESOURCE = 56,
        /* 0x39 */
        TYPE_QUATERNIONRESOURCE = 57,
        /* 0x3a */
        TYPE_IMAGESOURCE = 58,
        /* 0x3b */
        TYPE_DRAWINGIMAGE = 59,
        /* 0x3c */
        TYPE_TRANSFORM = 60,
        /* 0x3d */
        TYPE_TRANSFORMGROUP = 61,
        /* 0x3e */
        TYPE_TRANSLATETRANSFORM = 62,
        /* 0x3f */
        TYPE_SCALETRANSFORM = 63,
        /* 0x40 */
        TYPE_SKEWTRANSFORM = 64,
        /* 0x41 */
        TYPE_ROTATETRANSFORM = 65,
        /* 0x42 */
        TYPE_MATRIXTRANSFORM = 66,
        /* 0x43 */
        TYPE_GEOMETRY = 67,
        /* 0x44 */
        TYPE_LINEGEOMETRY = 68,
        /* 0x45 */
        TYPE_RECTANGLEGEOMETRY = 69,
        /* 0x46 */
        TYPE_ELLIPSEGEOMETRY = 70,
        /* 0x47 */
        TYPE_GEOMETRYGROUP = 71,
        /* 0x48 */
        TYPE_COMBINEDGEOMETRY = 72,
        /* 0x49 */
        TYPE_PATHGEOMETRY = 73,
        /* 0x4a */
        TYPE_BRUSH = 74,
        /* 0x4b */
        TYPE_SOLIDCOLORBRUSH = 75,
        /* 0x4c */
        TYPE_GRADIENTBRUSH = 76,
        /* 0x4d */
        TYPE_LINEARGRADIENTBRUSH = 77,
        /* 0x4e */
        TYPE_RADIALGRADIENTBRUSH = 78,
        /* 0x4f */
        TYPE_TILEBRUSH = 79,
        /* 0x50 */
        TYPE_IMAGEBRUSH = 80,
        /* 0x51 */
        TYPE_DRAWINGBRUSH = 81,
        /* 0x52 */
        TYPE_VISUALBRUSH = 82,
        /* 0x53 */
        TYPE_BITMAPCACHEBRUSH = 83,
        /* 0x54 */
        TYPE_DASHSTYLE = 84,
        /* 0x55 */
        TYPE_PEN = 85,
        /* 0x56 */
        TYPE_DRAWING = 86,
        /* 0x57 */
        TYPE_GEOMETRYDRAWING = 87,
        /* 0x58 */
        TYPE_GLYPHRUNDRAWING = 88,
        /* 0x59 */
        TYPE_IMAGEDRAWING = 89,
        /* 0x5a */
        TYPE_VIDEODRAWING = 90,
        /* 0x5b */
        TYPE_DRAWINGGROUP = 91,
        /* 0x5c */
        TYPE_GUIDELINESET = 92,
        /* 0x5d */
        TYPE_CACHEMODE = 93,
        /* 0x5e */
        TYPE_BITMAPCACHE = 94,
        /* 0x5f */
        TYPE_BITMAPSOURCE = 95,
        /* 0x60 */
        TYPE_DOUBLEBUFFEREDBITMAP = 96,
        /* 0x61 */
        TYPE_D3DIMAGE = 97,
    };
}
