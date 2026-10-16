using System.Text;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;

namespace MediaSamples;

internal unsafe sealed partial class VideoDevice
{
    internal uint VideoWidth, VideoHeight;
    private ID3D11Buffer* _uvBuffer;
    private ID3D11VertexShader* _vertexShader;
    private ID3D11PixelShader* _pixelShader;
    private ID3D11SamplerState* _sampler;
    private ID3D11Texture2D* _staging;
    private ID3D11Texture2D* _luma;
    private ID3D11Texture2D* _chroma;
    private ID3D11ShaderResourceView* _lumaView;
    private ID3D11ShaderResourceView* _chromaView;
    private uint _shaderWidth, _shaderHeight;

    internal void ResizeOutput()
    {
        if (SwapChain == null) return;
        RECT client;
        if (!PInvoke.GetClientRect(_window, &client)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        int width = client.right - client.left, height = client.bottom - client.top;
        if (width <= 0 || height <= 0 || (width == Width && height == Height)) return;
        Context->OMSetRenderTargets(0, null, null);
        Release(Target); Target = null;
        SwapChain->ResizeBuffers(0, (uint)width, (uint)height, DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, 0).ThrowOnFailure();
        void* target = null;
        var iid = typeof(ID3D11Texture2D).GUID;
        SwapChain->GetBuffer(0, &iid, &target).ThrowOnFailure();
        Target = (ID3D11Texture2D*)target;
        Width = width; Height = height;
        Release(_processor); _processor = null;
        Release(_enumerator); _enumerator = null;
        Console.WriteLine($"Output resized: {Width}x{Height}.");
    }

    private ID3DBlob* CompileShader(string source, string entry, string profile)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        byte[] entryBytes = Encoding.ASCII.GetBytes(entry + '\0');
        byte[] profileBytes = Encoding.ASCII.GetBytes(profile + '\0');
        ID3DBlob* code = null;
        ID3DBlob* errors = null;
        try
        {
            fixed (byte* data = bytes)
            fixed (byte* entryPoint = entryBytes)
            fixed (byte* target = profileBytes)
            {
                var status = PInvoke.D3DCompile(data, (nuint)bytes.Length, default, null, null,
                    new PCSTR(entryPoint), new PCSTR(target), 0, 0, &code, &errors);
                if (status.Failed)
                    throw new COMException(errors == null ? "Shader compilation failed." : Marshal.PtrToStringAnsi((nint)errors->GetBufferPointer()), status.Value);
            }
            return code;
        }
        finally { Release(errors); }
    }

    private void InitializeShaders()
    {
        const string source = """
            cbuffer SourceSize : register(b0) { float4 uvScale; };
            Texture2D<float> yPlane : register(t0);
            Texture2D<float2> uvPlane : register(t1);
            SamplerState linearSampler : register(s0);
            struct Vertex { float4 position : SV_Position; float2 uv : TEXCOORD0; };
            Vertex VS(uint id : SV_VertexID) {
                Vertex v;
                v.uv = float2((id << 1) & 2, id & 2);
                v.position = float4(v.uv * float2(2, -2) + float2(-1, 1), 0, 1);
                return v;
            }
            float4 PS(Vertex v) : SV_Target {
                v.uv *= uvScale.xy;
                float y = (yPlane.Sample(linearSampler, v.uv) - 16.0 / 255.0) * 1.164384;
                float2 uv = uvPlane.Sample(linearSampler, v.uv) - 128.0 / 255.0;
                return float4(saturate(float3(y + 1.792741 * uv.y,
                    y - 0.213249 * uv.x - 0.532909 * uv.y, y + 2.112402 * uv.x)), 1);
            }
            """;
        ID3DBlob* vertex = null;
        ID3DBlob* pixel = null;
        try
        {
            vertex = CompileShader(source, "VS", "vs_4_0");
            pixel = CompileShader(source, "PS", "ps_4_0");
            ID3D11VertexShader* vs = null;
            Device->CreateVertexShader(vertex->GetBufferPointer(), vertex->GetBufferSize(), (ID3D11ClassLinkage*)null, &vs).ThrowOnFailure();
            _vertexShader = vs;
            ID3D11PixelShader* ps = null;
            Device->CreatePixelShader(pixel->GetBufferPointer(), pixel->GetBufferSize(), (ID3D11ClassLinkage*)null, &ps).ThrowOnFailure();
            _pixelShader = ps;
            var desc = new D3D11_SAMPLER_DESC {
                Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
                AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
                AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
                AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
                MaxLOD = float.MaxValue
            };
            ID3D11SamplerState* sampler = null;
            Device->CreateSamplerState(&desc, &sampler).ThrowOnFailure();
            _sampler = sampler;
            var bufferDesc = new D3D11_BUFFER_DESC { ByteWidth = 16, BindFlags = D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER };
            ID3D11Buffer* buffer = null;
            Device->CreateBuffer(&bufferDesc, (D3D11_SUBRESOURCE_DATA*)null, &buffer).ThrowOnFailure();
            _uvBuffer = buffer;
        }
        finally { Release(pixel); Release(vertex); }
    }

