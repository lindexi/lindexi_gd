using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace HomographyShaderEffectDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PreviewImage.Effect = _effect;
        UpdateVisuals();
    }

    private const double ThumbRadius = 11;
    private readonly PerspectiveEffect _effect = new();
    private readonly Point[] _corners =
    [
        new(0.08, 0.10),
        new(0.78, 0.04),
        new(0.94, 0.82),
        new(0.12, 0.94)
    ];

    private void CornerThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var thumb = (Thumb)sender;
        var index = int.Parse((string)thumb.Tag);
        var current = _corners[index];
        var candidate = new Point(
            Math.Clamp(current.X + (e.HorizontalChange / Viewport.Width), 0, 1),
            Math.Clamp(current.Y + (e.VerticalChange / Viewport.Height), 0, 1));

        var previous = _corners[index];
        _corners[index] = candidate;

        if (!Homography.IsConvexQuadrilateral(_corners))
        {
            _corners[index] = previous;
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        var inverse = Homography.CreateQuadrilateralToUnitSquare(_corners);
        _effect.SetInverseMatrix(inverse);

        var points = _corners
            .Select(point => new Point(point.X * Viewport.Width, point.Y * Viewport.Height))
            .ToArray();

        Outline.Points = new PointCollection(points);
        PositionThumb(TopLeftThumb, points[0]);
        PositionThumb(TopRightThumb, points[1]);
        PositionThumb(BottomRightThumb, points[2]);
        PositionThumb(BottomLeftThumb, points[3]);
    }

    private static void PositionThumb(Thumb thumb, Point point)
    {
        Canvas.SetLeft(thumb, point.X - ThumbRadius);
        Canvas.SetTop(thumb, point.Y - ThumbRadius);
    }
}

internal static class Homography
{
    private const double Epsilon = 1e-8;

    public static double[,] CreateQuadrilateralToUnitSquare(IReadOnlyList<Point> corners)
    {
        ArgumentNullException.ThrowIfNull(corners);

        if (corners.Count != 4)
        {
            throw new ArgumentException("必须提供四个角点。", nameof(corners));
        }

        var forward = CreateUnitSquareToQuadrilateral(
            corners[0], corners[1], corners[2], corners[3]);
        return Invert(forward);
    }

    public static bool IsConvexQuadrilateral(IReadOnlyList<Point> corners)
    {
        ArgumentNullException.ThrowIfNull(corners);

        if (corners.Count != 4)
        {
            return false;
        }

        double? sign = null;
        for (var i = 0; i < corners.Count; i++)
        {
            var first = corners[i];
            var second = corners[(i + 1) % corners.Count];
            var third = corners[(i + 2) % corners.Count];
            var cross = ((second.X - first.X) * (third.Y - second.Y))
                        - ((second.Y - first.Y) * (third.X - second.X));

            if (Math.Abs(cross) < 0.002)
            {
                return false;
            }

            var currentSign = Math.Sign(cross);
            sign ??= currentSign;
            if (sign != currentSign)
            {
                return false;
            }
        }

        return true;
    }

    private static double[,] CreateUnitSquareToQuadrilateral(
        Point topLeft,
        Point topRight,
        Point bottomRight,
        Point bottomLeft)
    {
        var dx1 = topRight.X - bottomRight.X;
        var dx2 = bottomLeft.X - bottomRight.X;
        var dx3 = topLeft.X - topRight.X + bottomRight.X - bottomLeft.X;
        var dy1 = topRight.Y - bottomRight.Y;
        var dy2 = bottomLeft.Y - bottomRight.Y;
        var dy3 = topLeft.Y - topRight.Y + bottomRight.Y - bottomLeft.Y;
        var denominator = (dx1 * dy2) - (dx2 * dy1);

        if (Math.Abs(denominator) < Epsilon)
        {
            throw new ArgumentException("四边形无法构成有效的单应变换。");
        }

        var g = ((dx3 * dy2) - (dx2 * dy3)) / denominator;
        var h = ((dx1 * dy3) - (dx3 * dy1)) / denominator;

        return new[,]
        {
            { topRight.X - topLeft.X + (g * topRight.X), bottomLeft.X - topLeft.X + (h * bottomLeft.X), topLeft.X },
            { topRight.Y - topLeft.Y + (g * topRight.Y), bottomLeft.Y - topLeft.Y + (h * bottomLeft.Y), topLeft.Y },
            { g, h, 1 }
        };
    }

    private static double[,] Invert(double[,] matrix)
    {
        var a = matrix[0, 0];
        var b = matrix[0, 1];
        var c = matrix[0, 2];
        var d = matrix[1, 0];
        var e = matrix[1, 1];
        var f = matrix[1, 2];
        var g = matrix[2, 0];
        var h = matrix[2, 1];
        var i = matrix[2, 2];
        var determinant = (a * ((e * i) - (f * h)))
                          - (b * ((d * i) - (f * g)))
                          + (c * ((d * h) - (e * g)));

        if (Math.Abs(determinant) < Epsilon)
        {
            throw new InvalidOperationException("单应矩阵不可逆。");
        }

        var scale = 1 / determinant;
        return new[,]
        {
            { ((e * i) - (f * h)) * scale, ((c * h) - (b * i)) * scale, ((b * f) - (c * e)) * scale },
            { ((f * g) - (d * i)) * scale, ((a * i) - (c * g)) * scale, ((c * d) - (a * f)) * scale },
            { ((d * h) - (e * g)) * scale, ((b * g) - (a * h)) * scale, ((a * e) - (b * d)) * scale }
        };
    }
}

