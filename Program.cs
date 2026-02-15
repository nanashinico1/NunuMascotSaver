namespace NunuMascotSaver;

internal enum ScreenSaverMode
{
    Configure,
    Run,
    Preview
}

internal readonly record struct ScreenSaverLaunch(ScreenSaverMode Mode, IntPtr PreviewHandle)
{
    public static ScreenSaverLaunch Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new(ScreenSaverMode.Configure, IntPtr.Zero);
        }

        string token = args[0].Trim().ToLowerInvariant();
        if (token.StartsWith("/s") || token.StartsWith("-s"))
        {
            return new(ScreenSaverMode.Run, IntPtr.Zero);
        }

        if (token.StartsWith("/c") || token.StartsWith("-c"))
        {
            return new(ScreenSaverMode.Configure, IntPtr.Zero);
        }

        if (token.StartsWith("/p") || token.StartsWith("-p"))
        {
            string handleText = ExtractPreviewHandle(token, args);
            if (long.TryParse(handleText, out long handleValue) && handleValue != 0)
            {
                return new(ScreenSaverMode.Preview, new IntPtr(handleValue));
            }

            return new(ScreenSaverMode.Configure, IntPtr.Zero);
        }

        return new(ScreenSaverMode.Configure, IntPtr.Zero);
    }

    private static string ExtractPreviewHandle(string token, string[] args)
    {
        int separatorIndex = token.IndexOf(':');
        if (separatorIndex >= 0 && separatorIndex + 1 < token.Length)
        {
            return token[(separatorIndex + 1)..].Trim();
        }

        return args.Length > 1 ? args[1].Trim() : string.Empty;
    }
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        ScreenSaverLaunch launch = ScreenSaverLaunch.Parse(args);
        ScreenSaverSettings settings = ScreenSaverSettings.Load();

        switch (launch.Mode)
        {
            case ScreenSaverMode.Configure:
                ShowSettings(settings);
                break;
            case ScreenSaverMode.Preview:
                Application.Run(new ScreenSaverHostContext(settings, launch.PreviewHandle));
                break;
            case ScreenSaverMode.Run:
                Application.Run(new ScreenSaverHostContext(settings, IntPtr.Zero));
                break;
        }
    }

    private static void ShowSettings(ScreenSaverSettings settings)
    {
        using var form = new ScreenSaverSettingsForm(settings);
        form.ShowDialog();
    }
}
