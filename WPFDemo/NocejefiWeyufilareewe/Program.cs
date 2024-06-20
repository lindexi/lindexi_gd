//using Windows.UI.ViewManagement;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml;

namespace NocejefiWeyufilareewe;

public sealed class Example
{
    public int SampleProperty { get; set; }

    public static string SayHello()
    {
        return "Hello from your C# WinRT component";
    }
}

//public class App : Application
//{
//    public event EventHandler<LaunchActivatedEventArgs>? Launched;

//    protected override void OnLaunched(LaunchActivatedEventArgs args)
//    {
//        Launched?.Invoke(this, args);
//    }
//}

//internal class Program
//{
//    static void Main(string[] args)
//    {
//        global::WinRT.ComWrappersSupport.InitializeComWrappers();
//        global::Microsoft.UI.Xaml.Application.Start((p) =>
//        {
//            var app = new App();
//            app.Launched += (_, e) =>
//            {
//                var window = new Window()
//                {
//                    Title = "控制台创建应用"
//                };
//                window.Content = new Grid()
//                {
//                    Children =
//                    {
//                        new TextBlock()
//                        {
//                            Text = "控制台应用",
//                            HorizontalAlignment = HorizontalAlignment.Center,
//                            VerticalAlignment = VerticalAlignment.Center
//                        },
//                        new Button()
//                        {
//                            Content = "点击",
//                            Margin = new Thickness(0, 100, 0, 0),
//                            HorizontalAlignment = HorizontalAlignment.Center,
//                            VerticalAlignment = VerticalAlignment.Center,
//                        }.Do(button => button.Click += (s, _) =>
//                        {
//                            // 以下代码会导致炸掉
//                            // 0x0D9D8624
//                            // 0xC0000005: 读取位置 0x00000000 时发生访问冲突。
//                            // 堆栈
///*
// * Microsoft.ui.xaml.dll!Microsoft::WRL::ComPtr<struct ABI::Microsoft::UI::Composition::IVisual>::As<struct ABI::Microsoft::UI::Composition::ISpriteVisual>(class Microsoft::WRL::Details::ComPtrRef<class Microsoft::WRL::ComPtr<struct ABI::Microsoft::UI::Composition::ISpriteVisual> >)	未知
//   Microsoft.ui.xaml.dll!DCompTreeHost::UpdateUIThreadCounters(unsigned int,float)	未知
//   Microsoft.ui.xaml.dll!CCoreServices::SubmitPrimitiveCompositionCommands()	未知
//   Microsoft.ui.xaml.dll!CCoreServices::NWDrawTree(class HWWalk *,class CWindowRenderTarget *,class VisualTree *,bool,bool *)	未知
//   Microsoft.ui.xaml.dll!CCoreServices::NWDrawMainTree()	未知
//   Microsoft.ui.xaml.dll!CWindowRenderTarget::Draw()	未知
//   Microsoft.ui.xaml.dll!CXcpBrowserHost::OnTick()	未知
//   Microsoft.ui.xaml.dll!CXcpDispatcher::Tick()	未知
//   Microsoft.ui.xaml.dll!CXcpDispatcher::OnReentrancyProtectedWindowMessage(struct HWND__ *,unsigned int,unsigned int,long)	未知
//   Microsoft.ui.xaml.dll!CDeferredInvoke::DispatchQueuedMessage(bool *,bool *)	未知
//   Microsoft.ui.xaml.dll!Microsoft::WRL::Details::DelegateArgTraits<long (__stdcall ABI::Windows::Foundation::ITypedEventHandler_impl<ABI::Windows::Foundation::Internal::AggregateType<ABI::Microsoft::UI::Dispatching::DispatcherQueueTimer *,ABI::Microsoft::UI::Dispatching::IDispatcherQueueTimer *>,IInspectable *>::*)(ABI::Microsoft::UI::Dispatching::IDispatcherQueueTimer *,IInspectable *)>::DelegateInvokeHelper<Microsoft::WRL::Implements<Microsoft::WRL::RuntimeClassFlags<2>,ABI::Windows::Foundation::ITypedEventHandler<ABI::Microsoft::UI::Dispatching::DispatcherQueueTimer *,IInspectable *>,Microsoft::WRL::FtmBase>,`CXcpDispatcher::Init'::`55'::<lambda_1> &,1,ABI::Microsoft::UI::Dispatching::IDispatcherQueueTimer *,IInspectable *>::Invoke()	未知
//   CoreMessagingXP.dll!Microsoft::WRL::Details::DelegateArgTraits<long ( Windows::Foundation::ITypedEventHandler_impl<struct Windows::Foundation::Internal::AggregateType<class Microsoft::UI::Dispatching::DispatcherQueueTimer *,struct Microsoft::UI::Dispatching::IDispatcherQueueTimer *>,struct IInspectable *>::*)(struct Microsoft::UI::Dispatching::IDispatcherQueueTimer *,struct IInspectable *)>::DelegateInvokeHelper<struct Microsoft::WRL::Implements<struct Microsoft::WRL::RuntimeClassFlags<2>,struct Windows::Foundation::ITypedEventHandler<class Microsoft::UI::Dispatching::DispatcherQueueTimer *,struct IInspectable *>,class Microsoft::WRL::FtmBase>,class <lambda_1b1a5b0dce93060c2ffe10d1d311f882>,-1,struct Microsoft::UI::Dispatching::IDispatcherQueueTimer *,struct IInspectable *>::Invoke(struct Microsoft::UI::Dispatching::IDispatcherQueueTimer *,struct IInspectable *)	未知
//   CoreMessagingXP.dll!Microsoft::WRL::InvokeTraits<-2>::InvokeDelegates<class <lambda_2ad0659dc62ecd7334c0ef0269e3265b>,struct Windows::Foundation::ITypedEventHandler<class Microsoft::UI::Dispatching::DispatcherQueue *,struct IInspectable *> >(class <lambda_2ad0659dc62ecd7334c0ef0269e3265b>,class Microsoft::WRL::Details::EventTargetArray *,class Microsoft::WRL::EventSource<struct Windows::Foundation::ITypedEventHandler<class Microsoft::UI::Dispatching::DispatcherQueue *,struct IInspectable *>,struct Microsoft::WRL::InvokeModeOptions<-2> > *)	未知
//   CoreMessagingXP.dll!Microsoft::WRL::EventSource<struct Windows::Foundation::ITypedEventHandler<class Microsoft::UI::Dispatching::DispatcherQueue *,struct IInspectable *>,struct Microsoft::WRL::InvokeModeOptions<-2> >::InvokeAll<class Microsoft::UI::Dispatching::DispatcherQueue *,std::nullptr_t>(class Microsoft::UI::Dispatching::DispatcherQueue *,std::nullptr_t)	未知
//   CoreMessagingXP.dll!Microsoft::UI::Dispatching::DispatcherQueueTimer::TimerCallback(void *)	未知
//   CoreMessagingXP.dll!CFlat::SehSafe::Execute<<lambda_e16aea3717fc5beac95aa2e513a8f395>>()	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::ActionCallback::ImportAdapter$(class CFlat::Box$1<struct CFlat::FunctionPointerAndUserData$1<long (*)(void *)> > *)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Support::ActivationContext::CallbackWithActivationContext(class Microsoft::CoreUI::Dispatch::Timeout *,struct System::UIntPtr)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::TimeoutManager::Callback_OnDispatch(void)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::Dispatcher::Callback_DispatchNextItem(class Microsoft::CoreUI::Dispatch::DispatchItem *)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::Dispatcher::Callback_DispatchLoop(enum Microsoft::CoreUI::Dispatch::RunnablePriorityMask)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::EventLoop::Callback_RunCoreLoop(enum Microsoft::CoreUI::Dispatch::RunMode)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::DrainCoreMessagingQueue(enum Microsoft::CoreUI::Dispatch::UserAdapter$UserPriority,void * *)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::OnUserDispatch(bool,enum Microsoft::CoreUI::Dispatch::UserAdapter$UserPriority,void * *)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::OnUserDispatchRaw(enum Microsoft::CoreUI::Dispatch::UserAdapter$UserPriority,bool,void * *)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::DoWork(struct HWND__ *,enum Microsoft::CoreUI::Dispatch::UserAdapter$UserPriority,bool)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::HandleDispatchNotifyMessage(struct HWND__ *,unsigned int,long)	未知
//   CoreMessagingXP.dll!Microsoft::CoreUI::Dispatch::UserAdapter::WindowProc(struct HWND__ *,unsigned int,unsigned int,long)	未知
//   user32.dll!__InternalCallWinProc@20()	未知
//   user32.dll!UserCallWinProcCheckWow(struct _ACTIVATION_CONTEXT *,void *,struct HWND__ *,enum _WM_VALUE,unsigned int,long,void *,int)	未知
//   user32.dll!DispatchClientMessage()	未知
//   user32.dll!___fnDWORD@4()	未知
//   ntdll.dll!_KiUserCallbackDispatcher@12()	未知
//   Microsoft.ui.xaml.dll!DirectUI::FrameworkApplication::StartDesktop(void)	未知
//   Microsoft.ui.xaml.dll!DirectUI::FrameworkApplicationFactory::Start()	未知
//   [托管到本机的转换]	
//   Microsoft.WinUI.dll!ABI.Microsoft.UI.Xaml.IApplicationStaticsMethods.Start(WinRT.IObjectReference _obj = {WinRT.ObjectReference<WinRT.Interop.IUnknownVftbl>}, Microsoft.UI.Xaml.ApplicationInitializationCallback callback = {Method = {System.Reflection.RuntimeMethodInfo}})	未知
//   Microsoft.WinUI.dll!Microsoft.UI.Xaml.Application.Start(Microsoft.UI.Xaml.ApplicationInitializationCallback callback = {Method = {System.Reflection.RuntimeMethodInfo}})	未知

// */
//                            Application.Current.DebugSettings.EnableFrameRateCounter = true;
//                            //ApplicationView.TryUnsnapToFullscreen();

//                            // System.InvalidCastException:“Specified cast is not valid.”
//                            // https://stackoverflow.com/questions/73936140/how-to-get-the-window-hosting-a-uielement-instance
//                            //var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(s);
//                        })
//                    }
//                };
//                window.Activate();
//            };
//        });
//    }
//}

//static class CsharpMarkup
//{
//    public static T Do<T>(this T element, Action<T> action) where T : UIElement
//    {
//        action(element);

//        return element;
//    }
//}