using System.Drawing.Imaging;
using System.Reflection;
using Vortice;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DXGI;
using static Vortice.Direct2D1.D2D1;

namespace NunuMascotSaver;

internal sealed class Direct2DRenderer : IDisposable
{
    private static readonly string[] PoseFiles =
    [
        "tachi.png",
        "mae.png",
        "migi.png",
        "hidari.png",
        "ushiro.png"
    ];

    private readonly IntPtr _hwnd;
    private readonly ID2D1Factory _factory;
    private ID2D1HwndRenderTarget? _renderTarget;
    private readonly ID2D1Bitmap?[] _poseBitmaps = new ID2D1Bitmap[PoseFiles.Length];
    private ID2D1Bitmap? _backgroundBitmap;
    private Bitmap? _backgroundSource;

    private bool _disposed;

    private Direct2DRenderer(IntPtr hwnd, System.Drawing.Size clientSize)
    {
        _hwnd = hwnd;
        _factory = D2D1CreateFactory<ID2D1Factory>(FactoryType.SingleThreaded);
        CreateRenderTarget(clientSize);
        LoadPoseBitmaps();
    }

    public static bool TryCreate(IntPtr hwnd, System.Drawing.Size clientSize, out Direct2DRenderer? renderer)
    {
        renderer = null;
        try
        {
            renderer = new Direct2DRenderer(hwnd, clientSize);
            return true;
        }
        catch
        {
            renderer?.Dispose();
            renderer = null;
            return false;
        }
    }

    public bool Resize(System.Drawing.Size clientSize)
    {
        if (_disposed || _renderTarget is null)
        {
            return false;
        }

        int width = Math.Max(1, clientSize.Width);
        int height = Math.Max(1, clientSize.Height);

        _renderTarget.Resize(new Vortice.Mathematics.SizeI(width, height));

        if (_backgroundSource is not null)
        {
            _backgroundBitmap?.Dispose();
            _backgroundBitmap = CreateBitmapFromGdiBitmap(_backgroundSource);
        }

        return true;
    }

    public bool Render(MascotSimulation simulation, Bitmap? desktopBackground)
    {
        if (_disposed || _renderTarget is null)
        {
            return false;
        }

        try
        {
            EnsureBackgroundBitmap(desktopBackground);

            _renderTarget.BeginDraw();
            if (_backgroundBitmap is not null)
            {
                _renderTarget.DrawBitmap(_backgroundBitmap);
            }
            else
            {
                _renderTarget.Clear(new Vortice.Mathematics.Color4(0f, 0f, 0f, 1f));
            }

            ReadOnlySpan<MascotSimulation.MascotDrawCommand> commands = simulation.BuildDrawCommands();
            for (int i = 0; i < commands.Length; i += 1)
            {
                ref readonly MascotSimulation.MascotDrawCommand command = ref commands[i];
                ID2D1Bitmap? bitmap = _poseBitmaps[command.PoseIndex];
                if (bitmap is null)
                {
                    continue;
                }

                RawRectF destRect = new(
                    command.X,
                    command.Y,
                    command.X + command.Width,
                    command.Y + command.Height);

                BitmapInterpolationMode interpolation = command.UseLinearSampling
                    ? BitmapInterpolationMode.Linear
                    : BitmapInterpolationMode.NearestNeighbor;

                _renderTarget.DrawBitmap(bitmap, destRect, 1.0f, interpolation, null);
            }

            var drawResult = _renderTarget.EndDraw();
            return drawResult.Success;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _backgroundBitmap?.Dispose();
        _backgroundBitmap = null;

        for (int i = 0; i < _poseBitmaps.Length; i += 1)
        {
            _poseBitmaps[i]?.Dispose();
            _poseBitmaps[i] = null;
        }

        _renderTarget?.Dispose();
        _renderTarget = null;

        _factory.Dispose();
    }

    private void CreateRenderTarget(System.Drawing.Size clientSize)
    {
        int width = Math.Max(1, clientSize.Width);
        int height = Math.Max(1, clientSize.Height);

        // DPI を 96 に固定して物理ピクセルと 1:1 対応させる。
        // 0 を指定するとシステム DPI（例: 192 = 200%）が使われ、
        // DrawBitmap がスケーリングされてデスクトップ背景がズームされる。
        var renderTargetProps = new RenderTargetProperties(
            RenderTargetType.Default,
            new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96.0f,
            96.0f,
            RenderTargetUsage.None,
            FeatureLevel.Default);

        var hwndProps = new HwndRenderTargetProperties
        {
            Hwnd = _hwnd,
            PixelSize = new Vortice.Mathematics.SizeI(width, height),
            PresentOptions = PresentOptions.Immediately
        };

        _renderTarget = _factory.CreateHwndRenderTarget(renderTargetProps, hwndProps);
    }

    private void LoadPoseBitmaps()
    {
        for (int i = 0; i < PoseFiles.Length; i += 1)
        {
            using Bitmap bitmap = LoadEmbeddedBitmap(PoseFiles[i]);
            _poseBitmaps[i] = CreateBitmapFromGdiBitmap(bitmap);
        }
    }

    private void EnsureBackgroundBitmap(Bitmap? desktopBackground)
    {
        if (_backgroundSource == desktopBackground)
        {
            return;
        }

        _backgroundBitmap?.Dispose();
        _backgroundBitmap = null;
        _backgroundSource = desktopBackground;

        if (desktopBackground is not null)
        {
            _backgroundBitmap = CreateBitmapFromGdiBitmap(desktopBackground);
        }
    }

    private ID2D1Bitmap CreateBitmapFromGdiBitmap(Bitmap source)
    {
        if (_renderTarget is null)
        {
            throw new InvalidOperationException("Render target is not initialized.");
        }

        using Bitmap converted = ConvertToPbgra32(source);
        Rectangle rect = new(0, 0, converted.Width, converted.Height);
        BitmapData bitmapData = converted.LockBits(
            rect,
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            return _renderTarget.CreateBitmap(
                new Vortice.Mathematics.SizeI(converted.Width, converted.Height),
                bitmapData.Scan0,
                (uint)bitmapData.Stride,
                new BitmapProperties(new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied)));
        }
        finally
        {
            converted.UnlockBits(bitmapData);
        }
    }

    private static Bitmap ConvertToPbgra32(Bitmap source)
    {
        if (source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
        {
            return (Bitmap)source.Clone();
        }

        Bitmap converted = new(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using Graphics g = Graphics.FromImage(converted);
        g.DrawImage(source, 0, 0, source.Width, source.Height);
        return converted;
    }

    private static Bitmap LoadEmbeddedBitmap(string fileName)
    {
        Assembly assembly = typeof(Direct2DRenderer).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"埋め込み画像が見つかりません: {fileName}");
        }

        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null)
        {
            throw new InvalidOperationException($"埋め込み画像を開けません: {fileName}");
        }

        using Image original = Image.FromStream(resourceStream);
        return new Bitmap(original);
    }
}
