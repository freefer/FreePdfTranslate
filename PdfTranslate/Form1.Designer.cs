namespace PdfTranslate
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            parrotFormHandle1 = new ReaLTaiizor.Controls.ParrotFormHandle();
            panelTopActions = new ReaLTaiizor.Controls.Panel();
            lblAppTitle = new ReaLTaiizor.Controls.LabelEdit();
            btnSelectPdf = new ReaLTaiizor.Controls.MaterialButton();
            nightControlBox1 = new ReaLTaiizor.Controls.NightControlBox();
            lostSeparator1 = new ReaLTaiizor.Controls.LostSeparator();
            parrotFormEllipse1 = new ReaLTaiizor.Controls.ParrotFormEllipse();
            splitContainerMain = new SplitContainer();
            panelRight = new ReaLTaiizor.Controls.Panel();
            panelTranslatedScroll = new Panel();
            flowLayoutPanelTranslated = new FlowLayoutPanel();
            panelRightHeader = new ReaLTaiizor.Controls.Panel();
            lblTranslatedPageInfo = new ReaLTaiizor.Controls.LabelEdit();
            btnSavePdf = new ReaLTaiizor.Controls.MaterialButton();
            btnTranslate = new ReaLTaiizor.Controls.MaterialButton();
            lblTranslatedTitle = new ReaLTaiizor.Controls.LabelEdit();
            separator2 = new ReaLTaiizor.Controls.Separator();
            panelLeft = new ReaLTaiizor.Controls.Panel();
            panelLeftHeader = new ReaLTaiizor.Controls.Panel();
            btnNext = new ReaLTaiizor.Controls.MaterialButton();
            btnPrevious = new ReaLTaiizor.Controls.MaterialButton();
            lblPageInfo = new ReaLTaiizor.Controls.LabelEdit();
            lblOriginalTitle = new ReaLTaiizor.Controls.LabelEdit();
            separator1 = new ReaLTaiizor.Controls.Separator();
            panelMain = new ReaLTaiizor.Controls.Panel();
            panelBottom = new ReaLTaiizor.Controls.Panel();
            progressBar = new ReaLTaiizor.Controls.MetroProgressBar();
            lblStatus = new ReaLTaiizor.Controls.LabelEdit();
            lostSeparator2 = new ReaLTaiizor.Controls.LostSeparator();
            panelTopActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            panelRight.SuspendLayout();
            panelTranslatedScroll.SuspendLayout();
            panelRightHeader.SuspendLayout();
            panelLeft.SuspendLayout();
            panelLeftHeader.SuspendLayout();
            panelMain.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // parrotFormHandle1
            // 
            parrotFormHandle1.DockAtTop = true;
            parrotFormHandle1.HandleControl = panelTopActions;
            // 
            // panelTopActions
            // 
            panelTopActions.BackColor = Color.White;
            panelTopActions.Controls.Add(lblAppTitle);
            panelTopActions.Controls.Add(btnSelectPdf);
            panelTopActions.Controls.Add(nightControlBox1);
            panelTopActions.Controls.Add(lostSeparator1);
            panelTopActions.Dock = DockStyle.Top;
            panelTopActions.EdgeColor = Color.Transparent;
            panelTopActions.Location = new Point(5, 5);
            panelTopActions.Name = "panelTopActions";
            panelTopActions.Padding = new Padding(1);
            panelTopActions.Size = new Size(1590, 95);
            panelTopActions.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelTopActions.TabIndex = 1;
            panelTopActions.Text = "panel6";
            // 
            // lblAppTitle
            // 
            lblAppTitle.BackColor = Color.Transparent;
            lblAppTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(38, 50, 56);
            lblAppTitle.Location = new Point(16, 16);
            lblAppTitle.Name = "lblAppTitle";
            lblAppTitle.Size = new Size(500, 50);
            lblAppTitle.TabIndex = 2;
            lblAppTitle.Text = "🌐 PDF 智能翻译工具";
            lblAppTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSelectPdf
            // 
            btnSelectPdf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectPdf.AutoSize = false;
            btnSelectPdf.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSelectPdf.CharacterCasing = ReaLTaiizor.Controls.MaterialButton.CharacterCasingEnum.Normal;
            btnSelectPdf.Cursor = Cursors.Hand;
            btnSelectPdf.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnSelectPdf.Depth = 0;
            btnSelectPdf.HighEmphasis = true;
            btnSelectPdf.Icon = null;
            btnSelectPdf.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnSelectPdf.Location = new Point(1280, 27);
            btnSelectPdf.Margin = new Padding(4, 6, 4, 6);
            btnSelectPdf.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnSelectPdf.Name = "btnSelectPdf";
            btnSelectPdf.NoAccentTextColor = Color.Empty;
            btnSelectPdf.Size = new Size(160, 40);
            btnSelectPdf.TabIndex = 0;
            btnSelectPdf.Text = "📁 选择 PDF 文件";
            btnSelectPdf.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnSelectPdf.UseAccentColor = true;
            btnSelectPdf.UseVisualStyleBackColor = true;
            btnSelectPdf.Click += btnSelectPdf_Click;
            // 
            // nightControlBox1
            // 
            nightControlBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            nightControlBox1.BackColor = Color.Transparent;
            nightControlBox1.CloseHoverColor = Color.FromArgb(237, 73, 86);
            nightControlBox1.CloseHoverForeColor = Color.White;
            nightControlBox1.Cursor = Cursors.Hand;
            nightControlBox1.DefaultLocation = false;
            nightControlBox1.DisableMaximizeColor = Color.FromArgb(200, 200, 200);
            nightControlBox1.DisableMinimizeColor = Color.FromArgb(200, 200, 200);
            nightControlBox1.EnableCloseColor = Color.FromArgb(142, 142, 142);
            nightControlBox1.EnableMaximizeButton = true;
            nightControlBox1.EnableMaximizeColor = Color.FromArgb(142, 142, 142);
            nightControlBox1.EnableMinimizeButton = true;
            nightControlBox1.EnableMinimizeColor = Color.FromArgb(142, 142, 142);
            nightControlBox1.Location = new Point(1448, 2);
            nightControlBox1.MaximizeHoverColor = Color.FromArgb(245, 245, 245);
            nightControlBox1.MaximizeHoverForeColor = Color.FromArgb(38, 38, 38);
            nightControlBox1.MinimizeHoverColor = Color.FromArgb(245, 245, 245);
            nightControlBox1.MinimizeHoverForeColor = Color.FromArgb(38, 38, 38);
            nightControlBox1.Name = "nightControlBox1";
            nightControlBox1.Size = new Size(139, 31);
            nightControlBox1.TabIndex = 5;
            // 
            // lostSeparator1
            // 
            lostSeparator1.BackColor = Color.FromArgb(45, 45, 48);
            lostSeparator1.Dock = DockStyle.Bottom;
            lostSeparator1.ForeColor = Color.FromArgb(63, 63, 70);
            lostSeparator1.Horizontal = false;
            lostSeparator1.Location = new Point(1, 90);
            lostSeparator1.Name = "lostSeparator1";
            lostSeparator1.Size = new Size(1588, 4);
            lostSeparator1.TabIndex = 1;
            lostSeparator1.Text = "lostSeparator1";
            // 
            // parrotFormEllipse1
            // 
            parrotFormEllipse1.CornerRadius = 5;
            parrotFormEllipse1.EffectedForm = this;
            // 
            // splitContainerMain
            // 
            splitContainerMain.BackColor = Color.FromArgb(250, 250, 250);
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(5, 100);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(panelLeft);
            splitContainerMain.Panel1.Padding = new Padding(15, 0, 8, 0);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelRight);
            splitContainerMain.Panel2.Padding = new Padding(8, 0, 15, 0);
            splitContainerMain.Size = new Size(1590, 745);
            splitContainerMain.SplitterDistance = 785;
            splitContainerMain.SplitterWidth = 10;
            splitContainerMain.TabIndex = 2;
            // 
            // panelRight
            // 
            panelRight.BackColor = Color.White;
            panelRight.Controls.Add(panelTranslatedScroll);
            panelRight.Controls.Add(panelRightHeader);
            panelRight.Dock = DockStyle.Fill;
            panelRight.EdgeColor = Color.FromArgb(219, 219, 219);
            panelRight.Location = new Point(8, 0);
            panelRight.Name = "panelRight";
            panelRight.Padding = new Padding(1);
            panelRight.Size = new Size(772, 745);
            panelRight.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelRight.TabIndex = 1;
            panelRight.Text = "panel4";
            // 
            // panelTranslatedScroll
            // 
            panelTranslatedScroll.AutoScroll = true;
            panelTranslatedScroll.BackColor = Color.FromArgb(250, 250, 250);
            panelTranslatedScroll.Controls.Add(flowLayoutPanelTranslated);
            panelTranslatedScroll.Dock = DockStyle.Fill;
            panelTranslatedScroll.Location = new Point(1, 91);
            panelTranslatedScroll.Name = "panelTranslatedScroll";
            panelTranslatedScroll.Padding = new Padding(15);
            panelTranslatedScroll.Size = new Size(770, 653);
            panelTranslatedScroll.TabIndex = 1;
            // 
            // flowLayoutPanelTranslated
            // 
            flowLayoutPanelTranslated.AutoSize = true;
            flowLayoutPanelTranslated.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanelTranslated.BackColor = Color.Transparent;
            flowLayoutPanelTranslated.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelTranslated.Location = new Point(0, 0);
            flowLayoutPanelTranslated.Name = "flowLayoutPanelTranslated";
            flowLayoutPanelTranslated.Size = new Size(0, 0);
            flowLayoutPanelTranslated.TabIndex = 0;
            flowLayoutPanelTranslated.WrapContents = false;
            // 
            // panelRightHeader
            // 
            panelRightHeader.BackColor = Color.White;
            panelRightHeader.Controls.Add(lblTranslatedPageInfo);
            panelRightHeader.Controls.Add(btnSavePdf);
            panelRightHeader.Controls.Add(btnTranslate);
            panelRightHeader.Controls.Add(lblTranslatedTitle);
            panelRightHeader.Controls.Add(separator2);
            panelRightHeader.Dock = DockStyle.Top;
            panelRightHeader.EdgeColor = Color.Transparent;
            panelRightHeader.Location = new Point(1, 1);
            panelRightHeader.Name = "panelRightHeader";
            panelRightHeader.Padding = new Padding(20, 15, 20, 10);
            panelRightHeader.Size = new Size(770, 90);
            panelRightHeader.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelRightHeader.TabIndex = 0;
            panelRightHeader.Text = "panel5";
            // 
            // lblTranslatedPageInfo
            // 
            lblTranslatedPageInfo.BackColor = Color.Transparent;
            lblTranslatedPageInfo.Font = new Font("Segoe UI", 9.5F);
            lblTranslatedPageInfo.ForeColor = Color.FromArgb(100, 100, 100);
            lblTranslatedPageInfo.Location = new Point(25, 55);
            lblTranslatedPageInfo.Name = "lblTranslatedPageInfo";
            lblTranslatedPageInfo.Size = new Size(250, 25);
            lblTranslatedPageInfo.TabIndex = 3;
            lblTranslatedPageInfo.Text = "✨ 已翻译: 0 / 0";
            lblTranslatedPageInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSavePdf
            // 
            btnSavePdf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSavePdf.AutoSize = false;
            btnSavePdf.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSavePdf.CharacterCasing = ReaLTaiizor.Controls.MaterialButton.CharacterCasingEnum.Normal;
            btnSavePdf.Cursor = Cursors.Hand;
            btnSavePdf.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnSavePdf.Depth = 0;
            btnSavePdf.Enabled = false;
            btnSavePdf.HighEmphasis = true;
            btnSavePdf.Icon = null;
            btnSavePdf.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnSavePdf.Location = new Point(635, 47);
            btnSavePdf.Margin = new Padding(4, 6, 4, 6);
            btnSavePdf.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnSavePdf.Name = "btnSavePdf";
            btnSavePdf.NoAccentTextColor = Color.Empty;
            btnSavePdf.Size = new Size(115, 32);
            btnSavePdf.TabIndex = 2;
            btnSavePdf.Text = "💾 保存 PDF";
            btnSavePdf.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnSavePdf.UseAccentColor = false;
            btnSavePdf.UseVisualStyleBackColor = true;
            btnSavePdf.Click += btnSavePdf_Click;
            // 
            // btnTranslate
            // 
            btnTranslate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTranslate.AutoSize = false;
            btnTranslate.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTranslate.CharacterCasing = ReaLTaiizor.Controls.MaterialButton.CharacterCasingEnum.Normal;
            btnTranslate.Cursor = Cursors.Hand;
            btnTranslate.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnTranslate.Depth = 0;
            btnTranslate.Enabled = false;
            btnTranslate.HighEmphasis = true;
            btnTranslate.Icon = null;
            btnTranslate.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnTranslate.Location = new Point(510, 47);
            btnTranslate.Margin = new Padding(4, 6, 4, 6);
            btnTranslate.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnTranslate.Name = "btnTranslate";
            btnTranslate.NoAccentTextColor = Color.Empty;
            btnTranslate.Size = new Size(115, 32);
            btnTranslate.TabIndex = 1;
            btnTranslate.Text = "🚀 开始翻译";
            btnTranslate.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnTranslate.UseAccentColor = true;
            btnTranslate.UseVisualStyleBackColor = true;
            btnTranslate.Click += btnTranslate_Click;
            // 
            // lblTranslatedTitle
            // 
            lblTranslatedTitle.BackColor = Color.Transparent;
            lblTranslatedTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTranslatedTitle.ForeColor = Color.FromArgb(38, 50, 56);
            lblTranslatedTitle.Location = new Point(25, 15);
            lblTranslatedTitle.Name = "lblTranslatedTitle";
            lblTranslatedTitle.Size = new Size(300, 35);
            lblTranslatedTitle.TabIndex = 0;
            lblTranslatedTitle.Text = "✨ 翻译结果";
            lblTranslatedTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // separator2
            // 
            separator2.BackColor = Color.White;
            separator2.LineColor = Color.Gray;
            separator2.Location = new Point(20, 78);
            separator2.Name = "separator2";
            separator2.Size = new Size(730, 5);
            separator2.TabIndex = 4;
            separator2.Text = "separator2";
            // 
            // panelLeft
            // 
            panelLeft.AutoScroll = true;
            panelLeft.BackColor = Color.White;
            panelLeft.Controls.Add(panelLeftHeader);
            panelLeft.Dock = DockStyle.Fill;
            panelLeft.EdgeColor = Color.FromArgb(219, 219, 219);
            panelLeft.Location = new Point(15, 0);
            panelLeft.Name = "panelLeft";
            panelLeft.Padding = new Padding(1);
            panelLeft.Size = new Size(762, 745);
            panelLeft.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelLeft.TabIndex = 0;
            panelLeft.Text = "panel2";
            // 
            // panelLeftHeader
            // 
            panelLeftHeader.BackColor = Color.White;
            panelLeftHeader.Controls.Add(btnNext);
            panelLeftHeader.Controls.Add(btnPrevious);
            panelLeftHeader.Controls.Add(lblPageInfo);
            panelLeftHeader.Controls.Add(lblOriginalTitle);
            panelLeftHeader.Controls.Add(separator1);
            panelLeftHeader.Dock = DockStyle.Top;
            panelLeftHeader.EdgeColor = Color.Transparent;
            panelLeftHeader.Location = new Point(1, 1);
            panelLeftHeader.Name = "panelLeftHeader";
            panelLeftHeader.Padding = new Padding(20, 15, 20, 10);
            panelLeftHeader.Size = new Size(760, 90);
            panelLeftHeader.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelLeftHeader.TabIndex = 0;
            panelLeftHeader.Text = "panel3";
            // 
            // btnNext
            // 
            btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNext.AutoSize = false;
            btnNext.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNext.CharacterCasing = ReaLTaiizor.Controls.MaterialButton.CharacterCasingEnum.Normal;
            btnNext.Cursor = Cursors.Hand;
            btnNext.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnNext.Depth = 0;
            btnNext.HighEmphasis = true;
            btnNext.Icon = null;
            btnNext.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnNext.Location = new Point(670, 47);
            btnNext.Margin = new Padding(4, 6, 4, 6);
            btnNext.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnNext.Name = "btnNext";
            btnNext.NoAccentTextColor = Color.Empty;
            btnNext.Size = new Size(75, 32);
            btnNext.TabIndex = 2;
            btnNext.Text = "下一页 ▶";
            btnNext.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnNext.UseAccentColor = true;
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // btnPrevious
            // 
            btnPrevious.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrevious.AutoSize = false;
            btnPrevious.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPrevious.CharacterCasing = ReaLTaiizor.Controls.MaterialButton.CharacterCasingEnum.Normal;
            btnPrevious.Cursor = Cursors.Hand;
            btnPrevious.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnPrevious.Depth = 0;
            btnPrevious.HighEmphasis = true;
            btnPrevious.Icon = null;
            btnPrevious.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnPrevious.Location = new Point(585, 47);
            btnPrevious.Margin = new Padding(4, 6, 4, 6);
            btnPrevious.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnPrevious.Name = "btnPrevious";
            btnPrevious.NoAccentTextColor = Color.Empty;
            btnPrevious.Size = new Size(75, 32);
            btnPrevious.TabIndex = 1;
            btnPrevious.Text = "◀ 上一页";
            btnPrevious.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnPrevious.UseAccentColor = true;
            btnPrevious.UseVisualStyleBackColor = true;
            btnPrevious.Click += btnPrevious_Click;
            // 
            // lblPageInfo
            // 
            lblPageInfo.BackColor = Color.Transparent;
            lblPageInfo.Font = new Font("Segoe UI", 9.5F);
            lblPageInfo.ForeColor = Color.FromArgb(100, 100, 100);
            lblPageInfo.Location = new Point(25, 50);
            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.Size = new Size(250, 25);
            lblPageInfo.TabIndex = 3;
            lblPageInfo.Text = "📄 页面: 0 / 0";
            lblPageInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblOriginalTitle
            // 
            lblOriginalTitle.BackColor = Color.Transparent;
            lblOriginalTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblOriginalTitle.ForeColor = Color.FromArgb(38, 50, 56);
            lblOriginalTitle.Location = new Point(25, 15);
            lblOriginalTitle.Name = "lblOriginalTitle";
            lblOriginalTitle.Size = new Size(300, 35);
            lblOriginalTitle.TabIndex = 0;
            lblOriginalTitle.Text = "📄 原始文档";
            lblOriginalTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // separator1
            // 
            separator1.BackColor = Color.White;
            separator1.LineColor = Color.Gray;
            separator1.Location = new Point(20, 78);
            separator1.Name = "separator1";
            separator1.Size = new Size(720, 5);
            separator1.TabIndex = 4;
            separator1.Text = "separator1";
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.White;
            panelMain.Controls.Add(splitContainerMain);
            panelMain.Controls.Add(panelTopActions);
            panelMain.Controls.Add(panelBottom);
            panelMain.Dock = DockStyle.Fill;
            panelMain.EdgeColor = Color.FromArgb(219, 219, 219);
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(5);
            panelMain.Size = new Size(1600, 940);
            panelMain.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelMain.TabIndex = 0;
            panelMain.Text = "panel1";
            // 
            // panelBottom
            // 
            panelBottom.BackColor = Color.White;
            panelBottom.Controls.Add(progressBar);
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(lostSeparator2);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.EdgeColor = Color.Transparent;
            panelBottom.Location = new Point(5, 845);
            panelBottom.Name = "panelBottom";
            panelBottom.Padding = new Padding(1, 0, 1, 1);
            panelBottom.Size = new Size(1590, 90);
            panelBottom.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            panelBottom.TabIndex = 0;
            panelBottom.Text = "panel7";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            progressBar.BackgroundColor = Color.FromArgb(239, 239, 239);
            progressBar.BorderColor = Color.FromArgb(219, 219, 219);
            progressBar.DisabledBackColor = Color.FromArgb(239, 239, 239);
            progressBar.DisabledBorderColor = Color.FromArgb(219, 219, 219);
            progressBar.DisabledProgressColor = Color.FromArgb(200, 200, 200);
            progressBar.IsDerivedStyle = true;
            progressBar.Location = new Point(1210, 30);
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;
            progressBar.Name = "progressBar";
            progressBar.Orientation = ReaLTaiizor.Enum.Metro.ProgressOrientation.Horizontal;
            progressBar.ProgressColor = Color.FromArgb(0, 149, 246);
            progressBar.Size = new Size(360, 35);
            progressBar.Style = ReaLTaiizor.Enum.Metro.Style.Custom;
            progressBar.StyleManager = null;
            progressBar.TabIndex = 1;
            progressBar.ThemeAuthor = "Taiizor";
            progressBar.ThemeName = "MetroLight";
            progressBar.Value = 0;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 10F);
            lblStatus.ForeColor = Color.FromArgb(70, 70, 70);
            lblStatus.Location = new Point(25, 30);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(800, 35);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "✓ 就绪 - 请选择 PDF 文件";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lostSeparator2
            // 
            lostSeparator2.BackColor = Color.FromArgb(45, 45, 48);
            lostSeparator2.Dock = DockStyle.Top;
            lostSeparator2.ForeColor = Color.FromArgb(63, 63, 70);
            lostSeparator2.Horizontal = false;
            lostSeparator2.Location = new Point(1, 0);
            lostSeparator2.Name = "lostSeparator2";
            lostSeparator2.Size = new Size(1588, 4);
            lostSeparator2.TabIndex = 2;
            lostSeparator2.Text = "lostSeparator2";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(1600, 940);
            ControlBox = false;
            Controls.Add(panelMain);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(1200, 800);
            Name = "Form1";
            ShowIcon = false;
            Text = "PDF 智能翻译工具";
            panelTopActions.ResumeLayout(false);
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            panelRight.ResumeLayout(false);
            panelTranslatedScroll.ResumeLayout(false);
            panelTranslatedScroll.PerformLayout();
            panelRightHeader.ResumeLayout(false);
            panelLeft.ResumeLayout(false);
            panelLeftHeader.ResumeLayout(false);
            panelMain.ResumeLayout(false);
            panelBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private ReaLTaiizor.Controls.ParrotFormHandle parrotFormHandle1;
        private ReaLTaiizor.Controls.ParrotFormEllipse parrotFormEllipse1;
        private ReaLTaiizor.Controls.Panel panelTopActions;
        private ReaLTaiizor.Controls.LabelEdit lblAppTitle;
        private ReaLTaiizor.Controls.MaterialButton btnSelectPdf;
        private ReaLTaiizor.Controls.NightControlBox nightControlBox1;
        private ReaLTaiizor.Controls.LostSeparator lostSeparator1;
        private ReaLTaiizor.Controls.Panel panelMain;
        private SplitContainer splitContainerMain;
        private ReaLTaiizor.Controls.Panel panelLeft;
        private ReaLTaiizor.Controls.Panel panelLeftHeader;
        private ReaLTaiizor.Controls.MaterialButton btnNext;
        private ReaLTaiizor.Controls.MaterialButton btnPrevious;
        private ReaLTaiizor.Controls.LabelEdit lblPageInfo;
        private ReaLTaiizor.Controls.LabelEdit lblOriginalTitle;
        private ReaLTaiizor.Controls.Separator separator1;
        private ReaLTaiizor.Controls.Panel panelRight;
        private System.Windows.Forms.Panel panelTranslatedScroll;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelTranslated;
        private ReaLTaiizor.Controls.Panel panelRightHeader;
        private ReaLTaiizor.Controls.LabelEdit lblTranslatedPageInfo;
        private ReaLTaiizor.Controls.MaterialButton btnSavePdf;
        private ReaLTaiizor.Controls.MaterialButton btnTranslate;
        private ReaLTaiizor.Controls.LabelEdit lblTranslatedTitle;
        private ReaLTaiizor.Controls.Separator separator2;
        private ReaLTaiizor.Controls.Panel panelBottom;
        private ReaLTaiizor.Controls.MetroProgressBar progressBar;
        private ReaLTaiizor.Controls.LabelEdit lblStatus;
        private ReaLTaiizor.Controls.LostSeparator lostSeparator2;
    }
}
