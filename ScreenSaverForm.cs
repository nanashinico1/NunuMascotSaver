using System.Diagnostics;

namespace NunuMascotSaver;

internal sealed class ScreenSaverForm : Form
{
    private readonly ScreenSaverSettings _settings;
    private readonly Rectangle _screenBounds;
    private readonly IntPtr _previewHandle;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly double _targetIntervalSeconds;
    private readonly DesktopSnapshot? _desktopSnapshot;

    private MascotSimulation? _simulation;
    private double _lastTickSeconds;
    private Point _initialMousePoint;
    private bool _mouseInitialized;
    private DateTime _mouseGuardUntil;

    private Thread? _timerThread;
    private volatile bool _running;
    private int _animationTickPending;
    private Bitmap? _desktopBackground;
    private Direct2DRenderer? _direct2DRenderer;
    private readonly Random _overlayRandom = new();
    private bool _overlayEnabled;
    private bool _overlayVisible;
    private double _overlayNextShowAtSeconds;
    private double _overlayHideAtSeconds;

    internal event EventHandler? ExitRequested;

    public ScreenSaverForm(
        ScreenSaverSettings settings,
        Rectangle screenBounds,
        IntPtr previewHandle,
        DesktopSnapshot? desktopSnapshot)
    {
        _settings = settings;
        _screenBounds = screenBounds;
        _previewHandle = previewHandle;
        _targetIntervalSeconds = ResolveTargetIntervalSeconds(settings.MascotCount);
        _desktopSnapshot = desktopSnapshot;

        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        KeyPreview = true;

        if (_previewHandle == IntPtr.Zero)
        {
            TopMost = true;
            Bounds = _screenBounds;
        }
        else
        {
            TopLevel = false;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (_previewHandle != IntPtr.Zero)
        {
            AttachToPreviewWindow();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _simulation = new MascotSimulation(_settings, ClientSize);
        _simulation.SetTextPlacementAreas(BuildTextPlacementAreas(ClientSize), ClientSize);
        _lastTickSeconds = _clock.Elapsed.TotalSeconds;
        if (_desktopSnapshot is not null)
        {
            _desktopBackground = _desktopSnapshot.CreateWindowBackground(Handle, ClientSize);
        }

        if (Direct2DRenderer.TryCreate(Handle, ClientSize, out Direct2DRenderer? renderer))
        {
            _direct2DRenderer = renderer;
        }

        InitializeOverlayTextState();

        _mouseInitialized = false;
        _mouseGuardUntil = DateTime.UtcNow.AddMilliseconds(350);

        _running = true;
        _timerThread = new Thread(TimerLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };
        _timerThread.Start();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _simulation?.ClampToBounds(ClientSize);
        _simulation?.SetTextPlacementAreas(BuildTextPlacementAreas(ClientSize), ClientSize);
        _direct2DRenderer?.Resize(ClientSize);
        if (_simulation is not null && _overlayEnabled)
        {
            _simulation.SetTextFormation(_settings.OverlayText, _overlayVisible, ClientSize);
        }
        if (_desktopSnapshot is not null && IsHandleCreated && !IsDisposed)
        {
            Bitmap? latest = _desktopSnapshot.CreateWindowBackground(Handle, ClientSize);
            if (latest is not null)
            {
                _desktopBackground?.Dispose();
                _desktopBackground = latest;
            }
        }

        if (_overlayEnabled)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_direct2DRenderer is not null)
        {
            return;
        }

        base.OnPaint(e);
        if (_desktopBackground is not null)
        {
            Rectangle clip = e.ClipRectangle;
            if (clip.IsEmpty)
            {
                e.Graphics.DrawImageUnscaled(_desktopBackground, Point.Empty);
            }
            else
            {
                e.Graphics.DrawImage(_desktopBackground, clip, clip, GraphicsUnit.Pixel);
            }
        }
        else
        {
            e.Graphics.Clear(Color.Black);
        }
        _simulation?.Draw(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (_direct2DRenderer is not null)
        {
            return;
        }

        base.OnPaintBackground(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _running = false;
        _timerThread?.Join(200);

        _simulation?.Dispose();
        _simulation = null;
        _desktopBackground?.Dispose();
        _desktopBackground = null;
        _direct2DRenderer?.Dispose();
        _direct2DRenderer = null;

        base.OnFormClosed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        RequestExitIfInteractive();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        RequestExitIfInteractive();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        RequestExitIfInteractive();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_previewHandle != IntPtr.Zero)
        {
            return;
        }

        Point current = Cursor.Position;
        if (!_mouseInitialized)
        {
            _initialMousePoint = current;
            _mouseInitialized = true;
            return;
        }

        if (DateTime.UtcNow < _mouseGuardUntil)
        {
            _initialMousePoint = current;
            return;
        }

        if (Math.Abs(current.X - _initialMousePoint.X) > 8 ||
            Math.Abs(current.Y - _initialMousePoint.Y) > 8)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void TimerLoop()
    {
        var sw = Stopwatch.StartNew();
        double nextTick = sw.Elapsed.TotalSeconds;

        while (_running)
        {
            double now = sw.Elapsed.TotalSeconds;
            if (now < nextTick)
            {
                double sleepMs = (nextTick - now) * 1000.0;
                if (sleepMs > 1.0)
                {
                    Thread.Sleep((int)sleepMs);
                }
                else
                {
                    Thread.SpinWait(100);
                }

                continue;
            }

            nextTick += _targetIntervalSeconds;
            if (now - nextTick > _targetIntervalSeconds * 3)
            {
                nextTick = now + _targetIntervalSeconds;
            }

            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    if (Interlocked.CompareExchange(ref _animationTickPending, 1, 0) == 0)
                    {
                        BeginInvoke(OnAnimationTick);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref _animationTickPending, 0);
                break;
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _animationTickPending, 0);
                break;
            }
        }
    }

    private void OnAnimationTick()
    {
        try
        {
            if (_simulation is null || IsDisposed)
            {
                return;
            }

            double nowSeconds = _clock.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(0.04, nowSeconds - _lastTickSeconds);
            _lastTickSeconds = nowSeconds;

            if (dt <= 0f)
            {
                return;
            }

            _simulation.Update(dt, ClientSize);
            bool overlayChanged = UpdateOverlayTextState(nowSeconds);
            if (overlayChanged)
            {
                _simulation.SetTextFormation(_settings.OverlayText, _overlayVisible, ClientSize);
            }

            if (_direct2DRenderer is not null)
            {
                bool rendered = _direct2DRenderer.Render(_simulation, _desktopBackground);
                if (!rendered)
                {
                    _direct2DRenderer.Dispose();
                    _direct2DRenderer = null;
                }
                return;
            }

            if (overlayChanged)
            {
                Invalidate();
                return;
            }

            Rectangle dirty = _simulation.DirtyRect;
            if (!dirty.IsEmpty)
            {
                Invalidate(dirty);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _animationTickPending, 0);
        }
    }

    private void InitializeOverlayTextState()
    {
        _overlayVisible = false;
        _overlayNextShowAtSeconds = double.PositiveInfinity;
        _overlayHideAtSeconds = double.PositiveInfinity;

        string text = (_settings.OverlayText ?? string.Empty).Trim();
        _overlayEnabled = _settings.EnableTextFormation && text.Length > 0;
        _simulation?.SetTextFormation(text, active: false, ClientSize);
        if (!_overlayEnabled)
        {
            return;
        }

        _overlayNextShowAtSeconds = _clock.Elapsed.TotalSeconds + ResolveOverlaySeconds(_settings.TextSwapIntervalSeconds);
    }

    private bool UpdateOverlayTextState(double nowSeconds)
    {
        if (!_overlayEnabled)
        {
            return false;
        }

        if (_overlayVisible)
        {
            if (nowSeconds < _overlayHideAtSeconds)
            {
                return false;
            }

            _overlayVisible = false;
            _overlayNextShowAtSeconds = nowSeconds + ResolveOverlaySeconds(_settings.TextSwapIntervalSeconds);
            return true;
        }

        if (nowSeconds < _overlayNextShowAtSeconds)
        {
            return false;
        }

        _overlayVisible = true;
        _overlayHideAtSeconds = nowSeconds + ResolveOverlaySeconds(_settings.TextVisibleDurationSeconds);
        return true;
    }

    private double ResolveOverlaySeconds(double baseSeconds)
    {
        double safeBase = Math.Max(0.1, baseSeconds);
        int randomPercent = Math.Clamp(_settings.TextRandomPercent, 0, 100);
        if (randomPercent == 0)
        {
            return safeBase;
        }

        double range = randomPercent / 100.0;
        double factor = 1.0 + ((2.0 * _overlayRandom.NextDouble()) - 1.0) * range;
        factor = Math.Max(0.05, factor);
        return safeBase * factor;
    }

    private void RequestExitIfInteractive()
    {
        if (_previewHandle == IntPtr.Zero)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void AttachToPreviewWindow()
    {
        NativeMethods.SetParent(Handle, _previewHandle);

        long style = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_STYLE).ToInt64();
        style |= NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE;
        style &= ~NativeMethods.WS_POPUP;
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_STYLE, new IntPtr(style));

        if (NativeMethods.GetClientRect(_previewHandle, out NativeMethods.RECT rect))
        {
            Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            Location = Point.Empty;
        }
    }

