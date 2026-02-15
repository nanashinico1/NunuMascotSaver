using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NunuMascotSaver;

/// <summary>
/// DPI スケーリングを正しく考慮してデスクトップ全体をキャプチャする。
///
/// PerMonitorV2 プロセスでは Screen.Bounds / GetWindowRect は物理ピクセルを返す。
/// 一方 GetDC(NULL) のデスクトップ DC はプライマリモニタの DPI で仮想化された
/// 論理座標を使うため、座標系が一致しない。
///
/// 解決策:
///   各モニタの DeviceName で CreateDC を呼び、モニタ固有のデバイス DC を取得。
///   デバイス DC は常に (0,0) 起点でモニタの物理解像度と一致するため、
///   DPI の影響を一切受けない。キャプチャした各モニタの画像を
///   Screen.Bounds の物理ピクセル座標に配置することで、
///   Form.Bounds（= Screen.Bounds）と完全に一致する合成画像を作成する。
/// </summary>
internal sealed class DesktopSnapshot : IDisposable
{
    private readonly Bitmap _virtualDesktop;
    private readonly Rectangle _virtualBounds;

    private DesktopSnapshot(Bitmap virtualDesktop, Rectangle virtualBounds)
    {
        _virtualDesktop = virtualDesktop;
        _virtualBounds = virtualBounds;
    }

    public static DesktopSnapshot? Capture()
    {
        try
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0)
            {
                return null;
            }

            // Screen.Bounds は PerMonitorV2 で物理ピクセルを返す
            // これを使って仮想デスクトップのバウンディングボックスを算出
            int left = int.MaxValue, top = int.MaxValue;
            int right = int.MinValue, bottom = int.MinValue;
            foreach (Screen screen in screens)
            {
                Rectangle b = screen.Bounds;
                if (b.Left < left) left = b.Left;
                if (b.Top < top) top = b.Top;
                if (b.Right > right) right = b.Right;
                if (b.Bottom > bottom) bottom = b.Bottom;
            }

            int compositeW = right - left;
            int compositeH = bottom - top;
            Rectangle virtualBounds = new(left, top, compositeW, compositeH);

            if (compositeW <= 0 || compositeH <= 0)
            {
                return null;
            }

