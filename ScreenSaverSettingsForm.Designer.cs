namespace NunuMascotSaver;

public partial class ScreenSaverSettingsForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private NumericUpDown _countInput = null!;
    private NumericUpDown _sizeInput = null!;
    private CheckBox _squishInput = null!;
    private ComboBox _backgroundModeInput = null!;
    private CheckBox _textFormationEnabledInput = null!;
    private TextBox _overlayTextInput = null!;
    private NumericUpDown _textIntervalInput = null!;
    private NumericUpDown _textDurationInput = null!;
    private NumericUpDown _textRandomInput = null!;
    private CheckBox _giantMascotEnabledInput = null!;
    private NumericUpDown _giantMascotIntervalInput = null!;
    private NumericUpDown _giantMascotRandomIntervalInput = null!;
    private NumericUpDown _giantMascotSizeInput = null!;
    private LinkLabel _creditLink = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        countLabel = new Label();
        _countInput = new NumericUpDown();
        sizeLabel = new Label();
        _sizeInput = new NumericUpDown();
        _squishInput = new CheckBox();
        backgroundLabel = new Label();
        _backgroundModeInput = new ComboBox();
        _textFormationEnabledInput = new CheckBox();
        overlayTextLabel = new Label();
        _overlayTextInput = new TextBox();
        intervalLabel = new Label();
        _textIntervalInput = new NumericUpDown();
        durationLabel = new Label();
        _textDurationInput = new NumericUpDown();
        randomLabel = new Label();
        _textRandomInput = new NumericUpDown();
        _giantMascotEnabledInput = new CheckBox();
        giantIntervalLabel = new Label();
        _giantMascotIntervalInput = new NumericUpDown();
        giantRandomIntervalLabel = new Label();
        _giantMascotRandomIntervalInput = new NumericUpDown();
        giantSizeLabel = new Label();
        _giantMascotSizeInput = new NumericUpDown();
        creditLabel = new Label();
        _creditLink = new LinkLabel();
        _saveButton = new Button();
        _cancelButton = new Button();
        ((System.ComponentModel.ISupportInitialize)_countInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_sizeInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_textIntervalInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_textDurationInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_textRandomInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotIntervalInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotRandomIntervalInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotSizeInput).BeginInit();
        SuspendLayout();
        // 
        // countLabel
        // 
        countLabel.AutoSize = true;
        countLabel.Location = new Point(12, 20);
        countLabel.Name = "countLabel";
        countLabel.Size = new Size(89, 32);
        countLabel.TabIndex = 0;
        countLabel.Text = "キャラ数";
        // 
        // _countInput
        // 
        _countInput.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        _countInput.Location = new Point(430, 16);
        _countInput.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
        _countInput.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
        _countInput.Name = "_countInput";
        _countInput.Size = new Size(120, 39);
        _countInput.TabIndex = 0;
        _countInput.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // sizeLabel
        // 
        sizeLabel.AutoSize = true;
        sizeLabel.Location = new Point(12, 60);
        sizeLabel.Name = "sizeLabel";
        sizeLabel.Size = new Size(159, 32);
        sizeLabel.TabIndex = 1;
        sizeLabel.Text = "キャラサイズ(px)";
        // 
        // _sizeInput
        // 
        _sizeInput.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        _sizeInput.Location = new Point(430, 56);
        _sizeInput.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
        _sizeInput.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
        _sizeInput.Name = "_sizeInput";
        _sizeInput.Size = new Size(120, 39);
        _sizeInput.TabIndex = 1;
        _sizeInput.Value = new decimal(new int[] { 20, 0, 0, 0 });
        // 
        // _squishInput
        // 
        _squishInput.AutoSize = true;
        _squishInput.Location = new Point(12, 99);
        _squishInput.Name = "_squishInput";
        _squishInput.Size = new Size(223, 36);
        _squishInput.TabIndex = 2;
        _squishInput.Text = "衝突時に変形する";
        // 
        // backgroundLabel
        // 
        backgroundLabel.AutoSize = true;
        backgroundLabel.Location = new Point(12, 138);
        backgroundLabel.Name = "backgroundLabel";
        backgroundLabel.Size = new Size(62, 32);
        backgroundLabel.TabIndex = 3;
        backgroundLabel.Text = "背景";
        // 
        // _backgroundModeInput
        // 
        _backgroundModeInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _backgroundModeInput.Location = new Point(430, 134);
        _backgroundModeInput.Name = "_backgroundModeInput";
        _backgroundModeInput.Size = new Size(223, 40);
        _backgroundModeInput.TabIndex = 3;
        // 
        // _textFormationEnabledInput
        // 
        _textFormationEnabledInput.AutoSize = true;
        _textFormationEnabledInput.Location = new Point(12, 179);
        _textFormationEnabledInput.Name = "_textFormationEnabledInput";
        _textFormationEnabledInput.Size = new Size(199, 36);
        _textFormationEnabledInput.TabIndex = 4;
        _textFormationEnabledInput.Text = "文字に変形する";
        _textFormationEnabledInput.CheckedChanged += TextFormationEnabledInput_CheckedChanged;
        // 
        // overlayTextLabel
        // 
        overlayTextLabel.AutoSize = true;
        overlayTextLabel.Location = new Point(12, 218);
        overlayTextLabel.Name = "overlayTextLabel";
        overlayTextLabel.Size = new Size(154, 32);
        overlayTextLabel.TabIndex = 5;
        overlayTextLabel.Text = "対象の文字列";
        // 
        // _overlayTextInput
        // 
        _overlayTextInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _overlayTextInput.Location = new Point(430, 214);
        _overlayTextInput.Name = "_overlayTextInput";
        _overlayTextInput.Size = new Size(226, 39);
        _overlayTextInput.TabIndex = 5;
        // 
        // intervalLabel
        // 
        intervalLabel.AutoSize = true;
        intervalLabel.Location = new Point(12, 258);
        intervalLabel.Name = "intervalLabel";
        intervalLabel.Size = new Size(163, 32);
        intervalLabel.TabIndex = 6;
        intervalLabel.Text = "変わる間隔(秒)";
        // 
        // _textIntervalInput
        // 
        _textIntervalInput.DecimalPlaces = 1;
        _textIntervalInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _textIntervalInput.Location = new Point(430, 254);
        _textIntervalInput.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        _textIntervalInput.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        _textIntervalInput.Name = "_textIntervalInput";
        _textIntervalInput.Size = new Size(120, 39);
        _textIntervalInput.TabIndex = 6;
        _textIntervalInput.Value = new decimal(new int[] { 1, 0, 0, 65536 });
        // 
        // durationLabel
        // 
        durationLabel.AutoSize = true;
        durationLabel.Location = new Point(12, 298);
        durationLabel.Name = "durationLabel";
        durationLabel.Size = new Size(215, 32);
        durationLabel.TabIndex = 7;
        durationLabel.Text = "代わっている時間(秒)";
        // 
        // _textDurationInput
        // 
        _textDurationInput.DecimalPlaces = 1;
        _textDurationInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _textDurationInput.Location = new Point(430, 294);
        _textDurationInput.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        _textDurationInput.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
        _textDurationInput.Name = "_textDurationInput";
        _textDurationInput.Size = new Size(120, 39);
        _textDurationInput.TabIndex = 7;
        _textDurationInput.Value = new decimal(new int[] { 1, 0, 0, 65536 });
        // 
        // randomLabel
        // 
        randomLabel.AutoSize = true;
        randomLabel.Location = new Point(12, 338);
        randomLabel.Name = "randomLabel";
        randomLabel.Size = new Size(166, 32);
        randomLabel.TabIndex = 8;
        randomLabel.Text = "ランダム時間(%)";
        // 
        // _textRandomInput
        // 
        _textRandomInput.Increment = new decimal(new int[] { 5, 0, 0, 0 });
        _textRandomInput.Location = new Point(430, 334);
        _textRandomInput.Name = "_textRandomInput";
        _textRandomInput.Size = new Size(120, 39);
        _textRandomInput.TabIndex = 8;
        // 
        // _giantMascotEnabledInput
        // 
        _giantMascotEnabledInput.AutoSize = true;
        _giantMascotEnabledInput.Location = new Point(12, 379);
        _giantMascotEnabledInput.Name = "_giantMascotEnabledInput";
        _giantMascotEnabledInput.Size = new Size(316, 36);
        _giantMascotEnabledInput.TabIndex = 9;
        _giantMascotEnabledInput.Text = "巨大キャラ演出を有効にする";
        _giantMascotEnabledInput.CheckedChanged += GiantMascotEnabledInput_CheckedChanged;
        // 
        // giantIntervalLabel
        // 
        giantIntervalLabel.AutoSize = true;
        giantIntervalLabel.Location = new Point(12, 418);
        giantIntervalLabel.Name = "giantIntervalLabel";
        giantIntervalLabel.Size = new Size(406, 32);
        giantIntervalLabel.TabIndex = 10;
        giantIntervalLabel.Text = "巨大キャラ表示間隔(秒, +0～ランダム秒)";
        // 
        // _giantMascotIntervalInput
        // 
        _giantMascotIntervalInput.DecimalPlaces = 1;
        _giantMascotIntervalInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _giantMascotIntervalInput.Location = new Point(430, 414);
        _giantMascotIntervalInput.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        _giantMascotIntervalInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _giantMascotIntervalInput.Name = "_giantMascotIntervalInput";
        _giantMascotIntervalInput.Size = new Size(140, 39);
        _giantMascotIntervalInput.TabIndex = 10;
        _giantMascotIntervalInput.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // giantRandomIntervalLabel
        // 
        giantRandomIntervalLabel.AutoSize = true;
        giantRandomIntervalLabel.Location = new Point(12, 458);
        giantRandomIntervalLabel.Name = "giantRandomIntervalLabel";
        giantRandomIntervalLabel.Size = new Size(336, 32);
        giantRandomIntervalLabel.TabIndex = 11;
        giantRandomIntervalLabel.Text = "巨大キャラ間隔ランダム(秒, 0～N)";
        // 
        // _giantMascotRandomIntervalInput
        // 
        _giantMascotRandomIntervalInput.DecimalPlaces = 1;
        _giantMascotRandomIntervalInput.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _giantMascotRandomIntervalInput.Location = new Point(430, 454);
        _giantMascotRandomIntervalInput.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        _giantMascotRandomIntervalInput.Name = "_giantMascotRandomIntervalInput";
        _giantMascotRandomIntervalInput.Size = new Size(140, 39);
        _giantMascotRandomIntervalInput.TabIndex = 11;
        // 
        // giantSizeLabel
        // 
        giantSizeLabel.AutoSize = true;
        giantSizeLabel.Location = new Point(12, 498);
        giantSizeLabel.Name = "giantSizeLabel";
        giantSizeLabel.Size = new Size(372, 32);
        giantSizeLabel.TabIndex = 12;
        giantSizeLabel.Text = "巨大キャラサイズ(%, 100=通常サイズ)";
        // 
        // _giantMascotSizeInput
        // 
        _giantMascotSizeInput.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        _giantMascotSizeInput.Location = new Point(430, 494);
        _giantMascotSizeInput.Maximum = new decimal(new int[] { 1200, 0, 0, 0 });
        _giantMascotSizeInput.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
        _giantMascotSizeInput.Name = "_giantMascotSizeInput";
        _giantMascotSizeInput.Size = new Size(140, 39);
        _giantMascotSizeInput.TabIndex = 12;
        _giantMascotSizeInput.Value = new decimal(new int[] { 100, 0, 0, 0 });
        // 
        // creditLabel
        // 
        creditLabel.AutoSize = true;
        creditLabel.Location = new Point(12, 542);
        creditLabel.Name = "creditLabel";
        creditLabel.Size = new Size(439, 32);
        creditLabel.TabIndex = 13;
        creditLabel.Text = "キャラクターは「双葉湊音」を利用しています。 ";
        // 
        // _creditLink
        // 
        _creditLink.AutoSize = true;
        _creditLink.LinkBehavior = LinkBehavior.HoverUnderline;
        _creditLink.Location = new Point(12, 574);
        _creditLink.Name = "_creditLink";
        _creditLink.Size = new Size(349, 32);
        _creditLink.TabIndex = 14;
        _creditLink.TabStop = true;
        _creditLink.Text = "https://www.futabaminato.com/";
        _creditLink.LinkClicked += CreditLink_LinkClicked;
        // 
        // _saveButton
        // 
        _saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _saveButton.Location = new Point(505, 701);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(151, 36);
        _saveButton.TabIndex = 14;
        _saveButton.Text = "保存";
        _saveButton.Click += SaveButton_Click;
        // 
        // _cancelButton
        // 
        _cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(351, 701);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(148, 36);
        _cancelButton.TabIndex = 15;
        _cancelButton.Text = "キャンセル";
        // 
        // ScreenSaverSettingsForm
        // 
        AcceptButton = _saveButton;
        AutoScaleDimensions = new SizeF(192F, 192F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _cancelButton;
        ClientSize = new Size(686, 749);
        Controls.Add(countLabel);
        Controls.Add(_countInput);
        Controls.Add(sizeLabel);
        Controls.Add(_sizeInput);
        Controls.Add(_squishInput);
        Controls.Add(backgroundLabel);
        Controls.Add(_backgroundModeInput);
        Controls.Add(_textFormationEnabledInput);
        Controls.Add(overlayTextLabel);
        Controls.Add(_overlayTextInput);
        Controls.Add(intervalLabel);
        Controls.Add(_textIntervalInput);
        Controls.Add(durationLabel);
        Controls.Add(_textDurationInput);
        Controls.Add(randomLabel);
        Controls.Add(_textRandomInput);
        Controls.Add(_giantMascotEnabledInput);
        Controls.Add(giantIntervalLabel);
        Controls.Add(_giantMascotIntervalInput);
        Controls.Add(giantRandomIntervalLabel);
        Controls.Add(_giantMascotRandomIntervalInput);
        Controls.Add(giantSizeLabel);
        Controls.Add(_giantMascotSizeInput);
        Controls.Add(creditLabel);
        Controls.Add(_creditLink);
        Controls.Add(_cancelButton);
        Controls.Add(_saveButton);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(680, 820);
        Name = "ScreenSaverSettingsForm";
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Nunu Mascot Saver 設定";
        ((System.ComponentModel.ISupportInitialize)_countInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_sizeInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_textIntervalInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_textDurationInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_textRandomInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotIntervalInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotRandomIntervalInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_giantMascotSizeInput).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label countLabel;
    private Label sizeLabel;
    private Label backgroundLabel;
    private Label overlayTextLabel;
    private Label intervalLabel;
    private Label durationLabel;
    private Label randomLabel;
    private Label giantIntervalLabel;
    private Label giantRandomIntervalLabel;
    private Label giantSizeLabel;
    private Label creditLabel;
}
