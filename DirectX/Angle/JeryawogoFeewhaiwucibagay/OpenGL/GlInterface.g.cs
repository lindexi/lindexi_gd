using System;
using System.Collections.Generic;
using System.Text;

namespace JeryawogoFeewhaiwucibagay.OpenGL;

unsafe partial class GlInterface
{
    delegate* unmanaged[Stdcall]<int, void> _addr_ClearStencil;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyClearStencil(int a0);
    public partial void ClearStencil(int @s)
    {
        _addr_ClearStencil(@s);
    }
    delegate* unmanaged[Stdcall]<float, float, float, float, void> _addr_ClearColor;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyClearColor(float a0, float a1, float a2, float a3);
    public partial void ClearColor(float @r, float @g, float @b, float @a)
    {
        _addr_ClearColor(@r, @g, @b, @a);
    }
    delegate* unmanaged[Stdcall]<double, void> _addr_ClearDepthDouble;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyClearDepthDouble(double a0);
    internal partial void ClearDepthDouble(double @value)
    {
        if (_addr_ClearDepthDouble == null) throw new System.EntryPointNotFoundException("ClearDepthDouble");
        _addr_ClearDepthDouble(@value);
    }
    internal bool IsClearDepthDoubleAvailable => _addr_ClearDepthDouble != null;
    delegate* unmanaged[Stdcall]<float, void> _addr_ClearDepthFloat;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyClearDepthFloat(float a0);
    internal partial void ClearDepthFloat(float @value)
    {
        if (_addr_ClearDepthFloat == null) throw new System.EntryPointNotFoundException("ClearDepthFloat");
        _addr_ClearDepthFloat(@value);
    }
    internal bool IsClearDepthFloatAvailable => _addr_ClearDepthFloat != null;
    delegate* unmanaged[Stdcall]<int, void> _addr_DepthFunc;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDepthFunc(int a0);
    public partial void DepthFunc(int @value)
    {
        _addr_DepthFunc(@value);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_DepthMask;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDepthMask(int a0);
    public partial void DepthMask(int @value)
    {
        _addr_DepthMask(@value);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_Clear;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyClear(int a0);
    public partial void Clear(int @bits)
    {
        _addr_Clear(@bits);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, void> _addr_Viewport;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyViewport(int a0, int a1, int a2, int a3);
    public partial void Viewport(int @x, int @y, int @width, int @height)
    {
        _addr_Viewport(@x, @y, @width, @height);
    }
    delegate* unmanaged[Stdcall]<void> _addr_Flush;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyFlush();
    public partial void Flush()
    {
        _addr_Flush();
    }
    delegate* unmanaged[Stdcall]<void> _addr_Finish;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyFinish();
    public partial void Finish()
    {
        _addr_Finish();
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GenFramebuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGenFramebuffers(int a0, int* a1);
    public partial void GenFramebuffers(int @count, int* @res)
    {
        _addr_GenFramebuffers(@count, @res);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_DeleteFramebuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteFramebuffers(int a0, int* a1);
    public partial void DeleteFramebuffers(int @count, int* @framebuffers)
    {
        _addr_DeleteFramebuffers(@count, @framebuffers);
    }
    delegate* unmanaged[Stdcall]<int, int, void> _addr_BindFramebuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindFramebuffer(int a0, int a1);
    public partial void BindFramebuffer(int @target, int @fb)
    {
        _addr_BindFramebuffer(@target, @fb);
    }
    delegate* unmanaged[Stdcall]<int, int> _addr_CheckFramebufferStatus;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyCheckFramebufferStatus(int a0);
    public partial int CheckFramebufferStatus(int @target)
    {
        return _addr_CheckFramebufferStatus(@target);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, int, int, void> _addr_BlitFramebuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBlitFramebuffer(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9);
    public partial void BlitFramebuffer(int @srcX0, int @srcY0, int @srcX1, int @srcY1, int @dstX0, int @dstY0, int @dstX1, int @dstY1, int @mask, int @filter)
    {
        if (_addr_BlitFramebuffer == null) throw new System.EntryPointNotFoundException("BlitFramebuffer");
        _addr_BlitFramebuffer(@srcX0, @srcY0, @srcX1, @srcY1, @dstX0, @dstY0, @dstX1, @dstY1, @mask, @filter);
    }
    public bool IsBlitFramebufferAvailable => _addr_BlitFramebuffer != null;
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GenRenderbuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGenRenderbuffers(int a0, int* a1);
    public partial void GenRenderbuffers(int @count, int* @res)
    {
        _addr_GenRenderbuffers(@count, @res);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_DeleteRenderbuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteRenderbuffers(int a0, int* a1);
    public partial void DeleteRenderbuffers(int @count, int* @renderbuffers)
    {
        _addr_DeleteRenderbuffers(@count, @renderbuffers);
    }
    delegate* unmanaged[Stdcall]<int, int, void> _addr_BindRenderbuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindRenderbuffer(int a0, int a1);
    public partial void BindRenderbuffer(int @target, int @fb)
    {
        _addr_BindRenderbuffer(@target, @fb);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, void> _addr_RenderbufferStorage;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyRenderbufferStorage(int a0, int a1, int a2, int a3);
    public partial void RenderbufferStorage(int @target, int @internalFormat, int @width, int @height)
    {
        _addr_RenderbufferStorage(@target, @internalFormat, @width, @height);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, void> _addr_FramebufferRenderbuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyFramebufferRenderbuffer(int a0, int a1, int a2, int a3);
    public partial void FramebufferRenderbuffer(int @target, int @attachment, int @renderbufferTarget, int @renderbuffer)
    {
        _addr_FramebufferRenderbuffer(@target, @attachment, @renderbufferTarget, @renderbuffer);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GenTextures;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGenTextures(int a0, int* a1);
    public partial void GenTextures(int @count, int* @res)
    {
        _addr_GenTextures(@count, @res);
    }
    delegate* unmanaged[Stdcall]<int, int, void> _addr_BindTexture;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindTexture(int a0, int a1);
    public partial void BindTexture(int @target, int @fb)
    {
        _addr_BindTexture(@target, @fb);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_ActiveTexture;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyActiveTexture(int a0);
    public partial void ActiveTexture(int @texture)
    {
        _addr_ActiveTexture(@texture);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_DeleteTextures;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteTextures(int a0, int* a1);
    public partial void DeleteTextures(int @count, int* @textures)
    {
        _addr_DeleteTextures(@count, @textures);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, nint, void> _addr_TexImage2D;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyTexImage2D(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, nint a8);
    public partial void TexImage2D(int @target, int @level, int @internalFormat, int @width, int @height, int @border, int @format, int @type, nint @data)
    {
        _addr_TexImage2D(@target, @level, @internalFormat, @width, @height, @border, @format, @type, @data);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, void> _addr_CopyTexSubImage2D;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyCopyTexSubImage2D(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7);
    public partial void CopyTexSubImage2D(int @target, int @level, int @xoffset, int @yoffset, int @x, int @y, int @width, int @height)
    {
        _addr_CopyTexSubImage2D(@target, @level, @xoffset, @yoffset, @x, @y, @width, @height);
    }
    delegate* unmanaged[Stdcall]<int, int, int, void> _addr_TexParameteri;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyTexParameteri(int a0, int a1, int a2);
    public partial void TexParameteri(int @target, int @name, int @value)
    {
        _addr_TexParameteri(@target, @name, @value);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, int, void> _addr_FramebufferTexture2D;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyFramebufferTexture2D(int a0, int a1, int a2, int a3, int a4);
    public partial void FramebufferTexture2D(int @target, int @attachment, int @texTarget, int @texture, int @level)
    {
        _addr_FramebufferTexture2D(@target, @attachment, @texTarget, @texture, @level);
    }
    delegate* unmanaged[Stdcall]<int, int> _addr_CreateShader;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyCreateShader(int a0);
    public partial int CreateShader(int @shaderType)
    {
        return _addr_CreateShader(@shaderType);
    }
    delegate* unmanaged[Stdcall]<int, int, nint, nint, void> _addr_ShaderSource;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyShaderSource(int a0, int a1, nint a2, nint a3);
    public partial void ShaderSource(int @shader, int @count, nint @strings, nint @lengths)
    {
        _addr_ShaderSource(@shader, @count, @strings, @lengths);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_CompileShader;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyCompileShader(int a0);
    public partial void CompileShader(int @shader)
    {
        _addr_CompileShader(@shader);
    }
    delegate* unmanaged[Stdcall]<int, int, int*, void> _addr_GetShaderiv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetShaderiv(int a0, int a1, int* a2);
    public partial void GetShaderiv(int @shader, int @name, int* @parameters)
    {
        _addr_GetShaderiv(@shader, @name, @parameters);
    }
    delegate* unmanaged[Stdcall]<int, int, int*, void*, void> _addr_GetShaderInfoLog;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetShaderInfoLog(int a0, int a1, int* a2, void* a3);
    public partial void GetShaderInfoLog(int @shader, int @maxLength, out int @length, void* @infoLog)
    {
        fixed (int* @__p_length = &length)
            _addr_GetShaderInfoLog(@shader, @maxLength, @__p_length, @infoLog);
    }
    delegate* unmanaged[Stdcall]<int> _addr_CreateProgram;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyCreateProgram();
    public partial int CreateProgram()
    {
        return _addr_CreateProgram();
    }
    delegate* unmanaged[Stdcall]<int, int, void> _addr_AttachShader;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyAttachShader(int a0, int a1);
    public partial void AttachShader(int @program, int @shader)
    {
        _addr_AttachShader(@program, @shader);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_LinkProgram;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyLinkProgram(int a0);
    public partial void LinkProgram(int @program)
    {
        _addr_LinkProgram(@program);
    }
    delegate* unmanaged[Stdcall]<int, int, int*, void> _addr_GetProgramiv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetProgramiv(int a0, int a1, int* a2);
    public partial void GetProgramiv(int @program, int @name, int* @parameters)
    {
        _addr_GetProgramiv(@program, @name, @parameters);
    }
    delegate* unmanaged[Stdcall]<int, int, int*, void*, void> _addr_GetProgramInfoLog;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetProgramInfoLog(int a0, int a1, int* a2, void* a3);
    public partial void GetProgramInfoLog(int @program, int @maxLength, out int @len, void* @infoLog)
    {
        fixed (int* @__p_len = &len)
            _addr_GetProgramInfoLog(@program, @maxLength, @__p_len, @infoLog);
    }
    delegate* unmanaged[Stdcall]<int, int, nint, void> _addr_BindAttribLocation;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindAttribLocation(int a0, int a1, nint a2);
    public partial void BindAttribLocation(int @program, int @index, nint @name)
    {
        _addr_BindAttribLocation(@program, @index, @name);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GenBuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGenBuffers(int a0, int* a1);
    public partial void GenBuffers(int @len, int* @rv)
    {
        _addr_GenBuffers(@len, @rv);
    }
    delegate* unmanaged[Stdcall]<int, int, void> _addr_BindBuffer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindBuffer(int a0, int a1);
    public partial void BindBuffer(int @target, int @buffer)
    {
        _addr_BindBuffer(@target, @buffer);
    }
    delegate* unmanaged[Stdcall]<int, nint, nint, int, void> _addr_BufferData;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBufferData(int a0, nint a1, nint a2, int a3);
    public partial void BufferData(int @target, nint @size, nint @data, int @usage)
    {
        _addr_BufferData(@target, @size, @data, @usage);
    }
    delegate* unmanaged[Stdcall]<int, nint, int> _addr_GetAttribLocation;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyGetAttribLocation(int a0, nint a1);
    public partial int GetAttribLocation(int @program, nint @name)
    {
        return _addr_GetAttribLocation(@program, @name);
    }
    delegate* unmanaged[Stdcall]<int, int, int, int, int, nint, void> _addr_VertexAttribPointer;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyVertexAttribPointer(int a0, int a1, int a2, int a3, int a4, nint a5);
    public partial void VertexAttribPointer(int @index, int @size, int @type, int @normalized, int @stride, nint @pointer)
    {
        _addr_VertexAttribPointer(@index, @size, @type, @normalized, @stride, @pointer);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_EnableVertexAttribArray;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyEnableVertexAttribArray(int a0);
    public partial void EnableVertexAttribArray(int @index)
    {
        _addr_EnableVertexAttribArray(@index);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_UseProgram;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyUseProgram(int a0);
    public partial void UseProgram(int @program)
    {
        _addr_UseProgram(@program);
    }
    delegate* unmanaged[Stdcall]<int, int, nint, void> _addr_DrawArrays;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDrawArrays(int a0, int a1, nint a2);
    public partial void DrawArrays(int @mode, int @first, nint @count)
    {
        _addr_DrawArrays(@mode, @first, @count);
    }
    delegate* unmanaged[Stdcall]<int, int, int, nint, void> _addr_DrawElements;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDrawElements(int a0, int a1, int a2, nint a3);
    public partial void DrawElements(int @mode, int @count, int @type, nint @indices)
    {
        _addr_DrawElements(@mode, @count, @type, @indices);
    }
    delegate* unmanaged[Stdcall]<int, nint, int> _addr_GetUniformLocation;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate int __wasmDummyGetUniformLocation(int a0, nint a1);
    public partial int GetUniformLocation(int @program, nint @name)
    {
        return _addr_GetUniformLocation(@program, @name);
    }
    delegate* unmanaged[Stdcall]<int, float, void> _addr_Uniform1f;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyUniform1f(int a0, float a1);
    public partial void Uniform1f(int @location, float @falue)
    {
        _addr_Uniform1f(@location, @falue);
    }
    delegate* unmanaged[Stdcall]<int, int, int, void*, void> _addr_UniformMatrix4fv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyUniformMatrix4fv(int a0, int a1, int a2, void* a3);
    public partial void UniformMatrix4fv(int @location, int @count, bool @transpose, void* @value)
    {
        _addr_UniformMatrix4fv(@location, @count, @transpose ? 1 : 0, @value);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_Enable;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyEnable(int a0);
    public partial void Enable(int @what)
    {
        _addr_Enable(@what);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_Disable;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDisable(int a0);
    public partial void Disable(int @what)
    {
        _addr_Disable(@what);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_DeleteBuffers;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteBuffers(int a0, int* a1);
    public partial void DeleteBuffers(int @count, int* @buffers)
    {
        _addr_DeleteBuffers(@count, @buffers);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_DeleteProgram;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteProgram(int a0);
    public partial void DeleteProgram(int @program)
    {
        _addr_DeleteProgram(@program);
    }
    delegate* unmanaged[Stdcall]<int, void> _addr_DeleteShader;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteShader(int a0);
    public partial void DeleteShader(int @shader)
    {
        _addr_DeleteShader(@shader);
    }
    delegate* unmanaged[Stdcall]<int, int, int*, void> _addr_GetRenderbufferParameteriv;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGetRenderbufferParameteriv(int a0, int a1, int* a2);
    public partial void GetRenderbufferParameteriv(int @target, int @name, out int @value)
    {
        fixed (int* @__p_value = &value)
            _addr_GetRenderbufferParameteriv(@target, @name, @__p_value);
    }
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_DeleteVertexArrays;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyDeleteVertexArrays(int a0, int* a1);
    public partial void DeleteVertexArrays(int @count, int* @arrays)
    {
        if (_addr_DeleteVertexArrays == null) throw new System.EntryPointNotFoundException("DeleteVertexArrays");
        _addr_DeleteVertexArrays(@count, @arrays);
    }
    public bool IsDeleteVertexArraysAvailable => _addr_DeleteVertexArrays != null;
    delegate* unmanaged[Stdcall]<int, void> _addr_BindVertexArray;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyBindVertexArray(int a0);
    public partial void BindVertexArray(int @array)
    {
        if (_addr_BindVertexArray == null) throw new System.EntryPointNotFoundException("BindVertexArray");
        _addr_BindVertexArray(@array);
    }
    public bool IsBindVertexArrayAvailable => _addr_BindVertexArray != null;
    delegate* unmanaged[Stdcall]<int, int*, void> _addr_GenVertexArrays;
    [global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate void __wasmDummyGenVertexArrays(int a0, int* a1);
    public partial void GenVertexArrays(int @n, int* @rv)
    {
        if (_addr_GenVertexArrays == null) throw new System.EntryPointNotFoundException("GenVertexArrays");
        _addr_GenVertexArrays(@n, @rv);
    }
    public bool IsGenVertexArraysAvailable => _addr_GenVertexArrays != null;
    void Initialize(Func<string, IntPtr> getProcAddress, global::JeryawogoFeewhaiwucibagay.OpenGL.GlInterface.GlContextInfo context)
    {
        var addr = IntPtr.Zero;
        // Initializing ClearStencil
        addr = IntPtr.Zero;
        addr = getProcAddress("glClearStencil");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_ClearStencil");
        _addr_ClearStencil = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing ClearColor
        addr = IntPtr.Zero;
        addr = getProcAddress("glClearColor");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_ClearColor");
        _addr_ClearColor = (delegate* unmanaged[Stdcall]<float, float, float, float, void>) addr;
        // Initializing ClearDepthDouble
        addr = IntPtr.Zero;
        addr = getProcAddress("glClearDepth");
        _addr_ClearDepthDouble = (delegate* unmanaged[Stdcall]<double, void>) addr;
        // Initializing ClearDepthFloat
        addr = IntPtr.Zero;
        addr = getProcAddress("glClearDepthf");
        _addr_ClearDepthFloat = (delegate* unmanaged[Stdcall]<float, void>) addr;
        // Initializing DepthFunc
        addr = IntPtr.Zero;
        addr = getProcAddress("glDepthFunc");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DepthFunc");
        _addr_DepthFunc = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing DepthMask
        addr = IntPtr.Zero;
        addr = getProcAddress("glDepthMask");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DepthMask");
        _addr_DepthMask = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing Clear
        addr = IntPtr.Zero;
        addr = getProcAddress("glClear");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Clear");
        _addr_Clear = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing Viewport
        addr = IntPtr.Zero;
        addr = getProcAddress("glViewport");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Viewport");
        _addr_Viewport = (delegate* unmanaged[Stdcall]<int, int, int, int, void>) addr;
        // Initializing Flush
        addr = IntPtr.Zero;
        addr = getProcAddress("glFlush");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Flush");
        _addr_Flush = (delegate* unmanaged[Stdcall]<void>) addr;
        // Initializing Finish
        addr = IntPtr.Zero;
        addr = getProcAddress("glFinish");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Finish");
        _addr_Finish = (delegate* unmanaged[Stdcall]<void>) addr;
        // Initializing GenFramebuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glGenFramebuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GenFramebuffers");
        _addr_GenFramebuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing DeleteFramebuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteFramebuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteFramebuffers");
        _addr_DeleteFramebuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing BindFramebuffer
        addr = IntPtr.Zero;
        addr = getProcAddress("glBindFramebuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindFramebuffer");
        _addr_BindFramebuffer = (delegate* unmanaged[Stdcall]<int, int, void>) addr;
        // Initializing CheckFramebufferStatus
        addr = IntPtr.Zero;
        addr = getProcAddress("glCheckFramebufferStatus");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CheckFramebufferStatus");
        _addr_CheckFramebufferStatus = (delegate* unmanaged[Stdcall]<int, int>) addr;
        // Initializing BlitFramebuffer
        addr = IntPtr.Zero;
        addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlMinVersionEntryPoint.GetProcAddress(getProcAddress, context, "glBlitFramebuffer", 3, 0);
        _addr_BlitFramebuffer = (delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, int, int, void>) addr;
        // Initializing GenRenderbuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glGenRenderbuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GenRenderbuffers");
        _addr_GenRenderbuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing DeleteRenderbuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteRenderbuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteRenderbuffers");
        _addr_DeleteRenderbuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing BindRenderbuffer
        addr = IntPtr.Zero;
        addr = getProcAddress("glBindRenderbuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindRenderbuffer");
        _addr_BindRenderbuffer = (delegate* unmanaged[Stdcall]<int, int, void>) addr;
        // Initializing RenderbufferStorage
        addr = IntPtr.Zero;
        addr = getProcAddress("glRenderbufferStorage");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_RenderbufferStorage");
        _addr_RenderbufferStorage = (delegate* unmanaged[Stdcall]<int, int, int, int, void>) addr;
        // Initializing FramebufferRenderbuffer
        addr = IntPtr.Zero;
        addr = getProcAddress("glFramebufferRenderbuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_FramebufferRenderbuffer");
        _addr_FramebufferRenderbuffer = (delegate* unmanaged[Stdcall]<int, int, int, int, void>) addr;
        // Initializing GenTextures
        addr = IntPtr.Zero;
        addr = getProcAddress("glGenTextures");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GenTextures");
        _addr_GenTextures = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing BindTexture
        addr = IntPtr.Zero;
        addr = getProcAddress("glBindTexture");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindTexture");
        _addr_BindTexture = (delegate* unmanaged[Stdcall]<int, int, void>) addr;
        // Initializing ActiveTexture
        addr = IntPtr.Zero;
        addr = getProcAddress("glActiveTexture");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_ActiveTexture");
        _addr_ActiveTexture = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing DeleteTextures
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteTextures");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteTextures");
        _addr_DeleteTextures = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing TexImage2D
        addr = IntPtr.Zero;
        addr = getProcAddress("glTexImage2D");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_TexImage2D");
        _addr_TexImage2D = (delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, nint, void>) addr;
        // Initializing CopyTexSubImage2D
        addr = IntPtr.Zero;
        addr = getProcAddress("glCopyTexSubImage2D");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CopyTexSubImage2D");
        _addr_CopyTexSubImage2D = (delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, void>) addr;
        // Initializing TexParameteri
        addr = IntPtr.Zero;
        addr = getProcAddress("glTexParameteri");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_TexParameteri");
        _addr_TexParameteri = (delegate* unmanaged[Stdcall]<int, int, int, void>) addr;
        // Initializing FramebufferTexture2D
        addr = IntPtr.Zero;
        addr = getProcAddress("glFramebufferTexture2D");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_FramebufferTexture2D");
        _addr_FramebufferTexture2D = (delegate* unmanaged[Stdcall]<int, int, int, int, int, void>) addr;
        // Initializing CreateShader
        addr = IntPtr.Zero;
        addr = getProcAddress("glCreateShader");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreateShader");
        _addr_CreateShader = (delegate* unmanaged[Stdcall]<int, int>) addr;
        // Initializing ShaderSource
        addr = IntPtr.Zero;
        addr = getProcAddress("glShaderSource");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_ShaderSource");
        _addr_ShaderSource = (delegate* unmanaged[Stdcall]<int, int, nint, nint, void>) addr;
        // Initializing CompileShader
        addr = IntPtr.Zero;
        addr = getProcAddress("glCompileShader");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CompileShader");
        _addr_CompileShader = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing GetShaderiv
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetShaderiv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetShaderiv");
        _addr_GetShaderiv = (delegate* unmanaged[Stdcall]<int, int, int*, void>) addr;
        // Initializing GetShaderInfoLog
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetShaderInfoLog");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetShaderInfoLog");
        _addr_GetShaderInfoLog = (delegate* unmanaged[Stdcall]<int, int, int*, void*, void>) addr;
        // Initializing CreateProgram
        addr = IntPtr.Zero;
        addr = getProcAddress("glCreateProgram");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_CreateProgram");
        _addr_CreateProgram = (delegate* unmanaged[Stdcall]<int>) addr;
        // Initializing AttachShader
        addr = IntPtr.Zero;
        addr = getProcAddress("glAttachShader");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_AttachShader");
        _addr_AttachShader = (delegate* unmanaged[Stdcall]<int, int, void>) addr;
        // Initializing LinkProgram
        addr = IntPtr.Zero;
        addr = getProcAddress("glLinkProgram");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_LinkProgram");
        _addr_LinkProgram = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing GetProgramiv
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetProgramiv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetProgramiv");
        _addr_GetProgramiv = (delegate* unmanaged[Stdcall]<int, int, int*, void>) addr;
        // Initializing GetProgramInfoLog
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetProgramInfoLog");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetProgramInfoLog");
        _addr_GetProgramInfoLog = (delegate* unmanaged[Stdcall]<int, int, int*, void*, void>) addr;
        // Initializing BindAttribLocation
        addr = IntPtr.Zero;
        addr = getProcAddress("glBindAttribLocation");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindAttribLocation");
        _addr_BindAttribLocation = (delegate* unmanaged[Stdcall]<int, int, nint, void>) addr;
        // Initializing GenBuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glGenBuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GenBuffers");
        _addr_GenBuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing BindBuffer
        addr = IntPtr.Zero;
        addr = getProcAddress("glBindBuffer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BindBuffer");
        _addr_BindBuffer = (delegate* unmanaged[Stdcall]<int, int, void>) addr;
        // Initializing BufferData
        addr = IntPtr.Zero;
        addr = getProcAddress("glBufferData");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_BufferData");
        _addr_BufferData = (delegate* unmanaged[Stdcall]<int, nint, nint, int, void>) addr;
        // Initializing GetAttribLocation
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetAttribLocation");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetAttribLocation");
        _addr_GetAttribLocation = (delegate* unmanaged[Stdcall]<int, nint, int>) addr;
        // Initializing VertexAttribPointer
        addr = IntPtr.Zero;
        addr = getProcAddress("glVertexAttribPointer");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_VertexAttribPointer");
        _addr_VertexAttribPointer = (delegate* unmanaged[Stdcall]<int, int, int, int, int, nint, void>) addr;
        // Initializing EnableVertexAttribArray
        addr = IntPtr.Zero;
        addr = getProcAddress("glEnableVertexAttribArray");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_EnableVertexAttribArray");
        _addr_EnableVertexAttribArray = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing UseProgram
        addr = IntPtr.Zero;
        addr = getProcAddress("glUseProgram");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_UseProgram");
        _addr_UseProgram = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing DrawArrays
        addr = IntPtr.Zero;
        addr = getProcAddress("glDrawArrays");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DrawArrays");
        _addr_DrawArrays = (delegate* unmanaged[Stdcall]<int, int, nint, void>) addr;
        // Initializing DrawElements
        addr = IntPtr.Zero;
        addr = getProcAddress("glDrawElements");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DrawElements");
        _addr_DrawElements = (delegate* unmanaged[Stdcall]<int, int, int, nint, void>) addr;
        // Initializing GetUniformLocation
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetUniformLocation");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetUniformLocation");
        _addr_GetUniformLocation = (delegate* unmanaged[Stdcall]<int, nint, int>) addr;
        // Initializing Uniform1f
        addr = IntPtr.Zero;
        addr = getProcAddress("glUniform1f");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Uniform1f");
        _addr_Uniform1f = (delegate* unmanaged[Stdcall]<int, float, void>) addr;
        // Initializing UniformMatrix4fv
        addr = IntPtr.Zero;
        addr = getProcAddress("glUniformMatrix4fv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_UniformMatrix4fv");
        _addr_UniformMatrix4fv = (delegate* unmanaged[Stdcall]<int, int, int, void*, void>) addr;
        // Initializing Enable
        addr = IntPtr.Zero;
        addr = getProcAddress("glEnable");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Enable");
        _addr_Enable = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing Disable
        addr = IntPtr.Zero;
        addr = getProcAddress("glDisable");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_Disable");
        _addr_Disable = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing DeleteBuffers
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteBuffers");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteBuffers");
        _addr_DeleteBuffers = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
        // Initializing DeleteProgram
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteProgram");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteProgram");
        _addr_DeleteProgram = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing DeleteShader
        addr = IntPtr.Zero;
        addr = getProcAddress("glDeleteShader");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_DeleteShader");
        _addr_DeleteShader = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing GetRenderbufferParameteriv
        addr = IntPtr.Zero;
        addr = getProcAddress("glGetRenderbufferParameteriv");
        if (addr == IntPtr.Zero) throw new System.EntryPointNotFoundException("_addr_GetRenderbufferParameteriv");
        _addr_GetRenderbufferParameteriv = (delegate* unmanaged[Stdcall]<int, int, int*, void>) addr;

        // Initializing DeleteVertexArrays
        addr = IntPtr.Zero;
        addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlMinVersionEntryPoint.GetProcAddress(getProcAddress, context, "glDeleteVertexArrays", 3, 0);
        if (addr == IntPtr.Zero) addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlExtensionEntryPoint.GetProcAddress(getProcAddress, context, "glDeleteVertexArraysOES", "GL_OES_vertex_array_object");
        _addr_DeleteVertexArrays = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;

        // Initializing BindVertexArray
        addr = IntPtr.Zero;
        addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlMinVersionEntryPoint.GetProcAddress(getProcAddress, context, "glBindVertexArray", 3, 0);
        if (addr == IntPtr.Zero) addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlExtensionEntryPoint.GetProcAddress(getProcAddress, context, "glBindVertexArrayOES", "GL_OES_vertex_array_object");
        _addr_BindVertexArray = (delegate* unmanaged[Stdcall]<int, void>) addr;
        // Initializing GenVertexArrays
        addr = IntPtr.Zero;
        addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlMinVersionEntryPoint.GetProcAddress(getProcAddress, context, "glGenVertexArrays", 3, 0);
        if (addr == IntPtr.Zero) addr = global::JeryawogoFeewhaiwucibagay.OpenGL.GlExtensionEntryPoint.GetProcAddress(getProcAddress, context, "glGenVertexArraysOES", "GL_OES_vertex_array_object");
        _addr_GenVertexArrays = (delegate* unmanaged[Stdcall]<int, int*, void>) addr;
    }
}