internal sealed class PerspectiveEffect : ShaderEffect
{
    private static readonly PixelShader Shader = RuntimePixelShaderCompiler.Compile();

    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty(
        nameof(Input), typeof(PerspectiveEffect), 0);

    public static readonly DependencyProperty MatrixRow0Property = DependencyProperty.Register(
        nameof(MatrixRow0), typeof(Point4D), typeof(PerspectiveEffect),
        new UIPropertyMetadata(new Point4D(1, 0, 0, 0), PixelShaderConstantCallback(0)));

    public static readonly DependencyProperty MatrixRow1Property = DependencyProperty.Register(
        nameof(MatrixRow1), typeof(Point4D), typeof(PerspectiveEffect),
        new UIPropertyMetadata(new Point4D(0, 1, 0, 0), PixelShaderConstantCallback(1)));

    public static readonly DependencyProperty MatrixRow2Property = DependencyProperty.Register(
        nameof(MatrixRow2), typeof(Point4D), typeof(PerspectiveEffect),
        new UIPropertyMetadata(new Point4D(0, 0, 1, 0), PixelShaderConstantCallback(2)));

    public PerspectiveEffect()
    {
        PixelShader = Shader;
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(MatrixRow0Property);
        UpdateShaderValue(MatrixRow1Property);
        UpdateShaderValue(MatrixRow2Property);
    }

    public Brush? Input
    {
        get => (Brush?)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public Point4D MatrixRow0
    {
        get => (Point4D)GetValue(MatrixRow0Property);
        set => SetValue(MatrixRow0Property, value);
    }

    public Point4D MatrixRow1
    {
        get => (Point4D)GetValue(MatrixRow1Property);
        set => SetValue(MatrixRow1Property, value);
    }

    public Point4D MatrixRow2
    {
        get => (Point4D)GetValue(MatrixRow2Property);
        set => SetValue(MatrixRow2Property, value);
    }

    public void SetInverseMatrix(double[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        MatrixRow0 = new Point4D(matrix[0, 0], matrix[0, 1], matrix[0, 2], 0);
        MatrixRow1 = new Point4D(matrix[1, 0], matrix[1, 1], matrix[1, 2], 0);
        MatrixRow2 = new Point4D(matrix[2, 0], matrix[2, 1], matrix[2, 2], 0);
    }
}

internal static class RuntimePixelShaderCompiler
{
    private const string ShaderSource = """
        sampler2D InputTexture : register(s0);
        float4 MatrixRow0 : register(c0);
        float4 MatrixRow1 : register(c1);
        float4 MatrixRow2 : register(c2);

        float4 main(float2 destination : TEXCOORD0) : COLOR0
        {
            float3 homogeneousPosition = float3(destination, 1.0);
            float sourceW = dot(MatrixRow2.xyz, homogeneousPosition);
            float2 source = float2(
                dot(MatrixRow0.xyz, homogeneousPosition),
                dot(MatrixRow1.xyz, homogeneousPosition)) / sourceW;
            float2 lowerBound = step(float2(0.0, 0.0), source);
            float2 upperBound = step(source, float2(1.0, 1.0));
            float visible = lowerBound.x * lowerBound.y * upperBound.x * upperBound.y;
            return tex2D(InputTexture, saturate(source)) * visible;
        }
        """;

    public static PixelShader Compile()
    {
        var source = Encoding.UTF8.GetBytes(ShaderSource);
        var result = D3DCompile(
            source, (nuint)source.Length, "PerspectiveEffect.fx", IntPtr.Zero, IntPtr.Zero,
            "main", "ps_2_0", 0, 0, out var shaderBlob, out var errorBlob);

        try
        {
            if (result < 0 || shaderBlob is null)
            {
                var message = errorBlob is null
                    ? $"D3DCompile 失败，HRESULT: 0x{result:X8}。"
                    : ReadBlobText(errorBlob);
                throw new InvalidOperationException(message, Marshal.GetExceptionForHR(result));
            }

            var byteCode = new byte[checked((int)shaderBlob.GetBufferSize())];
            Marshal.Copy(shaderBlob.GetBufferPointer(), byteCode, 0, byteCode.Length);
            var pixelShader = new PixelShader();
            using var stream = new MemoryStream(byteCode, writable: false);
            pixelShader.SetStreamSource(stream);
            return pixelShader;
        }
        finally
        {
            Release(errorBlob);
            Release(shaderBlob);
        }
    }

    private static string ReadBlobText(ID3DBlob blob)
    {
        var bytes = new byte[checked((int)blob.GetBufferSize())];
        Marshal.Copy(blob.GetBufferPointer(), bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(bytes).TrimEnd('\0', '\r', '\n');
    }

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
    private static extern int D3DCompile(
        byte[] sourceData,
        nuint sourceDataSize,
        string sourceName,
        IntPtr defines,
        IntPtr include,
        string entryPoint,
        string target,
        uint flags1,
        uint flags2,
        [MarshalAs(UnmanagedType.Interface)] out ID3DBlob? code,
        [MarshalAs(UnmanagedType.Interface)] out ID3DBlob? errors);

    [ComImport]
    [Guid("8BA5FB08-5195-40E2-AC58-0D989C3A0102")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ID3DBlob
    {
        [PreserveSig]
        IntPtr GetBufferPointer();

        [PreserveSig]
        nuint GetBufferSize();
    }
}