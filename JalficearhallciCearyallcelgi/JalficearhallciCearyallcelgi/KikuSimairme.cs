using System;
using System.Drawing;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;

namespace JalficearhallciCearyallcelgi
{
    class KikuSimairme : IDisposable
    {
        public KikuSimairme()
        {
            _renderForm = new RenderForm
            {
                ClientSize = new Size(Width, Height)
            };
            InitializeDeviceResources();
        }

        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // 释放顺序有要求
            _renderTargetView.Dispose();
            _swapChain.Dispose();
            _d3DDevice.Dispose();
            _d3DDeviceContext.Dispose();
            _renderForm?.Dispose();
        }

        private void RenderCallback()
        {
            Draw();
        }

        private void InitializeDeviceResources()
        {
            // 模式描述
            ModeDescription backBufferDesc =
                new ModeDescription
                (
                    Width,
                    Height,
                    // 表示刷新率，这里使用的就是 `1/60` 也就是 60hz 
                    new Rational(60, 1),
                    // 表示 RGBA 的颜色，注意颜色的顺序哦
                    Format.R8G8B8A8_UNorm
                );

            // 创建交换链的描述
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                // 多重采用 SampleDescription 用来优化图片，是一种用于采样和平衡渲染像素的创建亮丽色彩变化之间的平滑过渡的一种技术，这里设置等级 1 也就是1重采样，需要传入两个参数一个是Count 指定每个像素的采样数量，一个是Quality指定希望得到的质量级别
                SampleDescription = new SampleDescription(count: 1 /*每个像素的采样数量*/, quality: 0 /*质量级别*/),
                // 设置 CPU 访问缓冲的权限，这里设置可以访问 RenderTarget 输出
                Usage = Usage.RenderTargetOutput,
                // 后缓冲数量 BufferCount 建议设置一个，设置一个就是双缓冲。两个缓冲区已经足够用了
                BufferCount = 1,
                // 获取渲染窗口句柄
                OutputHandle = _renderForm.Handle,
                // 这个值设置是否希望是全屏，如果是 true 就是窗口
                IsWindowed = true
            };

            D3D11.Device.CreateWithSwapChain
            (
                /*
                 * 第一个参数 DriverType.Hardware 表示希望使用 GPU 渲染，设置 驱动设备类型 可以设置硬件设备（hardware device）、参考设备（reference device）、软件驱动设备（software driver device）
                   
                   - 硬件设备（hardware device）是一个运行在显卡上的D3D设备，在所有设备中运行速度是最快的
                   
                   - 软件驱动设备（software driverdevice）是开发人员自己编写的用于Direct3D的渲染驱动软件
                   
                   - 参考设备（reference device）是用于没有可用的硬件支持时在CPU上进行渲染的设备
                   
                   - WARP设备（WARPdevice）是一种高效的CPU渲染设备，可以模拟现阶段所有的Direct3D特性
                 */
                DriverType.Hardware,
                // 第二个参数选不使用特殊的方法，参见 [D3D11_CREATE_DEVICE_FLAG enumeration](https://msdn.microsoft.com/en-us/library/windows/desktop/ff476107(v=vs.85).aspx )
                D3D11.DeviceCreationFlags.None,
                // 第三个参数是输入上面的交换链描述
                swapChainDesc,
                // D3D设备（ID3D11Device）通常代表一个显示适配器（即显卡），它最主要的功能是用于创建各种所需资源，最常用的资源有：资源类（ID3D11Resource, 包含纹理和缓冲区），视图类以及着色器。此外，D3D设备还能够用于检测系统环境对功能的支持情况
                out _d3DDevice,
                out _swapChain);
            // D3D设备上下文(ID3D11DeviceContext)可以看做是一个渲染管线。通常我们在创建D3D设备的同时也会附赠一个立即设备上下文(Immediate Context)。一个D3D设备仅对应一个D3D立即设备上下文
            // 渲染管线主要负责渲染和计算工作，它需要绑定来自与它关联的D3D设备所创建的各种资源、视图和着色器才能正常运转，除此之外，它还能够负责对资源的直接读写操作
            _d3DDeviceContext = _d3DDevice.ImmediateContext;

            using (D3D11.Texture2D backBuffer = _swapChain.GetBackBuffer<D3D11.Texture2D>(0))
            {
                _renderTargetView = new D3D11.RenderTargetView(_d3DDevice, backBuffer);
            }
        }


        private const int Width = 1280;

        private const int Height = 720;

        private readonly RenderForm _renderForm;

        private D3D11.Device _d3DDevice;
        private D3D11.DeviceContext _d3DDeviceContext;
        private SwapChain _swapChain;
        private D3D11.RenderTargetView _renderTargetView;

        private void Draw()
        {
            _d3DDeviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            _d3DDeviceContext.ClearRenderTargetView(_renderTargetView, ColorToRaw4(Color.Coral));

            _swapChain.Present(1, PresentFlags.None);

            RawColor4 ColorToRaw4(Color color)
            {
                const float n = 255f;
                return new RawColor4(color.R / n, color.G / n, color.B / n, color.A / n);
            }
        }
    }
}