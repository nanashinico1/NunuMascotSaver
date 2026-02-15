using System.ComponentModel;
using System.Diagnostics;

namespace NunuMascotSaver;

[DesignerCategory("Form")]
public sealed partial class ScreenSaverSettingsForm : Form
{
    private readonly ScreenSaverSettings _settings;

    public ScreenSaverSettingsForm()
        : this(new ScreenSaverSettings())
    {
    }

    internal ScreenSaverSettingsForm(ScreenSaverSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            InitializeDesignValues();
            return;
        }

        InitializeRuntimeValues();
    }

    private void InitializeDesignValues()
    {
        PopulateBackgroundModeItems();
        if (_backgroundModeInput.Items.Count > 0)
        {
            _backgroundModeInput.SelectedIndex = 0;
        }

        UpdateTextOptionInputState();
        UpdateGiantMascotOptionInputState();
    }

    private void InitializeRuntimeValues()
    {
        PopulateBackgroundModeItems();

        _countInput.Value = ClampDecimal(_settings.MascotCount, _countInput.Minimum, _countInput.Maximum);
        _sizeInput.Value = ClampDecimal(_settings.MascotSize, _sizeInput.Minimum, _sizeInput.Maximum);
        _squishInput.Checked = _settings.EnableSquish;
        _backgroundModeInput.SelectedIndex = _settings.BackgroundMode == ScreenSaverBackgroundMode.Desktop ? 1 : 0;

        _textFormationEnabledInput.Checked = _settings.EnableTextFormation;
        _overlayTextInput.Text = _settings.OverlayText ?? string.Empty;
        _textIntervalInput.Value = ClampDecimal((decimal)_settings.TextSwapIntervalSeconds, _textIntervalInput.Minimum, _textIntervalInput.Maximum);
        _textDurationInput.Value = ClampDecimal((decimal)_settings.TextVisibleDurationSeconds, _textDurationInput.Minimum, _textDurationInput.Maximum);
        _textRandomInput.Value = ClampDecimal(_settings.TextRandomPercent, _textRandomInput.Minimum, _textRandomInput.Maximum);

        _giantMascotEnabledInput.Checked = _settings.EnableGiantMascotPopUp;
        _giantMascotIntervalInput.Value = ClampDecimal((decimal)_settings.GiantMascotIntervalSeconds, _giantMascotIntervalInput.Minimum, _giantMascotIntervalInput.Maximum);
        _giantMascotRandomIntervalInput.Value = ClampDecimal((decimal)_settings.GiantMascotRandomIntervalSeconds, _giantMascotRandomIntervalInput.Minimum, _giantMascotRandomIntervalInput.Maximum);
        _giantMascotSizeInput.Value = ClampDecimal(_settings.GiantMascotSizePercent, _giantMascotSizeInput.Minimum, _giantMascotSizeInput.Maximum);

        UpdateTextOptionInputState();
        UpdateGiantMascotOptionInputState();
    }

    private void PopulateBackgroundModeItems()
    {
        _backgroundModeInput.Items.Clear();
        _backgroundModeInput.Items.Add(new BackgroundModeItem(ScreenSaverBackgroundMode.Black, "黒背景"));
        _backgroundModeInput.Items.Add(new BackgroundModeItem(ScreenSaverBackgroundMode.Desktop, "デスクトップ背景"));
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        SaveAndClose();
    }

    private void TextFormationEnabledInput_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateTextOptionInputState();
    }

    private void GiantMascotEnabledInput_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateGiantMascotOptionInputState();
    }

    private void CreditLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenUrl(_creditLink.Text);
    }

    private void SaveAndClose()
    {
        _settings.MascotCount = (int)_countInput.Value;
        _settings.MascotSize = (int)_sizeInput.Value;
        _settings.EnableSquish = _squishInput.Checked;
        _settings.EnableTextFormation = _textFormationEnabledInput.Checked;
        _settings.OverlayText = _overlayTextInput.Text;
        _settings.TextSwapIntervalSeconds = (double)_textIntervalInput.Value;
        _settings.TextVisibleDurationSeconds = (double)_textDurationInput.Value;
        _settings.TextRandomPercent = (int)_textRandomInput.Value;
        _settings.EnableGiantMascotPopUp = _giantMascotEnabledInput.Checked;
        _settings.GiantMascotIntervalSeconds = (double)_giantMascotIntervalInput.Value;
        _settings.GiantMascotRandomIntervalSeconds = (double)_giantMascotRandomIntervalInput.Value;
        _settings.GiantMascotSizePercent = (int)_giantMascotSizeInput.Value;

        if (_backgroundModeInput.SelectedItem is BackgroundModeItem mode)
        {
            _settings.BackgroundMode = mode.Mode;
        }

        try
        {
            _settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"設定保存に失敗しました。\n{ex.Message}",
                "Nunu Mascot Saver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UpdateTextOptionInputState()
    {
        bool enabled = _textFormationEnabledInput.Checked;
        _overlayTextInput.Enabled = enabled;
        _textIntervalInput.Enabled = enabled;
        _textDurationInput.Enabled = enabled;
        _textRandomInput.Enabled = enabled;
    }

    private void UpdateGiantMascotOptionInputState()
    {
        bool enabled = _giantMascotEnabledInput.Checked;
        _giantMascotIntervalInput.Enabled = enabled;
        _giantMascotRandomIntervalInput.Enabled = enabled;
        _giantMascotSizeInput.Enabled = enabled;
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        return Math.Min(max, Math.Max(min, value));
    }

    private static decimal ClampDecimal(int value, decimal min, decimal max)
    {
        return ClampDecimal((decimal)value, min, max);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore launch failure.
        }
    }

    private sealed class BackgroundModeItem
    {
        private readonly string _label;

        public BackgroundModeItem(ScreenSaverBackgroundMode mode, string label)
        {
            Mode = mode;
            _label = label;
        }

        public ScreenSaverBackgroundMode Mode { get; }

        public override string ToString()
        {
            return _label;
        }
    }

}
