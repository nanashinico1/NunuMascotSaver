namespace NunuMascotSaver;

internal sealed class ScreenSaverHostContext : ApplicationContext
{
    private readonly List<ScreenSaverForm> _forms = [];
    private readonly bool _previewMode;
    private readonly DesktopSnapshot? _desktopSnapshot;
    private bool _closing;
    private bool _cursorHidden;

    public ScreenSaverHostContext(ScreenSaverSettings settings, IntPtr previewHandle)
    {
        _previewMode = previewHandle != IntPtr.Zero;

        if (!_previewMode)
        {
            Cursor.Hide();
            _cursorHidden = true;
        }

        _desktopSnapshot = (!_previewMode && settings.BackgroundMode == ScreenSaverBackgroundMode.Desktop)
            ? DesktopSnapshot.Capture()
            : null;

        if (_previewMode)
        {
            CreateForm(settings, Rectangle.Empty, previewHandle, null);
        }
        else
        {
            Screen[] screens = Screen.AllScreens;
            Rectangle virtualBounds = ComputeVirtualBounds(screens);
            CreateForm(settings, virtualBounds, IntPtr.Zero, _desktopSnapshot);
        }

        if (_forms.Count == 0)
        {
            _desktopSnapshot?.Dispose();
            RestoreCursor();
            ExitThread();
            return;
        }

        foreach (ScreenSaverForm form in _forms)
        {
            form.Show();
        }
    }

    private void CreateForm(
        ScreenSaverSettings settings,
        Rectangle bounds,
        IntPtr previewHandle,
        DesktopSnapshot? desktopSnapshot)
    {
        ScreenSaverForm form = new(settings.Clone(), bounds, previewHandle, desktopSnapshot);
        form.ExitRequested += OnExitRequested;
        form.FormClosed += OnFormClosed;
        _forms.Add(form);
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        CloseAll();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is not ScreenSaverForm form)
        {
            return;
        }

        form.ExitRequested -= OnExitRequested;
        form.FormClosed -= OnFormClosed;
        _forms.Remove(form);

        if (_forms.Count == 0)
        {
            _desktopSnapshot?.Dispose();
            RestoreCursor();
            ExitThread();
        }
    }

    private void CloseAll()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        foreach (ScreenSaverForm form in _forms.ToArray())
        {
            if (!form.IsDisposed)
            {
                form.Close();
            }
        }
    }

    private void RestoreCursor()
    {
        if (!_cursorHidden)
        {
            return;
        }

        Cursor.Show();
        _cursorHidden = false;
    }

    private static Rectangle ComputeVirtualBounds(Screen[] screens)
    {
        if (screens.Length == 0)
        {
            return Rectangle.Empty;
        }

        int left = screens[0].Bounds.Left;
        int top = screens[0].Bounds.Top;
        int right = screens[0].Bounds.Right;
        int bottom = screens[0].Bounds.Bottom;

        for (int i = 1; i < screens.Length; i += 1)
        {
            Rectangle bounds = screens[i].Bounds;
            if (bounds.Left < left)
            {
                left = bounds.Left;
            }

            if (bounds.Top < top)
            {
                top = bounds.Top;
            }

            if (bounds.Right > right)
            {
                right = bounds.Right;
            }

            if (bounds.Bottom > bottom)
            {
                bottom = bounds.Bottom;
            }
        }

        return Rectangle.FromLTRB(left, top, right, bottom);
    }

}
