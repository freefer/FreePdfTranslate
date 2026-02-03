using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using Docnet.Core;
using Docnet.Core.Models;
using UglyToad.PdfPig;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace PdfTranslate
{
 

    public partial class Form1 : ReaLTaiizor.Forms.CrownForm
    {
        private string? currentPdfPath;
        private int currentPageIndex = 0;
        private int totalPages = 0;
        // 原始页面改为仅存储临时文件路径，减少内存占用
        private List<string> originalPages = new List<string>();
        // 翻译结果仅记录临时文件路径，避免占用内存
        private List<string> translatedPages = new List<string>();
        private List<string> pageTexts = new List<string>();
        private List<List<TextBlockInfo>> pageTextBlocks = new List<List<TextBlockInfo>>(); // 存储每页的文本块信息
        private List<PageInfo> pageInfos = new List<PageInfo>(); // 存储每页的尺寸信息
        private readonly HttpClient httpClient = new HttpClient();
        private const string LLAMA_API_URL = "http://127.0.0.1:8033/v1/chat/completions";
        private bool isTranslating = false;
        private string? originalTempDir;    // 存放原始渲染图的临时目录
        private bool originalTempReady = false;
        private string? translationTempDir; // 存放翻译后图片的临时目录
        private bool translationTempReady = false;

        private PictureBox? pictureBoxOriginal;
        private PictureBox? pictureBoxTranslated;

        public Form1()
        {
            InitializeComponent();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            // 启用高质量渲染，消除圆角锯齿
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            // 设置窗体属性以改善渲染质量
            this.Load += Form1_Load;
            this.Resize += Form1_Resize;


            panelTranslatedScroll.Resize += PanelTranslatedScroll_Resize;

            SetHighQualityDisplay();
        }

        private void ClearTempDir(ref string? dir, ref bool readyFlag)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { }
            dir = null;
            readyFlag = false;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // 强制重绘以确保圆角平滑
            this.Refresh();


            flowLayoutPanelTranslated.Width = panelTranslatedScroll.ClientSize.Width - 30;
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // 分页模式下不需要特殊处理
        }


        private void PanelTranslatedScroll_Resize(object? sender, EventArgs e)
        {
            // 如果正在翻译，跳过更新，避免干扰翻译过程
            if (isTranslating)
                return;
                
            // 滚动面板大小改变时，调整 FlowLayoutPanel 宽度
            if (flowLayoutPanelTranslated != null && panelTranslatedScroll.ClientSize.Width > 0)
            {
                int newWidth = panelTranslatedScroll.ClientSize.Width - 30;
                if (newWidth > 0 && Math.Abs(flowLayoutPanelTranslated.Width - newWidth) > 10)
                {
                    flowLayoutPanelTranslated.Width = newWidth;

                    // 如果有翻译内容，也更新显示（延迟执行，避免立即触发）
                    if (translatedPages.Count > 0 && !isTranslating)
                    {
                        this.BeginInvoke(new Action(() => 
                        {
                            if (!isTranslating) // 再次检查，确保翻译没有开始
                            {
                                DisplayTranslatedPages();
                            }
                        }));
                    }
                }
            }
        }

        private void SetHighQualityDisplay()
        {

            panelTranslatedScroll.AutoScroll = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 启用高质量渲染，消除圆角锯齿
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            base.OnPaint(e);
        }

        private async void btnSelectPdf_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF 文件|*.pdf";
                openFileDialog.Title = "选择 PDF 文件";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    btnSelectPdf.Enabled = false;
                    try
                    {
                        await LoadPdfAsync(openFileDialog.FileName);
                    }
                    finally
                    {
                        btnSelectPdf.Enabled = true;
                    }
                }
            }
        }

        private async Task LoadPdfAsync(string pdfPath)
        {
            try
            {
                currentPdfPath = pdfPath;

                // 在UI线程上清理之前的数据
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        ClearTempDir(ref originalTempDir, ref originalTempReady);
                        ClearTempDir(ref translationTempDir, ref translationTempReady);
                        originalPages.Clear();
                        translatedPages.Clear();
                        pageTexts.Clear();
                        pageTextBlocks.Clear();
                        pageInfos.Clear();
                        UpdateStatus("正在加载 PDF 文档...");
                    }));
                }
                else
                {
                    ClearTempDir(ref originalTempDir, ref originalTempReady);
                    ClearTempDir(ref translationTempDir, ref translationTempReady);
                    originalPages.Clear();
                    translatedPages.Clear();
                    pageTexts.Clear();
                    pageTextBlocks.Clear();
                    pageInfos.Clear();
                    UpdateStatus("正在加载 PDF 文档...");
                }

                // 在后台线程执行耗时操作
                await Task.Run(async () =>
                {
                    // 第一步：使用 PdfPig 提取文本和文本块信息（快速准确）
                    try
                    {
                        using (var pigDocument = UglyToad.PdfPig.PdfDocument.Open(pdfPath))
                        {
                            int totalPagesCount = pigDocument.NumberOfPages;
                            
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => UpdateStatus($"正在提取 {totalPagesCount} 页文本和位置信息...")));
                            }
                            else
                            {
                                UpdateStatus($"正在提取 {totalPagesCount} 页文本和位置信息...");
                            }

                            for (int i = 1; i <= totalPagesCount; i++)
                            {
                                try
                                {
                                    var page = pigDocument.GetPage(i);
                                    pageTexts.Add(page.Text);

                                    // 提取文本块信息
                                    List<TextBlockInfo> textBlocks = new List<TextBlockInfo>();
                                    var words = page.GetWords();
                                    int blockId = 0;

                                    foreach (var word in words)
                                    {
                                        var boundingBox = word.BoundingBox;

                                        // 获取字体信息
                                        float fontSize = 12; // 默认字体大小
                                        string fontName = "Arial";

                                        if (word.Letters.Count > 0)
                                        {
                                            var firstLetter = word.Letters[0];
                                            fontSize = (float)firstLetter.FontSize;
                                            fontName = firstLetter.FontName ?? "Arial";
                                        }
                                        else
                                        {
                                            // 如果没有字母信息，根据高度估算
                                            fontSize = (float)(boundingBox.TopRight.Y - boundingBox.BottomLeft.Y) * 0.8f;
                                        }

                                        textBlocks.Add(new TextBlockInfo
                                        {
                                            Id = blockId++,
                                            Text = word.Text,
                                            X = (float)boundingBox.BottomLeft.X,
                                            Y = (float)boundingBox.BottomLeft.Y,
                                            Width = (float)(boundingBox.TopRight.X - boundingBox.BottomLeft.X),
                                            Height = (float)(boundingBox.TopRight.Y - boundingBox.BottomLeft.Y),
                                            FontSize = fontSize,
                                            FontName = fontName
                                        });
                                    }

                                    pageTextBlocks.Add(textBlocks);

                                    // 保存PDF页面尺寸
                                    pageInfos.Add(new PageInfo
                                    {
                                        PdfWidth = (float)page.Width,
                                        PdfHeight = (float)page.Height
                                    });
                                    
                                    // 更新进度
                                    if (i % 10 == 0 || i == totalPagesCount)
                                    {
                                        if (InvokeRequired)
                                        {
                                            BeginInvoke(new Action(() => UpdateStatus($"正在提取文本... {i}/{totalPagesCount} 页")));
                                        }
                                        else
                                        {
                                            UpdateStatus($"正在提取文本... {i}/{totalPagesCount} 页");
                                        }
                                    }
                                }
                                catch
                                {
                                    pageTexts.Add(""); // 提取失败，添加空文本
                                    pageTextBlocks.Add(new List<TextBlockInfo>());
                                    pageInfos.Add(new PageInfo { PdfWidth = 0, PdfHeight = 0 });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() => UpdateStatus($"文本提取警告: {ex.Message}")));
                        }
                        else
                        {
                            UpdateStatus($"文本提取警告: {ex.Message}");
                        }
                    }

                    // 第二步：使用 Docnet.Core 渲染原始 PDF 外观（完美保留格式）
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => UpdateStatus("正在渲染 PDF 预览...")));
                    }
                    else
                    {
                        UpdateStatus("正在渲染 PDF 预览...");
                    }

                    using (var library = DocLib.Instance)
                    using (var docReader = library.GetDocReader(pdfPath, new PageDimensions(3.5))) // 3.5倍缩放，超高清晰度（约 252 DPI）
                    {
                        totalPages = docReader.GetPageCount();

                        // 准备原图临时目录（只提示一次）
                        if (!EnsureTempDir(ref originalTempDir, ref originalTempReady, "original"))
                        {
                            return;
                        }

                        for (int i = 0; i < totalPages; i++)
                        {
                            // 更新进度
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => UpdateStatus($"正在渲染第 {i + 1}/{totalPages} 页...")));
                            }
                            else
                            {
                                UpdateStatus($"正在渲染第 {i + 1}/{totalPages} 页...");
                            }

                            try
                            {
                                using (var pageReader = docReader.GetPageReader(i))
                                {
                                    var width = pageReader.GetPageWidth();
                                    var height = pageReader.GetPageHeight();
                                    var rawBytes = pageReader.GetImage();

                        // 确保原图临时目录（只提示一次）
                        if (!EnsureTempDir(ref originalTempDir, ref originalTempReady, "original"))
                                    {
                                        return;
                                    }

                                    var bitmap = RawBytesToBitmap(rawBytes, width, height);
                                    string origPath = Path.Combine(originalTempDir!, $"orig_{i + 1:D4}.png");
                                    bitmap.Save(origPath, ImageFormat.Png);
                                    bitmap.Dispose();
                                    originalPages.Add(origPath);

                                    // 更新页面信息中的图像尺寸
                                    if (i < pageInfos.Count)
                                    {
                                        pageInfos[i].ImageWidth = width;
                                        pageInfos[i].ImageHeight = height;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (InvokeRequired)
                                {
                                    BeginInvoke(new Action(() => UpdateStatus($"第 {i + 1} 页渲染失败: {ex.Message}")));
                                }
                                else
                                {
                                    UpdateStatus($"第 {i + 1} 页渲染失败: {ex.Message}");
                                }
                                
                                if (!EnsureTempDir(ref originalTempDir, ref originalTempReady, "original"))
                                {
                                    return;
                                }
                                string origPath = Path.Combine(originalTempDir!, $"orig_{i + 1:D4}_failed.png");
                                CreatePlaceholderImage($"第 {i + 1} 页\n渲染失败", origPath);
                                originalPages.Add(origPath);

                                // 即使失败也保存占位图像尺寸（占位固定 800x1000）
                                if (i < pageInfos.Count)
                                {
                                    pageInfos[i].ImageWidth = 800;
                                    pageInfos[i].ImageHeight = 1000;
                                }
                            }
                            
                            // 每处理5页，让UI有机会更新
                            if ((i + 1) % 5 == 0)
                            {
                                await Task.Delay(10).ConfigureAwait(false);
                            }
                        }
                    }
                }).ConfigureAwait(false);

                // 在UI线程上更新最终状态
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        currentPageIndex = 0;
                        UpdateStatus($"✓ 已加载 PDF: {Path.GetFileName(pdfPath)}，共 {totalPages} 页");
                        DisplayCurrentPage();
                        btnTranslate.Enabled = true;
                        btnSavePdf.Enabled = false;
                        lblTranslatedPageInfo.Text = $"✨ 已翻译: 0 / {totalPages}";
                    }));
                }
                else
                {
                    currentPageIndex = 0;
                    UpdateStatus($"✓ 已加载 PDF: {Path.GetFileName(pdfPath)}，共 {totalPages} 页");
                    DisplayCurrentPage();
                    btnTranslate.Enabled = true;
                    btnSavePdf.Enabled = false;
                    lblTranslatedPageInfo.Text = $"✨ 已翻译: 0 / {totalPages}";
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => 
                    {
                        MessageBox.Show($"加载 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                else
                {
                    MessageBox.Show($"加载 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Bitmap RawBytesToBitmap(byte[] rawBytes, int width, int height)
        {
            // Docnet.Core 返回 BGRA 格式的原始字节
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }


        private string CreatePlaceholderImage(string message, string targetPath)
        {
            var bitmap = new Bitmap(800, 1000);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(245, 245, 245));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Font font = new Font("Segoe UI", 16))
                using (Brush brush = new SolidBrush(Color.Gray))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(message, font, brush, new RectangleF(0, 0, 800, 1000), sf);
                }
            }
            bitmap.Save(targetPath, ImageFormat.Png);
            bitmap.Dispose();
            return targetPath;
        }

        /// <summary>
        /// <summary>
        /// <summary>
        /// 确保临时目录存在；已准备则直接返回，不重复弹窗
        /// </summary>
        private bool EnsureTempDir(ref string? targetDir, ref bool readyFlag, string subFolder)
        {
            if (readyFlag && !string.IsNullOrWhiteSpace(targetDir) && Directory.Exists(targetDir))
            {
                return true;
            }

            try
            {
                string baseName = string.IsNullOrWhiteSpace(currentPdfPath)
                    ? $"temp_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : Path.GetFileNameWithoutExtension(currentPdfPath);

                string root = Path.Combine(Path.GetTempPath(), "FreePdfTranslate", subFolder);
                targetDir = Path.Combine(root, baseName);

                if (Directory.Exists(targetDir))
                {
                    var result = MessageBox.Show(
                        $"临时目录已存在：{targetDir}\n是否清空并覆盖？",
                        "提示",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        return false;
                    }

                    Directory.Delete(targetDir, true);
                }

                Directory.CreateDirectory(targetDir);
                readyFlag = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建临时目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void DisplayCurrentPage()
        {
            if (originalPages.Count == 0 || currentPageIndex < 0 || currentPageIndex >= totalPages)
                return;

            try
            {
                // 确保 PictureBox 已创建
                if (pictureBoxOriginal == null)
                {
                    // 清空旧控件（保留 Header）
                    List<Control> toRemove = new List<Control>();
                    foreach (Control ctrl in panelLeft.Controls)
                    {
                        if (ctrl != panelLeftHeader)
                        {
                            toRemove.Add(ctrl);
                        }
                    }
                    foreach (var ctrl in toRemove)
                    {
                        panelLeft.Controls.Remove(ctrl);
                        ctrl.Dispose();
                    }

                    pictureBoxOriginal = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.FromArgb(250, 250, 250),
                        Padding = new Padding(20),
                        Name = "pictureBoxOriginal"
                    };
                    panelLeft.Controls.Add(pictureBoxOriginal);
                    pictureBoxOriginal.BringToFront();
                }

                if (pictureBoxTranslated == null)
                {
                    // 清空旧控件（保留 Header 和 panelTranslatedScroll）
                    List<Control> toRemove = new List<Control>();
                    foreach (Control ctrl in panelRight.Controls)
                    {
                        if (ctrl != panelRightHeader && ctrl.Name != "panelTranslatedScroll")
                        {
                            toRemove.Add(ctrl);
                        }
                    }
                    foreach (var ctrl in toRemove)
                    {
                        panelRight.Controls.Remove(ctrl);
                        ctrl.Dispose();
                    }

                    pictureBoxTranslated = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        BackColor = Color.FromArgb(250, 250, 250),
                        Padding = new Padding(20),
                        Name = "pictureBoxTranslated"
                    };
                    panelRight.Controls.Add(pictureBoxTranslated);
                    pictureBoxTranslated.BringToFront();
                }

                // 显示当前页（从文件加载）
                if (currentPageIndex < originalPages.Count)
                {
                    var path = originalPages[currentPageIndex];
                    if (File.Exists(path))
                    {
                        if (pictureBoxOriginal.Image != null)
                        {
                            var old = pictureBoxOriginal.Image;
                            pictureBoxOriginal.Image = null;
                            old.Dispose();
                        }
                        using (var img = Image.FromFile(path))
                        {
                            pictureBoxOriginal.Image = new Bitmap(img);
                        }
                    }
                    else
                    {
                        pictureBoxOriginal.Image = null;
                    }
                }
                else
                {
                    pictureBoxOriginal.Image = null;
                }
                lblPageInfo.Text = $"📄 页面: {currentPageIndex + 1} / {totalPages}";

                // 显示翻译结果（如果有），从临时文件加载
                if (currentPageIndex < translatedPages.Count)
                {
                    var path = translatedPages[currentPageIndex];
                    if (File.Exists(path))
                    {
                        // 释放旧图，避免文件锁
                        if (pictureBoxTranslated.Image != null)
                        {
                            var old = pictureBoxTranslated.Image;
                            pictureBoxTranslated.Image = null;
                            old.Dispose();
                        }
                        using (var img = Image.FromFile(path))
                        {
                            pictureBoxTranslated.Image = new Bitmap(img);
                        }
                    }
                    else
                    {
                        pictureBoxTranslated.Image = null;
                    }
                }
                else
                {
                    pictureBoxTranslated.Image = null;
                }

                UpdateStatus($"显示第 {currentPageIndex + 1} 页");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示页面失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPrevious_Click(object? sender, EventArgs e)
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                DisplayCurrentPage();
            }
        }

        private void btnNext_Click(object? sender, EventArgs e)
        {
            if (currentPageIndex < totalPages - 1)
            {
                currentPageIndex++;
                DisplayCurrentPage();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 快捷键支持
            switch (keyData)
            {
                case Keys.Left:
                case Keys.PageUp:
                    if (currentPageIndex > 0)
                    {
                        currentPageIndex--;
                        DisplayCurrentPage();
                    }
                    return true;
                case Keys.Right:
                case Keys.PageDown:
                    if (currentPageIndex < totalPages - 1)
                    {
                        currentPageIndex++;
                        DisplayCurrentPage();
                    }
                    return true;
                case Keys.Home:
                    currentPageIndex = 0;
                    DisplayCurrentPage();
                    return true;
                case Keys.End:
                    currentPageIndex = totalPages - 1;
                    DisplayCurrentPage();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void DisplayTranslatedPages()
        {
            try
            {
                flowLayoutPanelTranslated.Controls.Clear();

                // 获取实际可用宽度（考虑滚动条）
                int availableWidth = panelTranslatedScroll.ClientSize.Width - 50;
                if (availableWidth < 200) availableWidth = 700; // 默认宽度

                flowLayoutPanelTranslated.SuspendLayout();

                for (int i = 0; i < translatedPages.Count; i++)
                {
                    if (!File.Exists(translatedPages[i]))
                    {
                        continue;
                    }

                    using var img = Image.FromFile(translatedPages[i]);

                    // 计算图片高度，保持宽高比
                    float aspectRatio = (float)img.Height / img.Width;
                    int imageHeight = (int)(availableWidth * aspectRatio);

                    PictureBox pb = new PictureBox
                    {
                        Image = new Bitmap(img),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Width = availableWidth,
                        Height = imageHeight,
                        Margin = new Padding(10, 10, 10, 5),
                        BackColor = Color.Transparent,  // 无边框
                        Name = $"translatedPageBox_{i}"
                    };

                    // 添加页码标签
                    Label pageLabel = new Label
                    {
                        Text = $"第 {i + 1} 页",
                        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(142, 142, 142),
                        Width = availableWidth,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(10, 0, 10, 15),
                        BackColor = Color.Transparent
                    };

                    flowLayoutPanelTranslated.Controls.Add(pb);
                    flowLayoutPanelTranslated.Controls.Add(pageLabel);
                }

                flowLayoutPanelTranslated.ResumeLayout();
                lblTranslatedPageInfo.Text = $"✨ 已翻译: {translatedPages.Count} / {totalPages}";
            }
            catch (Exception ex)
            {
                UpdateStatus($"显示翻译页面失败: {ex.Message}");
            }
        }

        private async void btnTranslate_Click(object? sender, EventArgs e)
        {
            if (originalPages.Count == 0 || isTranslating)
                return;

            isTranslating = true;
            btnTranslate.Enabled = false;
            btnSelectPdf.Enabled = false;
            btnTranslate.Text = "⏳ 翻译中...";
            progressBar.Maximum = totalPages;
            progressBar.Value = 0;

            try
            {
                // 确保临时目录
                if (!EnsureTempDir(ref translationTempDir, ref translationTempReady, "translated"))
                {
                    // 用户取消
                    isTranslating = false;
                    btnTranslate.Enabled = true;
                    btnSelectPdf.Enabled = true;
                    btnTranslate.Text = "🚀 开始翻译";
                    return;
                }

                translatedPages.Clear();

                for (int i = 0; i < totalPages; i++)
                {
                    // 使用 BeginInvoke 更新UI，避免阻塞，允许窗口调整大小
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => 
                        {
                            UpdateStatus($"⏳ 正在翻译第 {i + 1} / {totalPages} 页...");
                            progressBar.Value = i;
                        }));
                    }
                    else
                    {
                        UpdateStatus($"⏳ 正在翻译第 {i + 1} / {totalPages} 页...");
                        progressBar.Value = i;
                    }

                    var pageText = i < pageTexts.Count ? pageTexts[i] : "";
                    var pageImagePath = originalPages[i];

                    System.Drawing.Image? translatedImage = null;

                    if (i==13)
                    {
                        var c = pageText;
                    }
                    // 智能选择翻译方式
                    if (!string.IsNullOrWhiteSpace(pageText) && pageText.Length > 1)
                    {
                        using (var pageImage = Image.FromFile(pageImagePath))
                        {
                            // 使用文本翻译（更快更准）
                            translatedImage = await TranslatePageWithText(pageImage, pageText, i).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var pageImage = Image.FromFile(pageImagePath))
                        {
                            // 使用视觉翻译（扫描版或纯图像）
                            translatedImage = await TranslatePageWithVision(pageImage, i).ConfigureAwait(false);
                        }
                    }

                    // 保存到临时文件，translatedPages 仅记录路径
                    string pageFileName = $"page_{i + 1:D4}.png";
                    string pageFilePath = Path.Combine(translationTempDir!, pageFileName);

                    try
                    {
                        if (translatedImage != null)
                        {
                            translatedImage.Save(pageFilePath, ImageFormat.Png);
                            translatedImage.Dispose();
                        }
                        else
                        {
                            // 将原始图像文件复制为占位
                            File.Copy(pageImagePath, pageFilePath, true);
                        }
                        translatedPages.Add(pageFilePath);

                        // 如果当前页就是正在翻译的页，立即刷新预览
                        if (i == currentPageIndex)
                        {
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => DisplayCurrentPage()));
                            }
                            else
                            {
                                DisplayCurrentPage();
                            }
                        }
                    }
                    catch (Exception exSave)
                    {
                        // 保存失败则记录错误并继续
                        System.Diagnostics.Debug.WriteLine($"保存翻译页失败: {exSave.Message}");
                    }

                    // 在UI线程上更新界面
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            lblTranslatedPageInfo.Text = $"✨ 已翻译: {translatedPages.Count} / {totalPages}";

                            // 如果是当前页，立即显示
                            if (i == currentPageIndex)
                            {
                                DisplayCurrentPage();
                            }
                        }));
                    }
                    else
                    {
                        lblTranslatedPageInfo.Text = $"✨ 已翻译: {translatedPages.Count} / {totalPages}";
                        if (i == currentPageIndex)
                        {
                            DisplayCurrentPage();
                        }
                    }
                }

                // 在UI线程上更新最终状态
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => 
                    {
                        progressBar.Value = totalPages;
                        UpdateStatus($"✓ 翻译完成！共翻译 {translatedPages.Count} 页");
                        btnSavePdf.Enabled = translatedPages.Count > 0;
                    }));
                }
                else
                {
                    progressBar.Value = totalPages;
                    UpdateStatus($"✓ 翻译完成！共翻译 {translatedPages.Count} 页");
                    btnSavePdf.Enabled = translatedPages.Count > 0;
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => 
                    {
                        MessageBox.Show($"翻译过程出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                else
                {
                    MessageBox.Show($"翻译过程出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => 
                    {
                        isTranslating = false;
                        btnTranslate.Enabled = true;
                        btnTranslate.Text = "🚀 开始翻译";
                        btnSelectPdf.Enabled = true;
                        progressBar.Value = 0;
                    }));
                }
                else
                {
                    isTranslating = false;
                    btnTranslate.Enabled = true;
                    btnTranslate.Text = "🚀 开始翻译";
                    btnSelectPdf.Enabled = true;
                    progressBar.Value = 0;
                }
            }
        }

        private async Task<System.Drawing.Image?> TranslatePageWithText(System.Drawing.Image pageImage, string pageText, int pageNumber)
        {
            try
            {
                // 在UI线程上更新状态
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateStatus($"正在翻译第 {pageNumber + 1} 页文本...")));
                }
                else
                {
                    UpdateStatus($"正在翻译第 {pageNumber + 1} 页文本...");
                }

                // 直接使用 pageText 进行翻译
                if (string.IsNullOrWhiteSpace(pageText))
                {
                    return null;
                }
                // content =
                // "请将以下英文文档翻译成中文，保持原有段落、空行、标题、编号/列表、代码或公式等格式不变。" +
                // "输出要求：1) 仅返回中文正文；2) 保留段落换行与空行，连续空行最多保留一行；" +
                // "3) 保持标题/编号/列表结构；4) 不添加任何额外解释或标记，不包裹 ``` 代码块；" +
                // "5) 行内公式和特殊符号原样保留。" +
                // "\\n\\n原文：\\n\\n" + pageText

                var requestBody = new
                {
                    model = "Qwen3 VL 8B",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = $"请将以下文本翻译成中文，保持段落结构不变，只返回翻译后的中文文本，不要添加任何解释或额外内容：\\n\\n{pageText}"
                        }
                    },
                    stream = false,
                    reasoning_format = "auto",
                    temperature = 0.8,
                    max_tokens = -1,
                    dynatemp_range = 0,
                    dynatemp_exponent = 1,
                    top_k = 40,
                    top_p = 0.95,
                    min_p = 0.05,
                    xtc_probability = 0,
                    xtc_threshold = 0.1,
                    typ_p = 1,
                    repeat_last_n = 64,
                    repeat_penalty = 1,
                    presence_penalty = 0,
                    frequency_penalty = 0,
                    dry_multiplier = 0,
                    dry_base = 1.75,
                    dry_allowed_length = 2,
                    dry_penalty_last_n = -1,
                    samplers = new[] { "penalties", "dry", "top_n_sigma", "top_k", "typ_p", "top_p", "min_p", "xtc", "temperature" },
                    timings_per_token = true
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 使用 ConfigureAwait(false) 避免死锁
                var response = await httpClient.PostAsync(LLAMA_API_URL, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                string translatedText = result?.choices?[0]?.message?.content?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    return null;
                }

                // 清理翻译文本（移除可能的markdown代码块标记）
                translatedText = translatedText.Trim();
                // if (translatedText.StartsWith("```"))
                // {
                //     // 移除markdown代码块标记
                //     int firstNewline = translatedText.IndexOf('\n');
                //     int lastNewline = translatedText.LastIndexOf('\n');
                //     if (firstNewline >= 0 && lastNewline > firstNewline)
                //     {
                //         translatedText = translatedText.Substring(firstNewline + 1, lastNewline - firstNewline - 1);
                //     }
                //     translatedText = translatedText.Trim();
                // }

                // 在后台线程创建图像，避免阻塞UI线程
                var translatedImage = await Task.Run(() => 
                {
                    // 创建图像的副本，避免跨线程访问问题
                    System.Drawing.Image imageCopy;
                    lock (pageImage)
                    {
                        imageCopy = new Bitmap(pageImage);
                    }
                    return CreateTranslatedImageCentered(imageCopy, translatedText, pageNumber);
                }).ConfigureAwait(false);
                
                return translatedImage;
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateStatus($"文本翻译第 {pageNumber + 1} 页时出错: {ex.Message}")));
                }
                else
                {
                    UpdateStatus($"文本翻译第 {pageNumber + 1} 页时出错: {ex.Message}");
                }
                return null;
            }
        }

        private async Task<System.Drawing.Image?> TranslatePageWithVision(System.Drawing.Image pageImage, int pageNumber)
        {
            System.Drawing.Image? resizedImage = null;
            try
            {
                // 在后台线程压缩图像到640*640，并转换为Base64
                string base64Image = await Task.Run(() => 
                {
                    // 压缩图片到640*640（保持宽高比）
                    resizedImage = ResizeImage(pageImage, 640, 640);
                    return ImageToBase64(resizedImage);
                }).ConfigureAwait(false);

                var requestBody = new
                {
                    model = "Qwen3 VL 8B",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:image/png;base64,{base64Image}"
                                    }
                                },
                                new
                                {
                                    type = "text",
                                    text = "请识别这个PDF页面中的所有文本区域，将每个文本块翻译成中文，并返回JSON数组格式。\n\n要求：\n1. 识别每个文本块的边界框位置（bounding box）\n2. 将文本翻译成中文\n3. 返回JSON数组，每个对象包含：\n   - x: 文本块左上角X坐标（像素）\n   - y: 文本块左上角Y坐标（像素）\n   - width: 文本块宽度（像素）\n   - height: 文本块高度（像素）\n   - text: 翻译后的中文文本\n   - fontSize: 字体大小（像素，可选，如果不提供则根据height估算）\n\n坐标系统：左上角为原点(0,0)，X向右为正，Y向下为正。\n\n只返回JSON数组，不要添加任何解释、markdown代码块或其他内容。\n\n示例格式：\n[{\"x\": 100, \"y\": 50, \"width\": 200, \"height\": 30, \"text\": \"翻译后的文本\", \"fontSize\": 12}]"
                                }
                            }
                        }
                    },
                    stream = false,
                    reasoning_format = "auto",
                    temperature = 0.8,
                    max_tokens = -1,
                    dynatemp_range = 0,
                    dynatemp_exponent = 1,
                    top_k = 40,
                    top_p = 0.95,
                    min_p = 0.05,
                    xtc_probability = 0,
                    xtc_threshold = 0.1,
                    typ_p = 1,
                    repeat_last_n = 64,
                    repeat_penalty = 1,
                    presence_penalty = 0,
                    frequency_penalty = 0,
                    dry_multiplier = 0,
                    dry_base = 1.75,
                    dry_allowed_length = 2,
                    dry_penalty_last_n = -1,
                    samplers = new[] { "penalties", "dry", "top_n_sigma", "top_k", "typ_p", "top_p", "min_p", "xtc", "temperature" },
                    timings_per_token = true
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 使用 ConfigureAwait(false) 避免死锁
                var response = await httpClient.PostAsync(LLAMA_API_URL, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<dynamic>(responseBody);

                string translatedJson = result?.choices?[0]?.message?.content?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(translatedJson))
                {
                    return null;
                }

                // 解析翻译后的JSON数组
                List<VisionTextBlock>? visionBlocks = null;
                try
                {
                    // 尝试提取JSON（可能包含markdown代码块）
                    translatedJson = translatedJson.Trim();
                    if (translatedJson.StartsWith("```"))
                    {
                        int startIdx = translatedJson.IndexOf('[');
                        int endIdx = translatedJson.LastIndexOf(']');
                        if (startIdx >= 0 && endIdx > startIdx)
                        {
                            translatedJson = translatedJson.Substring(startIdx, endIdx - startIdx + 1);
                        }
                    }

                    visionBlocks = JsonConvert.DeserializeObject<List<VisionTextBlock>>(translatedJson);
                }
                catch (Exception ex)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => UpdateStatus($"解析视觉翻译JSON失败: {ex.Message}")));
                    }
                    else
                    {
                        UpdateStatus($"解析视觉翻译JSON失败: {ex.Message}");
                    }
                    return null;
                }

                if (visionBlocks == null || visionBlocks.Count == 0)
                {
                    return null;
                }

                // 在后台线程创建图像，根据边界框位置绘制翻译文本
                var translatedImage = await Task.Run(() => 
                {
                    // 创建原始图像的副本，避免跨线程访问问题
                    System.Drawing.Image imageCopy;
                    lock (pageImage)
                    {
                        imageCopy = new Bitmap(pageImage);
                    }
                    return CreateTranslatedImageFromVision(imageCopy, visionBlocks, resizedImage?.Width ?? 640, resizedImage?.Height ?? 640);
                }).ConfigureAwait(false);
                
                return translatedImage;
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateStatus($"视觉翻译第 {pageNumber + 1} 页时出错: {ex.Message}")));
                }
                else
                {
                    UpdateStatus($"视觉翻译第 {pageNumber + 1} 页时出错: {ex.Message}");
                }
                return null;
            }
            finally
            {
                // 释放压缩后的图像
                resizedImage?.Dispose();
            }
        }

        private string ImageToBase64(System.Drawing.Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }
        
        // 压缩图片到指定大小（保持宽高比）
        private System.Drawing.Image ResizeImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            // 计算缩放比例，保持宽高比
            float ratioX = (float)maxWidth / image.Width;
            float ratioY = (float)maxHeight / image.Height;
            float ratio = Math.Min(ratioX, ratioY);
            
            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);
            
            Bitmap resizedImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            
            return resizedImage;
        }

        private System.Drawing.Image CreateTranslatedImage(System.Drawing.Image originalImage, string translatedText)
        {
            // 创建超高分辨率图像（3倍大小，极致清晰）
            int highResWidth = originalImage.Width * 3;
            int highResHeight = originalImage.Height * 3;

            Bitmap translatedBitmap = new Bitmap(highResWidth, highResHeight);
            translatedBitmap.SetResolution(400, 400); // 设置 400 DPI，打印级质量

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置最高质量渲染
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // 白色背景
                g.Clear(Color.White);

                // 计算字体大小（基于超高分辨率）
                float fontSize = highResWidth / 50.0f; // 动态计算，使用更大字体
                fontSize = Math.Max(36, Math.Min(fontSize, 108)); // 限制在 36-108 之间

                // 使用高质量字体
                Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                Brush brush = new SolidBrush(Color.FromArgb(33, 37, 41));

                // 设置边距（比例计算）
                float margin = highResWidth * 0.08f;
                RectangleF textRect = new RectangleF(
                    margin,
                    margin,
                    highResWidth - margin * 2,
                    highResHeight - margin * 2);

                // 文本格式
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word,
                    FormatFlags = StringFormatFlags.LineLimit
                };

                // 绘制翻译文本
                g.DrawString(translatedText, font, brush, textRect, format);

                // 添加水印
                using (Font watermarkFont = new Font("Segoe UI", fontSize * 0.4f, FontStyle.Italic))
                using (Brush watermarkBrush = new SolidBrush(Color.FromArgb(100, 180, 180, 180)))
                {
                    g.DrawString("AI 翻译", watermarkFont, watermarkBrush,
                        new PointF(highResWidth - margin - 100, highResHeight - margin - 30));
                }
            }

            // 缩放回原始尺寸（保持高质量）
            var finalBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            finalBitmap.SetResolution(300, 300);

            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.DrawImage(translatedBitmap, 0, 0, originalImage.Width, originalImage.Height);
            }

            translatedBitmap.Dispose();
            return finalBitmap;
        }

        // 根据JSON文本块信息创建翻译图像，保留原始图片和文本位置
        private System.Drawing.Image CreateTranslatedImageFromJson(System.Drawing.Image originalImage, List<TextBlockInfo> translatedBlocks, int pageIndex)
        {
            // 创建与原始图像相同大小的位图
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置最高质量渲染
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // 首先绘制原始图像（保留所有图片和布局）
                g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                // 获取页面信息
                PageInfo? pageInfo = pageIndex < pageInfos.Count ? pageInfos[pageIndex] : null;
                if (pageInfo == null || pageInfo.PdfWidth == 0 || pageInfo.PdfHeight == 0)
                {
                    return translatedBitmap; // 如果没有页面信息，直接返回原始图像
                }

                // 计算坐标转换比例（PDF点 -> 图像像素）
                float scaleX = pageInfo.ImageWidth / pageInfo.PdfWidth;
                float scaleY = pageInfo.ImageHeight / pageInfo.PdfHeight;

                // 创建ID到翻译文本块的映射
                var translatedDict = translatedBlocks.ToDictionary(tb => tb.Id, tb => tb);

                // 获取原始文本块信息
                List<TextBlockInfo> originalBlocks = pageIndex < pageTextBlocks.Count ? pageTextBlocks[pageIndex] : new List<TextBlockInfo>();

                // 第一步：使用白色矩形覆盖所有原始文本区域
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    foreach (var originalBlock in originalBlocks)
                    {
                        // 将PDF坐标转换为图像坐标
                        // PDF: 原点在左下角，Y向上
                        // 图像: 原点在左上角，Y向下
                        float imageX = originalBlock.X * scaleX;
                        float pdfTopY = pageInfo.PdfHeight - (originalBlock.Y + originalBlock.Height);
                        float imageY = pdfTopY * scaleY;
                        float imageWidth = originalBlock.Width * scaleX;
                        float imageHeight = originalBlock.Height * scaleY;

                        // 稍微扩大覆盖区域，确保完全覆盖原始文本
                        RectangleF coverRect = new RectangleF(
                            Math.Max(0, imageX - 2),
                            Math.Max(0, imageY - 2),
                            Math.Min(originalImage.Width - imageX + 2, imageWidth + 4),
                            Math.Min(originalImage.Height - imageY + 2, imageHeight + 4)
                        );
                        g.FillRectangle(whiteBrush, coverRect);
                    }
                }

                // 第二步：在原始位置绘制翻译文本
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    foreach (var originalBlock in originalBlocks)
                    {
                        // 查找对应的翻译文本块
                        if (!translatedDict.TryGetValue(originalBlock.Id, out var translatedBlock))
                        {
                            continue; // 如果没有翻译，跳过
                        }

                        if (string.IsNullOrWhiteSpace(translatedBlock.Text))
                        {
                            continue; // 如果翻译为空，跳过
                        }

                        // 将PDF坐标转换为图像坐标
                        float imageX = originalBlock.X * scaleX;
                        float pdfTopY = pageInfo.PdfHeight - (originalBlock.Y + originalBlock.Height);
                        float imageY = pdfTopY * scaleY;
                        float imageWidth = originalBlock.Width * scaleX;
                        float imageHeight = originalBlock.Height * scaleY;

                        // 使用原始字体大小（转换为图像像素大小）
                        float fontSize = originalBlock.FontSize * scaleY *1.5f;
                        fontSize = Math.Max(6, Math.Min(fontSize, 72)); // 限制字体大小范围

                        // 使用中文字体
                        Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

                        // 测量文本大小
                        SizeF textSize = g.MeasureString(translatedBlock.Text, font, (int)imageWidth);

                        // 如果文本太长，缩小字体以适应
                        while (textSize.Height > imageHeight * 1.2f && fontSize > 6)
                        {
                            fontSize = fontSize * 0.9f;
                            font.Dispose();
                            font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                            textSize = g.MeasureString(translatedBlock.Text, font, (int)imageWidth);
                        }

                        // 绘制翻译文本
                        RectangleF drawRect = new RectangleF(
                            imageX,
                            imageY,
                            imageWidth,
                            Math.Max(imageHeight, textSize.Height)
                        );

                        StringFormat format = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            Trimming = StringTrimming.Word,
                            FormatFlags = StringFormatFlags.LineLimit
                        };

                        g.DrawString(translatedBlock.Text, font, textBrush, drawRect, format);

                        font.Dispose();
                    }
                }
            }

            return translatedBitmap;
        }

        // 创建居中显示的翻译图像（使用 DrawString 内置换行，简单可靠）
        private System.Drawing.Image CreateTranslatedImageCentered(System.Drawing.Image originalImage, string translatedText, int pageIndex)
        {
            // 创建与原始图像相同大小的位图
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置最高质量渲染
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // 绘制白色背景
                g.Clear(Color.White);

                // 设置边距（15%）
                float marginX = originalImage.Width * 0.15f;
                float marginY = originalImage.Height * 0.15f;
                float maxWidth = originalImage.Width - marginX * 2;
                float maxHeight = originalImage.Height - marginY * 2;

                // 清理文本
                translatedText = translatedText.Trim();
                
                // 使用 DrawString 内置换行功能（最简单可靠）
                // 初始字体大小
                float fontSize = Math.Min(originalImage.Width, originalImage.Height) / 25f;
                fontSize = Math.Max(10, Math.Min(fontSize, 48));
                
                Font? font = null;
                SizeF textSize;
                int iteration = 0;
                const int maxIterations = 15;
                
                // 自适应调整字体大小
                while (iteration < maxIterations)
                {
                    if (font != null)
                        font.Dispose();
                    
                    font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                    
                    // 定义文本绘制区域
                    RectangleF textRect = new RectangleF(marginX, marginY, maxWidth, maxHeight);
                    
                    // 测量文本实际占用的大小
                    textSize = g.MeasureString(translatedText, font, (int)maxWidth);
                    
                    // 如果文本适合，或字体已经很小，退出循环
                    if (textSize.Height <= maxHeight || fontSize <= 10)
                    {
                        break;
                    }
                    
                    // 缩小字体
                    float scale = maxHeight / textSize.Height * 0.9f;
                    fontSize = fontSize * scale;
                    fontSize = Math.Max(10, fontSize);
                    
                    iteration++;
                }
                
                // 绘制文本
                if (font != null)
                {
                    // 重新测量最终文本大小
                    textSize = g.MeasureString(translatedText, font, (int)maxWidth);
                    
                    // 计算垂直居中位置
                    float startY = marginY + (maxHeight - textSize.Height) / 2f;
                    startY = Math.Max(marginY, startY);
                    
                    // 定义文本绘制区域（垂直居中）
                    RectangleF textRect = new RectangleF(marginX, startY, maxWidth, maxHeight);
                    
                    // 定义文本格式（水平居中，自动换行）
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,  // 水平居中
                        LineAlignment = StringAlignment.Near,  // 顶部对齐
                        Trimming = StringTrimming.Word,  // 按单词截断
                        FormatFlags = StringFormatFlags.LineLimit  // 限制行数
                    };
                    
                    // 绘制文本（使用 DrawString 内置换行）
                    using (Brush textBrush = new SolidBrush(Color.Black))
                    {
                        g.DrawString(translatedText, font, textBrush, textRect, format);
                    }
                    
                    font.Dispose();
                }
            }

            return translatedBitmap;
        }
        
        // 根据视觉翻译返回的边界框位置创建翻译图像
        private System.Drawing.Image CreateTranslatedImageFromVision(System.Drawing.Image originalImage, List<VisionTextBlock> visionBlocks, int resizedWidth, int resizedHeight)
        {
            // 创建与原始图像相同大小的位图
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
            
            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置最高质量渲染
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                
                // 首先绘制原始图像（保留所有图片和布局）
                g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
                
                // 计算缩放比例（压缩后的图像 -> 原始图像）
                float scaleX = (float)originalImage.Width / resizedWidth;
                float scaleY = (float)originalImage.Height / resizedHeight;
                
                // 第一步：使用白色矩形覆盖所有文本区域（基于边界框位置）
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    foreach (var visionBlock in visionBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(visionBlock.Text))
                            continue;
                            
                        // 将压缩图像的坐标转换为原始图像坐标
                        float originalX = visionBlock.X * scaleX;
                        float originalY = visionBlock.Y * scaleY;
                        float originalWidth = visionBlock.Width * scaleX;
                        float originalHeight = visionBlock.Height * scaleY;
                        
                        // 稍微扩大覆盖区域，确保完全覆盖原始文本
                        RectangleF coverRect = new RectangleF(
                            Math.Max(0, originalX - 2),
                            Math.Max(0, originalY - 2),
                            Math.Min(originalImage.Width - originalX + 2, originalWidth + 4),
                            Math.Min(originalImage.Height - originalY + 2, originalHeight + 4)
                        );
                        // g.FillRectangle(whiteBrush, coverRect);
                    }
                }
                
                // 第二步：在边界框位置绘制翻译文本
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    foreach (var visionBlock in visionBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(visionBlock.Text))
                            continue;
                            
                        // 将压缩图像的坐标转换为原始图像坐标
                        float originalX = visionBlock.X * scaleX;
                        float originalY = visionBlock.Y * scaleY;
                        float originalWidth = visionBlock.Width * scaleX;
                        float originalHeight = visionBlock.Height * scaleY;
                        
                        // 使用AI返回的字体大小，或根据高度估算
                        float fontSize = visionBlock.FontSize > 0 
                            ? visionBlock.FontSize * scaleY 
                            : originalHeight * 0.8f;
                        fontSize = Math.Max(6, Math.Min(fontSize, 72)); // 限制字体大小范围
                        
                        // 使用中文字体
                        Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                        
                        // 测量文本大小
                        SizeF textSize = g.MeasureString(visionBlock.Text, font, (int)originalWidth);
                        
                        // 如果文本太长，缩小字体以适应
                        while (textSize.Height > originalHeight * 1.2f && fontSize > 6)
                        {
                            fontSize = fontSize * 0.9f;
                            font.Dispose();
                            font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                            textSize = g.MeasureString(visionBlock.Text, font, (int)originalWidth);
                        }
                        
                        // 绘制翻译文本
                        RectangleF drawRect = new RectangleF(
                            originalX,
                            originalY,
                            originalWidth,
                            Math.Max(originalHeight, textSize.Height)
                        );
                        
                        StringFormat format = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            Trimming = StringTrimming.Word,
                            FormatFlags = StringFormatFlags.LineLimit
                        };
                        
                        g.DrawString(visionBlock.Text, font, textBrush, drawRect, format);
                        
                        font.Dispose();
                    }
                }
            }
            
            return translatedBitmap;
        }

        private async void btnSavePdf_Click(object? sender, EventArgs e)
        {
            if (translatedPages.Count == 0)
            {
                MessageBox.Show("没有可保存的翻译内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PDF 文件|*.pdf";
                saveFileDialog.Title = "保存翻译后的 PDF";
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(currentPdfPath) + "_translated.pdf";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 禁用保存按钮，防止重复点击
                    btnSavePdf.Enabled = false;
                    try
                    {
                        await SaveTranslatedPdfAsync(saveFileDialog.FileName).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() => btnSavePdf.Enabled = true));
                        }
                        else
                        {
                            btnSavePdf.Enabled = true;
                        }
                    }
                }
            }
        }

        private async Task SaveTranslatedPdfAsync(string outputPath)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateStatus("正在保存 PDF...")));
                }
                else
                {
                    UpdateStatus("正在保存 PDF...");
                }

                await Task.Run(() =>
                {
                    using (PdfSharpCore.Pdf.PdfDocument document = new PdfSharpCore.Pdf.PdfDocument())
                    {
                        document.Info.Title = "翻译后的PDF";
                        document.Info.Author = "PDF智能翻译工具";

                        int totalPages = translatedPages.Count;
                        
                        for (int i = 0; i < totalPages; i++)
                        {
                            var pagePath = translatedPages[i];
                            if (string.IsNullOrWhiteSpace(pagePath) || !File.Exists(pagePath))
                                continue;

                            PdfSharpCore.Pdf.PdfPage page = document.AddPage();
                            
                            // 使用原始页面的尺寸信息
                            PageInfo? pageInfo = i < pageInfos.Count ? pageInfos[i] : null;
                            if (pageInfo != null && pageInfo.PdfWidth > 0 && pageInfo.PdfHeight > 0)
                            {
                                page.Width = XUnit.FromPoint(pageInfo.PdfWidth);
                                page.Height = XUnit.FromPoint(pageInfo.PdfHeight);
                            }
                            else
                            {
                                // 默认A4尺寸
                                page.Width = XUnit.FromMillimeter(210);
                                page.Height = XUnit.FromMillimeter(297);
                            }

                            using (XGraphics gfx = XGraphics.FromPdfPage(page))
                            {
                                using (XImage xImage = XImage.FromFile(pagePath))
                                {
                                    double scaleX = page.Width / xImage.PixelWidth;
                                    double scaleY = page.Height / xImage.PixelHeight;
                                    double scale = Math.Min(scaleX, scaleY);

                                    double width = xImage.PixelWidth * scale;
                                    double height = xImage.PixelHeight * scale;

                                    double x = (page.Width - width) / 2;
                                    double y = (page.Height - height) / 2;

                                    gfx.DrawImage(xImage, x, y, width, height);
                                }
                            }

                            // 更新进度（每10页更新一次，避免频繁UI更新）
                            if ((i + 1) % 10 == 0 || i + 1 == totalPages)
                            {
                                int progress = (int)((i + 1) * 100.0 / totalPages);
                                if (InvokeRequired)
                                {
                                    BeginInvoke(new Action(() => 
                                    {
                                        UpdateStatus($"正在保存 PDF... ({i + 1} / {totalPages})");
                                        progressBar.Value = Math.Min(progress, 100);
                                    }));
                                }
                                else
                                {
                                    UpdateStatus($"正在保存 PDF... ({i + 1} / {totalPages})");
                                    progressBar.Value = Math.Min(progress, 100);
                                }
                            }
                        }

                        document.Save(outputPath);
                    }
                }).ConfigureAwait(false);

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        UpdateStatus($"✓ PDF 已保存: {Path.GetFileName(outputPath)}");
                        progressBar.Value = 100;
                        MessageBox.Show("✓ 翻译后的 PDF 已成功保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
                else
                {
                    UpdateStatus($"✓ PDF 已保存: {Path.GetFileName(outputPath)}");
                    progressBar.Value = 100;
                    MessageBox.Show("✓ 翻译后的 PDF 已成功保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"保存 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UpdateStatus("保存 PDF 失败");
                    }));
                }
                else
                {
                    MessageBox.Show($"保存 PDF 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("保存 PDF 失败");
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                // 使用 BeginInvoke 而不是 Invoke，避免阻塞
                BeginInvoke(new Action<string>(UpdateStatus), message);
                return;
            }
            lblStatus.Text = message;
            // 移除 Application.DoEvents()，避免在异步操作中导致死锁
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            httpClient?.Dispose();

            // 清理临时目录（可选）
            try
            {
                ClearTempDir(ref originalTempDir, ref originalTempReady);
                ClearTempDir(ref translationTempDir, ref translationTempReady);
            }
            catch { }
        }
    }


    // 文本块信息（用于JSON序列化）
    public class TextBlockInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = "";

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("fontSize")]
        public float FontSize { get; set; }

        [JsonProperty("fontName")]
        public string FontName { get; set; } = "";
    }

    // PDF页面尺寸信息
    public class PageInfo
    {
        public float PdfWidth { get; set; }
        public float PdfHeight { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }

    // 视觉翻译返回的文本块信息（包含边界框位置）
    public class VisionTextBlock
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = "";

        [JsonProperty("fontSize")]
        public float FontSize { get; set; }
    }
}