            // 合成画像を物理ピクセルサイズで作成
            var compositeBitmap = new Bitmap(compositeW, compositeH, PixelFormat.Format32bppPArgb);
            using (Graphics compositeG = Graphics.FromImage(compositeBitmap))
            {
                compositeG.Clear(Color.Black);

                foreach (Screen screen in screens)
                {
                    Rectangle bounds = screen.Bounds;

                    // CreateDC(deviceName) でモニタ固有の DC を取得
                    // この DC は (0,0) 起点でモニタの物理解像度をそのまま持つ
                    IntPtr monitorDC = CreateDC(screen.DeviceName, null!, IntPtr.Zero, IntPtr.Zero);
                    if (monitorDC == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        int dcHorzRes = GetDeviceCaps(monitorDC, HORZRES);
                        int dcVertRes = GetDeviceCaps(monitorDC, VERTRES);
                        int dcDesktopHorzRes = GetDeviceCaps(monitorDC, DESKTOPHORZRES);
                        int dcDesktopVertRes = GetDeviceCaps(monitorDC, DESKTOPVERTRES);

                        int monW = dcDesktopHorzRes > 0 ? dcDesktopHorzRes : dcHorzRes;
                        int monH = dcDesktopVertRes > 0 ? dcDesktopVertRes : dcVertRes;

                        using Bitmap? monCapture = CaptureFromDC(monitorDC, monW, monH);
                        if (monCapture is null)
                        {
                            continue;
                        }

                        // Screen.Bounds の位置に配置
                        // DC の解像度と Screen.Bounds のサイズが異なる場合はスケーリング
                        int destX = bounds.Left - virtualBounds.Left;
                        int destY = bounds.Top - virtualBounds.Top;
                        int destW = bounds.Width;
                        int destH = bounds.Height;

                        if (monCapture.Width == destW && monCapture.Height == destH)
                        {
                            compositeG.DrawImageUnscaled(monCapture, destX, destY);
                        }
                        else
                        {
                            compositeG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                            compositeG.DrawImage(monCapture, destX, destY, destW, destH);
                        }
                    }
                    finally
                    {
                        DeleteDC(monitorDC);
                    }
                }
            }
            return new DesktopSnapshot(compositeBitmap, virtualBounds);
        }
        catch
        {
            return null;
        }
    }

    public Bitmap? CreateWindowBackground(IntPtr hwnd, Size clientSize)
    {
        if (clientSize.Width <= 0 || clientSize.Height <= 0)
        {
            return null;
        }

        // PerMonitorV2 コンテキストの GetWindowRect は物理ピクセルを返す
        // Screen.Bounds と同じ座標系なので、キャプチャ画像と一致する
        if (!NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT rect))
        {
            return null;
        }

        int windowX = rect.Left;
        int windowY = rect.Top;
        int windowW = rect.Right - rect.Left;
        int windowH = rect.Bottom - rect.Top;

        // ウィンドウの位置をキャプチャ画像内の座標に変換
        int sourceX = windowX - _virtualBounds.Left;
        int sourceY = windowY - _virtualBounds.Top;

        Rectangle sourceRect = new(sourceX, sourceY, windowW, windowH);
        Rectangle availableRect = new(0, 0, _virtualDesktop.Width, _virtualDesktop.Height);
        Rectangle intersect = Rectangle.Intersect(sourceRect, availableRect);

        if (intersect.Width <= 0 || intersect.Height <= 0)
        {
            return null;
        }

        Bitmap background = new(clientSize.Width, clientSize.Height, PixelFormat.Format32bppPArgb);
        using Graphics g = Graphics.FromImage(background);
        g.Clear(Color.Black);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

        int offsetX = intersect.X - sourceRect.X;
        int offsetY = intersect.Y - sourceRect.Y;

        // GetWindowRect の物理サイズと clientSize は通常一致する（FormBorderStyle.None）
        // 一致しない場合もスケーリングで対応
        float scaleX = (float)clientSize.Width / windowW;
        float scaleY = (float)clientSize.Height / windowH;

        RectangleF destRect = new(
            offsetX * scaleX,
            offsetY * scaleY,
            intersect.Width * scaleX,
            intersect.Height * scaleY);

        g.DrawImage(_virtualDesktop, destRect, intersect, GraphicsUnit.Pixel);

        return background;
    }

    public void Dispose()
    {
        _virtualDesktop.Dispose();
    }

    /// <summary>
    /// DC から指定サイズの領域を (0,0) 起点で BitBlt キャプチャする。
    /// </summary>
    private static Bitmap? CaptureFromDC(IntPtr sourceDC, int width, int height)
    {
        IntPtr memDC = NativeMethods.CreateCompatibleDC(sourceDC);
        if (memDC == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(sourceDC, width, height);
            if (hBitmap == IntPtr.Zero)
            {
                return null;
            }

            IntPtr oldBitmap = NativeMethods.SelectObject(memDC, hBitmap);
            try
            {
                NativeMethods.BitBlt(memDC, 0, 0, width, height, sourceDC, 0, 0, NativeMethods.SRCCOPY);
                NativeMethods.SelectObject(memDC, oldBitmap);

                Bitmap result = System.Drawing.Image.FromHbitmap(hBitmap);
                NativeMethods.DeleteObject(hBitmap);

                // Format32bppPArgb に変換（描画パフォーマンス向上）
                if (result.PixelFormat != PixelFormat.Format32bppPArgb)
                {
                    var converted = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppPArgb);
                    using (Graphics g = Graphics.FromImage(converted))
                    {
                        g.DrawImageUnscaled(result, 0, 0);
                    }
                    result.Dispose();
                    return converted;
                }

                return result;
            }
            catch
            {
                NativeMethods.SelectObject(memDC, oldBitmap);
                NativeMethods.DeleteObject(hBitmap);
                return null;
            }
        }
        finally
        {
            NativeMethods.DeleteDC(memDC);
        }
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateDC(string lpszDriver, string? lpszDevice, IntPtr lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private const int HORZRES = 8;
    private const int VERTRES = 10;
    private const int DESKTOPHORZRES = 118;
    private const int DESKTOPVERTRES = 117;
}