    private void CreatePlane(uint width, uint height, DXGI_FORMAT format, out ID3D11Texture2D* texture, out ID3D11ShaderResourceView* view)
    {
        var desc = new D3D11_TEXTURE2D_DESC {
            Width = width, Height = height, MipLevels = 1, ArraySize = 1,
            Format = format, SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
            BindFlags = D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE
        };
        ID3D11Texture2D* value = null;
        Device->CreateTexture2D(&desc, (D3D11_SUBRESOURCE_DATA*)null, &value).ThrowOnFailure();
        texture = value;
        ID3D11ShaderResourceView* resourceView = null;
        Device->CreateShaderResourceView((ID3D11Resource*)value, (D3D11_SHADER_RESOURCE_VIEW_DESC*)null, &resourceView).ThrowOnFailure();
        view = resourceView;
    }

    private void ProcessWithShader(ID3D11Texture2D* texture, uint subresource)
    {
        if (_vertexShader == null) InitializeShaders();
        D3D11_TEXTURE2D_DESC desc;
        texture->GetDesc(&desc);
        if (desc.Format != DXGI_FORMAT.DXGI_FORMAT_NV12) throw new NotSupportedException($"Shader conversion requires NV12, received {desc.Format}.");
        if (_staging == null || _shaderWidth != desc.Width || _shaderHeight != desc.Height)
        {
            DisposePlanes();
            _shaderWidth = desc.Width; _shaderHeight = desc.Height;
            desc.ArraySize = 1; desc.MipLevels = 1;
            desc.BindFlags = 0; desc.MiscFlags = 0;
            desc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
            desc.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
            ID3D11Texture2D* staging = null;
            Device->CreateTexture2D(&desc, (D3D11_SUBRESOURCE_DATA*)null, &staging).ThrowOnFailure();
            _staging = staging;
            CreatePlane(desc.Width, desc.Height, DXGI_FORMAT.DXGI_FORMAT_R8_UNORM, out _luma, out _lumaView);
            CreatePlane(desc.Width / 2, desc.Height / 2, DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM, out _chroma, out _chromaView);
            Console.WriteLine($"NV12 GPU shader source: {desc.Width}x{desc.Height}; output: {Width}x{Height}.");
        }
        // Decoder-only NV12 textures need a staging transfer on drivers without video views.
        Context->CopySubresourceRegion((ID3D11Resource*)_staging, 0, 0, 0, 0, (ID3D11Resource*)texture, subresource, (D3D11_BOX*)null);
        D3D11_MAPPED_SUBRESOURCE mapped;
        Context->Map((ID3D11Resource*)_staging, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped).ThrowOnFailure();
        try
        {
            Context->UpdateSubresource((ID3D11Resource*)_luma, 0, (D3D11_BOX*)null, mapped.pData, mapped.RowPitch, 0);
            Context->UpdateSubresource((ID3D11Resource*)_chroma, 0, (D3D11_BOX*)null, (byte*)mapped.pData + mapped.RowPitch * _shaderHeight, mapped.RowPitch, 0);
        }
        finally { Context->Unmap((ID3D11Resource*)_staging, 0); }
        ID3D11RenderTargetView* output = null;
        ID3D11ShaderResourceView** empty = stackalloc ID3D11ShaderResourceView*[2];
        empty[0] = null; empty[1] = null;
        try
        {
            Device->CreateRenderTargetView((ID3D11Resource*)Target, (D3D11_RENDER_TARGET_VIEW_DESC*)null, &output).ThrowOnFailure();
            Context->OMSetRenderTargets(1, &output, null);
            var viewport = new D3D11_VIEWPORT { Width = Width, Height = Height, MaxDepth = 1 };
            Context->RSSetViewports(1, &viewport);
            Context->IASetInputLayout(null);
            Context->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
            Context->VSSetShader(_vertexShader, null, 0);
            Context->PSSetShader(_pixelShader, null, 0);
            float* scale = stackalloc float[4] { VideoWidth == 0 ? 1 : (float)VideoWidth / _shaderWidth, VideoHeight == 0 ? 1 : (float)VideoHeight / _shaderHeight, 0, 0 };
            Context->UpdateSubresource((ID3D11Resource*)_uvBuffer, 0, (D3D11_BOX*)null, scale, 0, 0);
            var constants = _uvBuffer;
            Context->PSSetConstantBuffers(0, 1, &constants);
            ID3D11ShaderResourceView** views = stackalloc ID3D11ShaderResourceView*[2] { _lumaView, _chromaView };
            Context->PSSetShaderResources(0, 2, views);
            var sampler = _sampler;
            Context->PSSetSamplers(0, 1, &sampler);
            Context->Draw(3, 0);
        }
        finally
        {
            Context->PSSetShaderResources(0, 2, empty);
            Context->OMSetRenderTargets(0, null, null);
            Release(output);
        }
    }

    private void DisposePlanes()
    {
        Release(_chromaView); _chromaView = null;
        Release(_lumaView); _lumaView = null;
        Release(_chroma); _chroma = null;
        Release(_luma); _luma = null;
        Release(_staging); _staging = null;
    }

    private void DisposeShaders()
    {
        DisposePlanes();
        Release(_uvBuffer); _uvBuffer = null;
        Release(_sampler); _sampler = null;
        Release(_pixelShader); _pixelShader = null;
        Release(_vertexShader); _vertexShader = null;
    }
}
