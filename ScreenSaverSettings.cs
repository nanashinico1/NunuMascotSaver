using System.Text.Json;
using System.Text.Json.Serialization;

namespace NunuMascotSaver;

internal enum ScreenSaverBackgroundMode
{
    Black = 0,
    Desktop = 1
}

internal sealed class ScreenSaverSettings
{
    private const int MinCount = 5;
    private const int MinSize = 20;
    private const int MaxSize = 200;
    private const double MinTextIntervalSeconds = 0.1;
    private const double MaxTextIntervalSeconds = 3600.0;
    private const double MinTextDurationSeconds = 0.1;
    private const double MaxTextDurationSeconds = 3600.0;
    private const int MinRandomPercent = 0;
    private const int MaxRandomPercent = 100;
    private const int MaxOverlayTextLength = 200;
    private const double MinGiantIntervalSeconds = 1.0;
    private const double MaxGiantIntervalSeconds = 3600.0;
    private const double MinGiantRandomIntervalSeconds = 0.0;
    private const double MaxGiantRandomIntervalSeconds = 3600.0;
    private const int MinGiantSizePercent = 100;
    private const int MaxGiantSizePercent = 1200;

    public int MascotCount { get; set; } = 80;

    public bool EnableSquish { get; set; } = true;

    public int MascotSize { get; set; } = 80;

    public ScreenSaverBackgroundMode BackgroundMode { get; set; } = ScreenSaverBackgroundMode.Black;

    public bool EnableTextFormation { get; set; }

    public string OverlayText { get; set; } = string.Empty;

    public double TextSwapIntervalSeconds { get; set; } = 20.0;

    public double TextVisibleDurationSeconds { get; set; } = 4.0;

    public int TextRandomPercent { get; set; }

    public bool EnableGiantMascotPopUp { get; set; }

    public double GiantMascotIntervalSeconds { get; set; } = 30.0;

    public double GiantMascotRandomIntervalSeconds { get; set; } = 15.0;

    public int GiantMascotSizePercent { get; set; } = 320;

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NunuMascotSaver",
        "settings.json");

    public static ScreenSaverSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new ScreenSaverSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            ScreenSaverSettings? loaded = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.ScreenSaverSettings);
            if (loaded is null)
            {
                return new ScreenSaverSettings();
            }

            loaded.Normalize();
            return loaded;
        }
        catch
        {
            return new ScreenSaverSettings();
        }
    }

    public ScreenSaverSettings Clone()
    {
        return new ScreenSaverSettings
        {
            MascotCount = MascotCount,
            EnableSquish = EnableSquish,
            MascotSize = MascotSize,
            BackgroundMode = BackgroundMode,
            EnableTextFormation = EnableTextFormation,
            OverlayText = OverlayText,
            TextSwapIntervalSeconds = TextSwapIntervalSeconds,
            TextVisibleDurationSeconds = TextVisibleDurationSeconds,
            TextRandomPercent = TextRandomPercent,
            EnableGiantMascotPopUp = EnableGiantMascotPopUp,
            GiantMascotIntervalSeconds = GiantMascotIntervalSeconds,
            GiantMascotRandomIntervalSeconds = GiantMascotRandomIntervalSeconds,
            GiantMascotSizePercent = GiantMascotSizePercent
        };
    }

    public void Save()
    {
        Normalize();

        string directory = Path.GetDirectoryName(SettingsPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(this, SettingsJsonContext.Default.ScreenSaverSettings);
        File.WriteAllText(SettingsPath, json);
    }

    private void Normalize()
    {
        MascotCount = Math.Max(MinCount, MascotCount);
        MascotSize = Math.Clamp(MascotSize, MinSize, MaxSize);
        OverlayText = (OverlayText ?? string.Empty).Trim();
        if (OverlayText.Length > MaxOverlayTextLength)
        {
            OverlayText = OverlayText[..MaxOverlayTextLength];
        }
        TextSwapIntervalSeconds = Math.Clamp(TextSwapIntervalSeconds, MinTextIntervalSeconds, MaxTextIntervalSeconds);
        TextVisibleDurationSeconds = Math.Clamp(TextVisibleDurationSeconds, MinTextDurationSeconds, MaxTextDurationSeconds);
        TextRandomPercent = Math.Clamp(TextRandomPercent, MinRandomPercent, MaxRandomPercent);
        GiantMascotIntervalSeconds = Math.Clamp(GiantMascotIntervalSeconds, MinGiantIntervalSeconds, MaxGiantIntervalSeconds);
        GiantMascotRandomIntervalSeconds = Math.Clamp(GiantMascotRandomIntervalSeconds, MinGiantRandomIntervalSeconds, MaxGiantRandomIntervalSeconds);
        GiantMascotSizePercent = Math.Clamp(GiantMascotSizePercent, MinGiantSizePercent, MaxGiantSizePercent);
        if (!Enum.IsDefined(BackgroundMode))
        {
            BackgroundMode = ScreenSaverBackgroundMode.Black;
        }
    }
}

[JsonSerializable(typeof(ScreenSaverSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;