    private Rectangle[] BuildTextPlacementAreas(Size clientSize)
    {
        Rectangle clientRect = new(0, 0, Math.Max(1, clientSize.Width), Math.Max(1, clientSize.Height));
        if (_previewHandle != IntPtr.Zero)
        {
            return [clientRect];
        }

        Screen[] screens = Screen.AllScreens;
        if (screens.Length == 0)
        {
            return [clientRect];
        }

        var areas = new List<Rectangle>(screens.Length);
        for (int i = 0; i < screens.Length; i += 1)
        {
            Rectangle bounds = screens[i].Bounds;
            Rectangle local = new(
                bounds.Left - _screenBounds.Left,
                bounds.Top - _screenBounds.Top,
                bounds.Width,
                bounds.Height);
            Rectangle clipped = Rectangle.Intersect(clientRect, local);
            if (clipped.Width > 0 && clipped.Height > 0)
            {
                areas.Add(clipped);
            }
        }

        if (areas.Count == 0)
        {
            return [clientRect];
        }

        return [.. areas];
    }

    private static double ResolveTargetIntervalSeconds(int mascotCount)
    {
        if (mascotCount >= 1200)
        {
            return 1.0 / 24.0;
        }

        if (mascotCount >= 700)
        {
            return 1.0 / 30.0;
        }

        if (mascotCount >= 350)
        {
            return 1.0 / 45.0;
        }

        return 1.0 / 60.0;
    }
}
