namespace NunuMascotSaver;

internal sealed class ScreenSaverSettingsForm : Form
{
    private readonly NumericUpDown _countInput;
    private readonly NumericUpDown _sizeInput;
    private readonly CheckBox _squishInput;
    private readonly ComboBox _backgroundModeInput;
    private readonly CheckBox _textFormationEnabledInput;
    private readonly TextBox _overlayTextInput;
    private readonly NumericUpDown _textIntervalInput;
    private readonly NumericUpDown _textDurationInput;
    private readonly NumericUpDown _textRandomInput;
    private readonly CheckBox _giantMascotEnabledInput;
    private readonly NumericUpDown _giantMascotIntervalInput;
    private readonly NumericUpDown _giantMascotRandomIntervalInput;
    private readonly NumericUpDown _giantMascotSizeInput;
    private readonly ScreenSaverSettings _settings;

    public ScreenSaverSettingsForm(ScreenSaverSettings settings)
    {
        _settings = settings;

        Text = "Nunu Mascot Saver 設定";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = true;
        MinimumSize = new Size(560, 620);
        ClientSize = new Size(620, 700);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 15,
            AutoScroll = true,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var countLabel = new Label
        {
            Text = "キャラ数",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(countLabel, 0, 0);

        _countInput = new NumericUpDown
        {
            Minimum = 5,
            Maximum = int.MaxValue,
            Increment = 5,
            Value = Math.Max(5, settings.MascotCount),
            Anchor = AnchorStyles.Left,
            Width = 120,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_countInput, 1, 0);

        var sizeLabel = new Label
        {
            Text = "キャラサイズ(px)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(sizeLabel, 0, 1);

        _sizeInput = new NumericUpDown
        {
            Minimum = 20,
            Maximum = 200,
            Increment = 10,
            Value = Math.Clamp(settings.MascotSize, 20, 200),
            Anchor = AnchorStyles.Left,
            Width = 120,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_sizeInput, 1, 1);

        _squishInput = new CheckBox
        {
            Text = "衝突時に変形する",
            Checked = settings.EnableSquish,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.SetColumnSpan(_squishInput, 2);
        root.Controls.Add(_squishInput, 0, 2);

        var backgroundLabel = new Label
        {
            Text = "背景",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(backgroundLabel, 0, 3);

        _backgroundModeInput = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 0, 4)
        };
        _backgroundModeInput.Items.Add(new BackgroundModeItem(ScreenSaverBackgroundMode.Black, "黒背景"));
        _backgroundModeInput.Items.Add(new BackgroundModeItem(ScreenSaverBackgroundMode.Desktop, "デスクトップ背景"));
        _backgroundModeInput.SelectedIndex = settings.BackgroundMode == ScreenSaverBackgroundMode.Desktop ? 1 : 0;
        root.Controls.Add(_backgroundModeInput, 1, 3);

        _textFormationEnabledInput = new CheckBox
        {
            Text = "文字に変形する",
            Checked = settings.EnableTextFormation,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 6)
        };
        _textFormationEnabledInput.CheckedChanged += (_, _) => UpdateTextOptionInputState();
        root.SetColumnSpan(_textFormationEnabledInput, 2);
        root.Controls.Add(_textFormationEnabledInput, 0, 4);

        var overlayTextLabel = new Label
        {
            Text = "対象の文字列",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(overlayTextLabel, 0, 5);

        _overlayTextInput = new TextBox
        {
            Text = settings.OverlayText ?? string.Empty,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Width = 180,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_overlayTextInput, 1, 5);

        var intervalLabel = new Label
        {
            Text = "変わる間隔(秒)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(intervalLabel, 0, 6);

        _textIntervalInput = new NumericUpDown
        {
            Minimum = 0.1m,
            Maximum = 3600m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = ClampDecimal((decimal)settings.TextSwapIntervalSeconds, 0.1m, 3600m),
            Anchor = AnchorStyles.Left,
            Width = 120,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_textIntervalInput, 1, 6);

        var durationLabel = new Label
        {
            Text = "代わっている時間(秒)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(durationLabel, 0, 7);

        _textDurationInput = new NumericUpDown
        {
            Minimum = 0.1m,
            Maximum = 3600m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = ClampDecimal((decimal)settings.TextVisibleDurationSeconds, 0.1m, 3600m),
            Anchor = AnchorStyles.Left,
            Width = 120,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_textDurationInput, 1, 7);

        var randomLabel = new Label
        {
            Text = "ランダム時間(%)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(randomLabel, 0, 8);

        _textRandomInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 0,
            Increment = 5,
            Value = ClampDecimal(settings.TextRandomPercent, 0, 100),
            Anchor = AnchorStyles.Left,
            Width = 120,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_textRandomInput, 1, 8);

        _giantMascotEnabledInput = new CheckBox
        {
            Text = "巨大キャラ演出を有効にする",
            Checked = settings.EnableGiantMascotPopUp,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 0, 6)
        };
        _giantMascotEnabledInput.CheckedChanged += (_, _) => UpdateGiantMascotOptionInputState();
        root.SetColumnSpan(_giantMascotEnabledInput, 2);
        root.Controls.Add(_giantMascotEnabledInput, 0, 9);

        var giantIntervalLabel = new Label
        {
            Text = "巨大キャラ表示間隔(秒, +0～ランダム秒)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(giantIntervalLabel, 0, 10);

        _giantMascotIntervalInput = new NumericUpDown
        {
            Minimum = 1.0m,
            Maximum = 3600m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = ClampDecimal((decimal)settings.GiantMascotIntervalSeconds, 1.0m, 3600m),
            Anchor = AnchorStyles.Left,
            Width = 140,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_giantMascotIntervalInput, 1, 10);

        var giantRandomIntervalLabel = new Label
        {
            Text = "巨大キャラ間隔ランダム(秒, 0～N)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(giantRandomIntervalLabel, 0, 11);

        _giantMascotRandomIntervalInput = new NumericUpDown
        {
            Minimum = 0.0m,
            Maximum = 3600m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = ClampDecimal((decimal)settings.GiantMascotRandomIntervalSeconds, 0.0m, 3600m),
            Anchor = AnchorStyles.Left,
            Width = 140,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_giantMascotRandomIntervalInput, 1, 11);

        var giantSizeLabel = new Label
        {
            Text = "巨大キャラサイズ(%, 100=通常サイズ)",
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        root.Controls.Add(giantSizeLabel, 0, 12);

        _giantMascotSizeInput = new NumericUpDown
        {
            Minimum = 100,
            Maximum = 1200,
            DecimalPlaces = 0,
            Increment = 10,
            Value = ClampDecimal(settings.GiantMascotSizePercent, 100, 1200),
            Anchor = AnchorStyles.Left,
            Width = 140,
            Margin = new Padding(0, 4, 0, 4)
        };
        root.Controls.Add(_giantMascotSizeInput, 1, 12);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 14, 0, 0)
        };
        root.SetColumnSpan(buttonPanel, 2);
        root.Controls.Add(buttonPanel, 0, 14);

        var saveButton = new Button
        {
            Text = "保存",
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4)
        };
        saveButton.Click += (_, _) => SaveAndClose();
        buttonPanel.Controls.Add(saveButton);

        var cancelButton = new Button
        {
            Text = "キャンセル",
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            DialogResult = DialogResult.Cancel
        };
        buttonPanel.Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        UpdateTextOptionInputState();
        UpdateGiantMascotOptionInputState();
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

    private sealed class BackgroundModeItem(ScreenSaverBackgroundMode mode, string label)
    {
        public ScreenSaverBackgroundMode Mode { get; } = mode;
        public override string ToString() => label;
    }
}
