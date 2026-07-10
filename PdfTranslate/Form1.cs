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
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace PdfTranslate
{


    public partial class Form1 : ReaLTaiizor.Forms.CrownForm
    {
        private string? currentPdfPath;
        private int currentPageIndex = 0;
        private int totalPages = 0;
        // 原始页面改为仅存储临时文件路径，减少内存占用
        private List<string> originalPages = new List<string>();
        // 翻译结果仅记录临时文件路径，避免占用内存（key: 页码，value: 文件路径）
        private Dictionary<int, string> translatedPages = new Dictionary<int, string>();
        private List<string> pageTexts = new List<string>();
        private List<List<TextBlockInfo>> pageTextBlocks = new List<List<TextBlockInfo>>(); // 存储每页的文本块信息
        private List<List<PdfImageRegion>> pageImageRegions = new List<List<PdfImageRegion>>(); // 存储每页图片边界框（PDF坐标）
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
        //字体间距
        private float fontSpacing = 3.0f;
        
        // 目标翻译语言
        private string targetLanguage = "中文";
        private Dictionary<string, string> supportedLanguages = new Dictionary<string, string>
        {
            { "中文", "Chinese" },
            { "英文", "English" },
            { "日文", "Japanese" },
            { "韩文", "Korean" },
            { "法文", "French" },
            { "德文", "German" },
            { "西班牙文", "Spanish" },
            { "俄文", "Russian" },
            { "阿拉伯文", "Arabic" },
            { "葡萄牙文", "Portuguese" },
            { "意大利文", "Italian" },
            { "泰文", "Thai" },
            { "越南文", "Vietnamese" }
        };
        
        public Form1()
        {
            InitializeComponent();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            
            // 初始化语言选择
            InitializeLanguageSelector();

            // 启用高质量渲染，消除圆角锯齿
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            // 设置窗体属性以改善渲染质量
            this.Load += Form1_Load;
            this.Resize += Form1_Resize;

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

        private void InitializeLanguageSelector()
        {
            // 创建语言选择标签
            Label labelLanguage = new Label
            {
                Name = "labelLanguage",
                Text = "目标语言：",
                AutoSize = true,
                Location = new Point(this.Width - 240, 15),
                Font = new Font("Microsoft YaHei", 9F)
            };
            
            // 创建语言选择下拉框
            ComboBox languageComboBox = new ComboBox
            {
                Name = "comboBoxLanguage",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                Location = new Point(this.Width - 150, 12),
                Font = new Font("Microsoft YaHei", 9F)
            };
            
            // 添加语言选项
            foreach (var lang in supportedLanguages.Keys)
            {
                languageComboBox.Items.Add(lang);
            }
            
            // 添加选择改变事件
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            
            // 添加到窗体（需要先添加才能查找控件）
            this.Controls.Add(labelLanguage);
            this.Controls.Add(languageComboBox);
            
            // 加载用户之前的语言选择，如果没有则默认选中中文
            LoadLanguagePreference();
            if (languageComboBox.SelectedIndex == -1)
            {
                languageComboBox.SelectedIndex = 0;
            }
            
            // 监听窗体大小改变，自动调整控件位置
            this.Resize += (s, e) =>
            {
                if (this.Controls.ContainsKey("labelLanguage"))
                {
                    this.Controls["labelLanguage"]!.Location = new Point(this.Width - 240, 15);
                }
                if (this.Controls.ContainsKey("comboBoxLanguage"))
                {
                    this.Controls["comboBoxLanguage"]!.Location = new Point(this.Width - 150, 12);
                }
            };
            
            // 将控件置于最前
            labelLanguage.BringToFront();
            languageComboBox.BringToFront();
            
            // 更新标题栏
            UpdateFormTitle();
        }
        
        private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                targetLanguage = comboBox.SelectedItem.ToString() ?? "中文";
                UpdateFormTitle();
                SaveLanguagePreference();
            }
        }
        
        private void SaveLanguagePreference()
        {
            try
            {
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PdfTranslate", "config.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                File.WriteAllText(configPath, targetLanguage);
            }
            catch { }
        }
        
        private void LoadLanguagePreference()
        {
            try
            {
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PdfTranslate", "config.txt");
                if (File.Exists(configPath))
                {
                    string savedLanguage = File.ReadAllText(configPath).Trim();
                    if (supportedLanguages.ContainsKey(savedLanguage))
                    {
                        targetLanguage = savedLanguage;
                        if (this.Controls.ContainsKey("comboBoxLanguage") && this.Controls["comboBoxLanguage"] is ComboBox comboBox)
                        {
                            comboBox.SelectedItem = savedLanguage;
                        }
                    }
                }
            }
            catch { }
        }
        
        private void UpdateFormTitle()
        {
            this.Text = $"PDF 翻译工具 - 目标语言：{targetLanguage}";
        }
        
        private string GetTargetLanguageEnglish()
        {
            return supportedLanguages.TryGetValue(targetLanguage, out var englishName) ? englishName : "Chinese";
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
                        originalPages.Clear();
                        translatedPages.Clear();
                        pageTexts.Clear();
                        pageTextBlocks.Clear();
                        pageImageRegions.Clear();
                        pageInfos.Clear();
                        UpdateStatus("正在加载 PDF 文档...");
                    }));
                }
                else
                {
                  
                    originalPages.Clear();
                    translatedPages.Clear();
                    pageTexts.Clear();
                    pageTextBlocks.Clear();
                    pageImageRegions.Clear();
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

                                    // 提取文本块信息 - 使用智能分组
                                    List<TextBlockInfo> textBlocks = new List<TextBlockInfo>();
                                    var words = page.GetWords().ToList();
                                    var imageRegions = page.GetImages()
                                        .Select(img => new PdfImageRegion
                                        {
                                            Left = img.Bounds.Left,
                                            Right = img.Bounds.Right,
                                            Bottom = img.Bounds.Bottom,
                                            Top = img.Bounds.Top
                                        })
                                        .Where(r => r.Width > 12 && r.Height > 12) // 过滤极小图形噪声
                                        .ToList();
                                    pageImageRegions.Add(imageRegions);

                                    // 将单词按段落分组，合并为完整文本
                                    var paragraphs = ParseTextIntoParagraphs(i,words, imageRegions);
                                    int blockId = 0;

                                    foreach (var paragraph in paragraphs)
                                    {
                                        if (string.IsNullOrWhiteSpace(paragraph.Text)) continue;

                                        textBlocks.Add(new TextBlockInfo
                                        {
                                            Id = blockId++,
                                            Text = paragraph.Text,
                                            X = (float)paragraph.X,
                                            Y = (float)paragraph.Y,
                                            Width = (float)paragraph.Width,
                                            Height = (float)paragraph.Height,
                                            FontSize = (float)paragraph.FontSize,
                                            FontName = paragraph.FontName,
                                            IsBold = paragraph.IsBold,
                                            Lines = paragraph.Lines  // 传递行信息
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
                                    pageImageRegions.Add(new List<PdfImageRegion>());
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

                        // 准备原图临时目录，检查是否需要重新渲染
                        var (success, needRender) = EnsureTempDirEx(ref originalTempDir, ref originalTempReady, "original");
                        if (!success)
                        {
                            return;
                        }

                        if (!needRender)
                        {
                            // 使用缓存，直接从目录加载已有文件
                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => UpdateStatus("正在加载缓存文件...")));
                            }
                            else
                            {
                                UpdateStatus("正在加载缓存文件...");
                            }

                            var cachedFiles = Directory.GetFiles(originalTempDir!, "orig_*.png")
                                .OrderBy(f => f)
                                .ToList();

                            foreach (var file in cachedFiles)
                            {
                                originalPages.Add(file);
                                
                                // 读取图像尺寸并更新 pageInfos
                                try
                                {
                                    using (var img = Image.FromFile(file))
                                    {
                                        int pageIndex = originalPages.Count - 1;
                                        if (pageIndex < pageInfos.Count)
                                        {
                                            pageInfos[pageIndex].ImageWidth = img.Width;
                                            pageInfos[pageIndex].ImageHeight = img.Height;
                                        }
                                    }
                                }
                                catch { }
                            }

                            if (InvokeRequired)
                            {
                                BeginInvoke(new Action(() => UpdateStatus($"已加载 {cachedFiles.Count} 页缓存")));
                            }
                            else
                            {
                                UpdateStatus($"已加载 {cachedFiles.Count} 页缓存");
                            }
                        }
                        else
                        {
                            // 需要重新渲染
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
        /// <summary>
        /// 确保临时目录存在，返回值：(是否成功, 是否需要重新渲染)
        /// </summary>
        private (bool success, bool needRender) EnsureTempDirEx(ref string? targetDir, ref bool readyFlag, string subFolder)
        {
            if (readyFlag && !string.IsNullOrWhiteSpace(targetDir) && Directory.Exists(targetDir))
            {
                return (true, false); // 已准备好，不需要重新渲染
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
                    // 检查目录中是否有文件
                    var existingFiles = Directory.GetFiles(targetDir, "*.png");
                    
                    if (existingFiles.Length > 0)
                    {
                        // 统一询问用户是否使用缓存
                        string message = subFolder == "translated"
                            ? $"检测到翻译缓存 ({existingFiles.Length} 页)\n是否继续翻译？\n\n" +
                              $"选择【是】：从上次中断处继续（断点续传）\n选择【否】：清空缓存，重新翻译"
                            : $"检测到渲染缓存 ({existingFiles.Length} 页)\n是否使用缓存？\n\n" +
                              $"选择【是】：直接加载缓存（快速）\n选择【否】：重新渲染（慢，但最新）";

                        var result = MessageBox.Show(message, "发现缓存", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            // 使用现有缓存
                            readyFlag = true;
                            return (true, false); // 成功，不需要重新渲染/翻译
                        }
                        else
                        {
                            // 重新渲染/翻译，删除旧文件
                            Directory.Delete(targetDir, true);
                        }
                    }
                    else
                    {
                        // 目录存在但没有文件，直接删除
                        Directory.Delete(targetDir, true);
                    }
                }

                Directory.CreateDirectory(targetDir);
                readyFlag = true;
                return (true, true); // 成功，需要重新渲染
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建临时目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, false);
            }
        }

        // 保留旧的方法签名以兼容（内部调用新方法）
        private bool EnsureTempDir(ref string? targetDir, ref bool readyFlag, string subFolder)
        {
            var (success, _) = EnsureTempDirEx(ref targetDir, ref readyFlag, subFolder);
            return success;
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
                if (translatedPages.ContainsKey(currentPageIndex))
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

                // 按页码顺序遍历字典
                foreach (var kvp in translatedPages.OrderBy(x => x.Key))
                {
                    int pageIndex = kvp.Key;
                    string pagePath = kvp.Value;

                    if (!File.Exists(pagePath))
                    {
                        continue;
                    }

                    using var img = Image.FromFile(pagePath);

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
                        Name = $"translatedPageBox_{pageIndex}"
                    };

                    // 添加页码标签
                    Label pageLabel = new Label
                    {
                        Text = $"第 {pageIndex + 1} 页",
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
                // 确保临时目录，检查是否需要重新翻译
                var (success, needRender) = EnsureTempDirEx(ref translationTempDir, ref translationTempReady, "translated");
                if (!success)
                {
                    // 用户取消
                    isTranslating = false;
                    btnTranslate.Enabled = true;
                    btnSelectPdf.Enabled = true;
                    btnTranslate.Text = "🚀 开始翻译";
                    return;
                }

                // 检查已翻译的文件，确定起始页
                int startPage = 0;
                translatedPages.Clear();

                if (!needRender && Directory.Exists(translationTempDir))
                {
                    // 加载已有的翻译文件
                    var existingFiles = Directory.GetFiles(translationTempDir, "page_*.png")
                        .OrderBy(f => f)
                        .ToList();

                    foreach (var file in existingFiles)
                    {
                        // 从文件名提取页码 (page_0001.png -> 0)
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var pageNumStr = fileName.Replace("page_", "");
                        if (int.TryParse(pageNumStr, out int pageNum) && pageNum > 0)
                        {
                            translatedPages[pageNum - 1] = file;
                        }
                    }

                    startPage = translatedPages.Count;

                    if (startPage > 0)
                    {
                        // 从最后一个文件重新开始翻译（因为可能翻译不完整）
                        startPage = startPage - 1;
                        
                        // 更新UI显示已翻译的页面
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                lblTranslatedPageInfo.Text = $"✨ 已翻译: {translatedPages.Count} / {totalPages}";
                                progressBar.Value = startPage;
                                UpdateStatus($"📋 检测到 {translatedPages.Count} 页已翻译，从第 {startPage + 1} 页继续...");
                                
                                // 显示已翻译的页面
                                DisplayTranslatedPages();
                            }));
                        }
                        else
                        {
                            lblTranslatedPageInfo.Text = $"✨ 已翻译: {translatedPages.Count} / {totalPages}";
                            progressBar.Value = startPage;
                            UpdateStatus($"📋 检测到 {translatedPages.Count} 页已翻译，从第 {startPage + 1} 页继续...");
                            
                            // 显示已翻译的页面
                            DisplayTranslatedPages();
                        }
                        
                        await Task.Delay(1500); // 让用户看到提示
                    }
                }

                for (int i = startPage; i < totalPages; i++)
                {
                    //if (i != 1)
                    //{
                    //    continue;
                    //}

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

                    var pageImagePath = originalPages[i];


                    System.Drawing.Image? translatedImage = null;
                    var pageText = pageTexts[i];

                    // 智能选择翻译方式
                    if (!string.IsNullOrWhiteSpace(pageText) && pageText.Length > 1)
                    {
                        using (var pageImage = Image.FromFile(pageImagePath))
                        {
                            // 使用文本翻译（更快更准）
                            translatedImage = await TranslatePageWithText(pageImage, pageText, i, pageTextBlocks[i]).ConfigureAwait(false);
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
                        translatedPages[i] = pageFilePath;

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

        /// <summary>
        /// 基于空间布局将单词分组成文本块
        /// </summary>
        private List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoBlocks(List<UglyToad.PdfPig.Content.Word> words)
        {
            var result = new List<List<UglyToad.PdfPig.Content.Word>>();
            if (!words.Any()) return result;

            // 按Y坐标（从上到下）排序单词
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left).ToList();

            var currentGroup = new List<UglyToad.PdfPig.Content.Word> { sortedWords[0] };
            double lastYBottom = sortedWords[0].BoundingBox.Bottom;
            double lastXRight = sortedWords[0].BoundingBox.Right;
            double lastHeight = sortedWords[0].BoundingBox.Height;

            for (int i = 1; i < sortedWords.Count; i++)
            {
                var word = sortedWords[i];
                double yDiff = Math.Abs(word.BoundingBox.Bottom - lastYBottom);
                double xGap = word.BoundingBox.Left - lastXRight;

                // 判断是否应该开始新的文本块：
                // 1. Y坐标差异超过阈值（新行且不在同一行）
                // 2. X坐标间隙过大（可能是列分隔或段落分隔）
                double yThreshold = lastHeight * 0.5; // Y方向阈值为行高的50%
                double xThreshold = lastHeight * 2.0; // X方向阈值为行高的2倍

                if (yDiff > yThreshold || xGap > xThreshold)
                {
                    // 开始新的文本块
                    if (currentGroup.Any())
                    {
                        result.Add(currentGroup);
                    }
                    currentGroup = new List<UglyToad.PdfPig.Content.Word>();
                }

                currentGroup.Add(word);
                lastYBottom = word.BoundingBox.Bottom;
                lastXRight = word.BoundingBox.Right;
                lastHeight = word.BoundingBox.Height;
            }

            // 添加最后一组
            if (currentGroup.Any())
            {
                result.Add(currentGroup);
            }

            return result;
        }

        /// <summary>
        /// 按换行将单词列表分组为完整的段落（返回段落信息，包含合并后的文本和位置）
        /// 综合判断：Y坐标（行间距）、X坐标（左右对齐位置）、字体大小变化
        /// </summary>
        /// <param name="words">PDF单词列表</param>
        /// <returns>段落信息列表，每个段落包含合并后的文本和边界框</returns>
        private List<ParagraphInfo> ParseTextIntoParagraphs(int pageIndex, List<UglyToad.PdfPig.Content.Word> words, List<PdfImageRegion>? imageRegions = null)
        {
            var paragraphs = new List<ParagraphInfo>();

            if (words == null || !words.Any())
            {
                return paragraphs;
            }

            // 先获取Word对象的段落分组
            var wordParagraphs = ParseWordsIntoParagraphs(words);
            var paragraphBounds = wordParagraphs
                .Where(g => g.Any())
                .Select(g => new
                {
                    Left = g.Min(w => w.BoundingBox.Left),
                    Right = g.Max(w => w.BoundingBox.Right)
                })
                .ToList();

            // 将每个段落的单词合并为完整文本，并计算边界框
            foreach (var wordGroup in wordParagraphs)
            {
                if (!wordGroup.Any()) continue;

                // 合并段落文本（单词之间用空格连接）
                string paragraphText = string.Join(" ", wordGroup.Select(w => w.Text));

                // 计算段落的边界框（按行计算，避免包含右侧图片区域）
                double left = wordGroup.Min(w => w.BoundingBox.Left);
                double right = wordGroup.Max(w => w.BoundingBox.Right);
                double bottom = wordGroup.Min(w => w.BoundingBox.Bottom);
                double top = wordGroup.Max(w => w.BoundingBox.Top);
                
                // 按Y坐标将单词分组为行，并记录每行信息
                var lines = wordGroup
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1)) // 按Y坐标分组
                    .OrderByDescending(g => g.Key) // 从上到下
                    .ToList();
                
                // 记录每行的详细信息（宽度、Y坐标）
                var lineInfos = new List<LineInfo>();
                double maxLineWidth = 0;
                
                foreach (var line in lines)
                {
                    var lineWords = line.OrderBy(w => w.BoundingBox.Left).ToList();
                    if (lineWords.Any())
                    {
                        double lineLeft = lineWords.First().BoundingBox.Left;
                        double lineRight = lineWords.Last().BoundingBox.Right;
                        double lineWidth = lineRight - lineLeft;
                        double lineBottom = lineWords.Min(w => w.BoundingBox.Bottom);
                        double lineTop = lineWords.Max(w => w.BoundingBox.Top);
                        double lineHeight = lineTop - lineBottom;
                        
                        // 记录行信息
                        lineInfos.Add(new LineInfo
                        {
                            Y = lineBottom,
                            Left = lineLeft,
                            Right = lineRight,
                            Width = lineWidth,
                            Height = lineHeight
                        });
                        
                        maxLineWidth = Math.Max(maxLineWidth, lineWidth);
                    }
                }
 
                // 两种场景：
                // 1) 图文混排（右侧有图片挤占）：使用实际文字行宽，避免覆盖图片区域；
                // 2) 普通完整段落（两侧无图片）：使用段落整体区域宽度，避免宽度被某一行左缩进/短行误判。
                bool isImageTextMixed = IsImageTextMixed(left, right, bottom, top, lineInfos, imageRegions);

                double paragraphRegionWidth = right - left;
                if (!isImageTextMixed)
                {
                    // 普通段落：用同列段落的右边界估算整行宽度，避免短句段落只得到“文字本身宽度”。
                    double leftAlignTolerance = Math.Max(10.0, lineInfos.Any() ? lineInfos.Average(l => l.Height) * 1.5 : 14.0);
                    double alignedColumnRight = paragraphBounds
                        .Where(b => Math.Abs(b.Left - left) <= leftAlignTolerance)
                        .Select(b => b.Right)
                        .DefaultIfEmpty(right)
                        .Max();

                    if (alignedColumnRight > right)
                    {
                        paragraphRegionWidth = alignedColumnRight - left;
                    }
                }
                double width = isImageTextMixed
                    ? (maxLineWidth > 0 ? maxLineWidth : paragraphRegionWidth)
                    : paragraphRegionWidth;
                double height = top - bottom;
             
                // 计算平均字体大小
                double avgFontSize = wordGroup.Average(w => w.BoundingBox.Height);

                if (paragraphText.Contains("Ole K."))
                {
                    Debug.WriteLine(avgFontSize);
                }

                // 获取字体信息（使用第一个单词的字体）
                string fontName = "Arial";
                bool isBold = false;

                var firstWord = wordGroup[0];
                if (firstWord.Letters.Count > 0)
                {
                    var firstLetter = firstWord.Letters[0];
                    fontName = firstLetter.FontName ?? "Arial";
                    avgFontSize = firstLetter.FontSize; // 使用实际字体大小

                    // 检测是否加粗：字体名称包含 Bold、Heavy、Black 等关键词
                    string fontNameLower = fontName.ToLower();
                    isBold = fontNameLower.Contains("bold") ||
                             fontNameLower.Contains("heavy") ||
                             fontNameLower.Contains("black") ||
                             fontNameLower.Contains("semibold") ||
                             fontNameLower.Contains("extrabold");
                }

                paragraphs.Add(new ParagraphInfo
                {
                    Text = paragraphText,
                    X = left,
                    Y = bottom,
                    Width = width,
                    Height = height,
                    FontSize = avgFontSize,
                    FontName = fontName,
                    IsBold = isBold,
                    Lines = lineInfos  // 记录每行的宽度信息
                });
            }

            return paragraphs;
        }

        /// <summary>
        /// 判断是否为图文混排：
        /// - 忽略最后一行（普通段落最后一行通常较短）；
        /// - 至少两行明显右侧收窄，才认为右侧存在图片占位。
        /// </summary>
        private bool IsImageTextMixed(List<LineInfo>? lines)
        {
            if (lines == null || lines.Count < 3)
            {
                return false;
            }

            var orderedLines = lines
                .OrderByDescending(l => l.Y)
                .ToList();

            // 忽略最后一行，减少普通段落末行短句导致的误判
            var analysisLines = orderedLines.Take(orderedLines.Count - 1).ToList();
            if (analysisLines.Count < 2)
            {
                return false;
            }

            double maxWidth = analysisLines.Max(l => l.Width);
            if (maxWidth <= 0)
            {
                return false;
            }

            double narrowWidthThreshold = maxWidth * 0.82; // 小于主宽度82%视为明显变窄
            double fullRight = analysisLines.Max(l => l.Right);
            double rightInsetThreshold = Math.Max(6.0, maxWidth * 0.08); // 右边至少内缩8%或6pt

            var narrowLines = analysisLines
                .Where(l => l.Width < narrowWidthThreshold)
                .ToList();

            if (narrowLines.Count < 2)
            {
                return false;
            }

            int rightInsetNarrowLines = narrowLines.Count(l => (fullRight - l.Right) > rightInsetThreshold);
            return rightInsetNarrowLines >= 2;
        }

        /// <summary>
        /// 图文混排综合判断：优先使用图片边界框，若无图片数据再回退到行宽启发式。
        /// </summary>
        private bool IsImageTextMixed(double textLeft, double textRight, double textBottom, double textTop, List<LineInfo>? lines, List<PdfImageRegion>? imageRegions)
        {
            if (imageRegions != null && imageRegions.Count > 0)
            {
                return HasSideImageOverlap(textLeft, textRight, textBottom, textTop, lines, imageRegions);
            }

            return IsImageTextMixed(lines);
        }

        /// <summary>
        /// 判断段落左右两侧是否有与文本行重叠的图片区域。
        /// 优先按“行级别”判断，避免使用整段边界导致的漏判。
        /// </summary>
        private bool HasSideImageOverlap(double textLeft, double textRight, double textBottom, double textTop, List<LineInfo>? lines, List<PdfImageRegion> imageRegions)
        {
            double textHeight = Math.Max(1.0, textTop - textBottom);
            double textWidth = Math.Max(1.0, textRight - textLeft);
            var validLines = lines?
                .Where(l => l.Width > 1 && l.Height > 1)
                .ToList() ?? new List<LineInfo>();

            // 行级判定：只要有足够的行被右侧图片“挤压”，就视为图文混排
            if (validLines.Count > 0)
            {
                int requiredMatchedLines = validLines.Count >= 3 ? 2 : 1;
                int matchedLines = 0;

                foreach (var line in validLines)
                {
                    double lineBottom = line.Y;
                    double lineTop = line.Y + line.Height;
                    bool currentLineMatched = false;

                    foreach (var image in imageRegions)
                    {
                        double overlapBottom = Math.Max(lineBottom, image.Bottom);
                        double overlapTop = Math.Min(lineTop, image.Top);
                        double overlapHeight = overlapTop - overlapBottom;
                        double minLineOverlap = Math.Max(1.0, line.Height * 0.45);
                        if (overlapHeight < minLineOverlap)
                        {
                            continue;
                        }

                        // 图片起始位置要在该行文本的右半区域之后，避免把左侧插图误判为右侧图文混排
                        if (image.Left <= line.Left + line.Width * 0.55)
                        {
                            // 非右侧候选，继续检查左侧候选
                        }

                        // 允许极小量重叠（OCR/提取误差）
                        double maxAllowedOverlap = Math.Max(2.0, line.Width * 0.02);
                        double maxHorizontalDistance = Math.Max(24.0, line.Width * 0.6);

                        // 右侧图片：图片起始在文本右边，且距离不要过远
                        double rightGap = image.Left - line.Right;
                        bool rightSideMatch =
                            image.Left > line.Left + line.Width * 0.55 &&
                            rightGap >= -maxAllowedOverlap &&
                            rightGap <= maxHorizontalDistance;

                        // 左侧图片：图片结束在文本左边，且距离不要过远
                        double leftGap = line.Left - image.Right;
                        bool leftSideMatch =
                            image.Right < line.Right - line.Width * 0.55 &&
                            leftGap >= -maxAllowedOverlap &&
                            leftGap <= maxHorizontalDistance;

                        if (rightSideMatch || leftSideMatch)
                        {
                            currentLineMatched = true;
                            break;
                        }
                    }

                    if (currentLineMatched)
                    {
                        matchedLines++;
                        if (matchedLines >= requiredMatchedLines)
                        {
                            return true;
                        }
                    }
                }
            }

            // 段落级兜底：没有行信息时再用整段边界判断（更宽松）
            double minVerticalOverlapFallback = textHeight * 0.25;
            double sideToleranceFallback = Math.Max(10.0, textWidth * 0.06);
            double maxDistanceFallback = Math.Max(24.0, textWidth * 0.6);
            foreach (var image in imageRegions)
            {
                double overlapBottom = Math.Max(textBottom, image.Bottom);
                double overlapTop = Math.Min(textTop, image.Top);
                double overlapHeight = overlapTop - overlapBottom;
                if (overlapHeight < minVerticalOverlapFallback)
                {
                    continue;
                }

                double rightGap = image.Left - textRight;
                bool rightSideMatch =
                    image.Left >= textRight - sideToleranceFallback &&
                    rightGap <= maxDistanceFallback;

                double leftGap = textLeft - image.Right;
                bool leftSideMatch =
                    image.Right <= textLeft + sideToleranceFallback &&
                    leftGap <= maxDistanceFallback;

                if (rightSideMatch || leftSideMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 将单词列表按段落分组（内部方法，返回Word对象列表）
        /// </summary>
        private List<List<UglyToad.PdfPig.Content.Word>> ParseWordsIntoParagraphs(List<UglyToad.PdfPig.Content.Word> words)
        {
            var paragraphs = new List<List<UglyToad.PdfPig.Content.Word>>();

            if (words == null || !words.Any())
            {
                return paragraphs;
            }

            // 按Y坐标（从上到下）排序，然后按X坐标（从左到右）排序
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left).ToList();

            // 第一步：将单词按行分组
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();
            var currentLine = new List<UglyToad.PdfPig.Content.Word> { sortedWords[0] };
            double lastYBottom = sortedWords[0].BoundingBox.Bottom;
            double lastHeight = sortedWords[0].BoundingBox.Height;

            for (int i = 1; i < sortedWords.Count; i++)
            {
                var word = sortedWords[i];
                double yDiff = Math.Abs(word.BoundingBox.Bottom - lastYBottom);
                double yThreshold = lastHeight * 0.3; // 同一行的Y坐标差异阈值

                if (yDiff > yThreshold)
                {
                    // 换行了，保存当前行
                    if (currentLine.Any())
                    {
                        lines.Add(currentLine);
                    }
                    currentLine = new List<UglyToad.PdfPig.Content.Word>();
                }

                currentLine.Add(word);
                lastYBottom = word.BoundingBox.Bottom;
                lastHeight = word.BoundingBox.Height;
            }

            // 添加最后一行
            if (currentLine.Any())
            {
                lines.Add(currentLine);
            }

            if (!lines.Any())
            {
                return paragraphs;
            }

            // 第二步：将行分组为段落（主要基于行间距判断）
            var currentParagraph = new List<UglyToad.PdfPig.Content.Word>();
            currentParagraph.AddRange(lines[0]);

            // 记录上一行的特征
            double lastLineBottom = lines[0].Min(w => w.BoundingBox.Bottom);
            double lastLineHeight = lines[0].Average(w => w.BoundingBox.Height);
            double lastLineLeft = lines[0].Min(w => w.BoundingBox.Left);

            for (int i = 1; i < lines.Count; i++)
            {
                var currentLineWords = lines[i];

                // 当前行的特征
                double currentLineTop = currentLineWords.Max(w => w.BoundingBox.Top);
                double currentLineBottom = currentLineWords.Min(w => w.BoundingBox.Bottom);
                double currentLineHeight = currentLineWords.Average(w => w.BoundingBox.Height);
                double currentLineLeft = currentLineWords.Min(w => w.BoundingBox.Left);

                // 计算行间距（Y坐标差异）
                double lineSpacing = lastLineBottom - currentLineTop;
                double avgHeight = (lastLineHeight + currentLineHeight) / 2.0;

                // 计算左对齐差异
                double leftDiff = Math.Abs(currentLineLeft - lastLineLeft);

                // 计算字体大小差异
                double heightDiff = Math.Abs(currentLineHeight - lastLineHeight);

                // 判断是否是新段落
                bool isNewParagraph = false;

                // 核心判断：行间距是否超过2倍字体高度
                double paragraphSpacingThreshold = avgHeight * 2.0;
                if (lineSpacing > paragraphSpacingThreshold)
                {
                    // 行间距过大，明确的段落分隔
                    isNewParagraph = true;
                }
                // 辅助判断1：左对齐位置变化非常大（超过2倍字符宽度）
                else if (leftDiff > avgHeight * 2.0)
                {
                    // 左边距变化很大，可能是不同段落或列
                    isNewParagraph = true;
                }
                // 辅助判断2：字体大小变化明显（超过80%）
                else if (heightDiff > avgHeight * 0.8)
                {
                    // 字体大小变化明显，可能是标题
                    isNewParagraph = true;
                }

                if (isNewParagraph)
                {
                    // 开始新段落
                    if (currentParagraph.Any())
                    {
                        paragraphs.Add(currentParagraph);
                    }
                    currentParagraph = new List<UglyToad.PdfPig.Content.Word>();
                }

                // 将当前行的单词添加到当前段落
                currentParagraph.AddRange(currentLineWords);

                // 更新上一行的特征
                lastLineBottom = currentLineBottom;
                lastLineHeight = currentLineHeight;
                lastLineLeft = currentLineLeft;
            }

            // 添加最后一个段落
            if (currentParagraph.Any())
            {
                paragraphs.Add(currentParagraph);
            }

            return paragraphs;
        }

        private async Task<System.Drawing.Image?> TranslatePageWithText(System.Drawing.Image pageImage, string pageText, int pageNumber, List<TextBlockInfo> originalTextBlocks)
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

                // 检查是否有文本块需要翻译
                if (originalTextBlocks.Count == 0)
                {
                    return null;
                }

                var allTranslatedTexts = new List<string>();

                // 逐个翻译每个文本块
                for (int blockIndex = 0; blockIndex < originalTextBlocks.Count; blockIndex++)
                {
                    var currentBlock = originalTextBlocks[blockIndex];

                    // 跳过空文本块
                    if (string.IsNullOrWhiteSpace(currentBlock.Text))
                    {
                        allTranslatedTexts.Add("");
                        continue;
                    }

                    var requestBody = new
                    {
                        model = "Qwen3 VL 8B",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = $"将以下文本翻译成{targetLanguage}，只返回翻译结果，不要添加任何解释：\\n\\n{currentBlock.Text}"
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
                        // 翻译失败，使用原文
                        allTranslatedTexts.Add(currentBlock.Text);
                        System.Diagnostics.Debug.WriteLine($"警告: 块 {blockIndex} 翻译为空，使用原文（页面 {pageNumber}）");
                        continue;
                    }

                    // 清理翻译结果
                    translatedText = translatedText.Trim();

                    // 移除可能的引号
                    if (translatedText.StartsWith("\"") && translatedText.EndsWith("\""))
                    {
                        translatedText = translatedText.Substring(1, translatedText.Length - 2);
                    }

                    allTranslatedTexts.Add(translatedText);
                }

                // 检查翻译结果数量
                if (allTranslatedTexts.Count < originalTextBlocks.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 翻译块数({allTranslatedTexts.Count})少于原始块数({originalTextBlocks.Count})");
                    // 用原文填充缺失的部分
                    for (int i = allTranslatedTexts.Count; i < originalTextBlocks.Count; i++)
                    {
                        allTranslatedTexts.Add(originalTextBlocks[i].Text);
                    }
                }

                // 检测重复内容
                var duplicateGroups = allTranslatedTexts
                    .Select((text, index) => new { text, index })
                    .GroupBy(x => x.text)
                    .Where(g => g.Count() > 1 && g.Key.Length > 20) // 只检查长度>20的重复
                    .ToList();

                if (duplicateGroups.Any())
                {
                    foreach (var group in duplicateGroups)
                    {
                        var indices = string.Join(", ", group.Select(x => x.index));
                        System.Diagnostics.Debug.WriteLine($"警告: 检测到重复的翻译内容在块 [{indices}]: {group.Key.Substring(0, Math.Min(50, group.Key.Length))}...");
                    }
                }

                // 映射到原始文本块
                var translatedTextBlocks = new List<TextBlockInfo>();
                for (int i = 0; i < originalTextBlocks.Count; i++)
                {
                    var originalBlock = originalTextBlocks[i];
                    var translatedBlockText = i < allTranslatedTexts.Count ? allTranslatedTexts[i] : originalBlock.Text;

                    translatedTextBlocks.Add(new TextBlockInfo
                    {
                        Id = originalBlock.Id,
                        Text = translatedBlockText,
                        X = originalBlock.X,
                        Y = originalBlock.Y,
                        Width = originalBlock.Width,
                        Height = originalBlock.Height,
                        FontSize = originalBlock.FontSize
                    });
                }

                // 在后台线程创建图像，避免阻塞UI线程
                var translatedImage = await Task.Run(() =>
                {
                    // 创建图像的副本，避免跨线程访问问题
                    System.Drawing.Image imageCopy;
                    lock (pageImage)
                    {
                        imageCopy = new Bitmap(pageImage);
                    }
                    return CreateTranslatedImageFromJson(imageCopy, translatedTextBlocks, pageNumber);
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
            System.Drawing.Image? resizedImage = pageImage;
            try
            {
                // 在后台线程压缩图像到800*1000，并转换为Base64
                string base64Image = await Task.Run(() =>
                {
                    // 800*1000（保持宽高比）
                    resizedImage = ResizeImage(pageImage, 800, 1000);
                    return ImageToBase64(resizedImage);
                }).ConfigureAwait(false);


                var requestBody = new
                {
                    model = "Qwen3 VL 8B",
                    messages = new object[]
                    {

                        new
                        {
                            role = "user",
                            content = new object[]
                            {   new
                                {
                                    type = "text",
                                    text = $"翻译图中文字为{targetLanguage}，返回JSON数组，每项包含：original(原文)、translated(译文)、bounding_box(边界框)。格式示例: [{{\"original\":\"\",\"translated\":\"\",\"bounding_box\":[x1,y1,x2,y2]}}]"
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url = $"data:image/png;base64,{base64Image}"
                                    }
                                },

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

                string translatedText = result?.choices?[0]?.message?.content?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(translatedText))
                {
                    return null;
                }

                // 解析视觉模型返回的 JSON（translated + bounding_box）并转换为文本块
                List<TextBlockInfo> translatedTextBlocks;
                try
                {
                    int resizedWidth = resizedImage?.Width ?? pageImage.Width;
                    int resizedHeight = resizedImage?.Height ?? pageImage.Height;
                    translatedTextBlocks = ParseVisionResultToTextBlocks(
                        translatedText,
                        pageNumber,
                        pageImage.Width,
                        pageImage.Height,
                        resizedWidth,
                        resizedHeight,
                        resizedImage);
                }
                catch (Exception ex)
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => UpdateStatus($"视觉翻译解析失败: {ex.Message}")));
                    }
                    else
                    {
                        UpdateStatus($"视觉翻译解析失败: {ex.Message}");
                    }
                    return null;
                }

                if (!translatedTextBlocks.Any())
                {
                    return null;
                }

                // 在后台线程创建图像，根据文本块位置绘制翻译文本
                var translatedImage = await Task.Run(() =>
                {
                    // 创建原始图像的副本，避免跨线程访问问题
                    System.Drawing.Image imageCopy;
                    lock (pageImage)
                    {
                        imageCopy = new Bitmap(pageImage);
                    }
                    // 视觉模式直接按 AI 返回边界框绘制，不依赖原始文本块 ID 映射
                    return CreateTranslatedImageFromDetectedBlocks(imageCopy, translatedTextBlocks, pageNumber);
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

        /// <summary>
        /// 将视觉模型返回的 JSON 数组解析为可绘制文本块。
        /// </summary>
        private List<TextBlockInfo> ParseVisionResultToTextBlocks(
            string modelOutput,
            int pageNumber,
            int originalImageWidth,
            int originalImageHeight,
            int resizedWidth,
            int resizedHeight,
            System.Drawing.Image? debugSourceImage = null)
        {
            var result = new List<TextBlockInfo>();
            var debugRects = new List<RectangleF>();
            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                return result;
            }

            if (pageNumber >= pageInfos.Count)
            {
                return result;
            }

            var pageInfo = pageInfos[pageNumber];
            if (pageInfo.PdfWidth <= 0 || pageInfo.PdfHeight <= 0 || pageInfo.ImageWidth <= 0 || pageInfo.ImageHeight <= 0)
            {
                return result;
            }

            float pdfToImageScaleX = pageInfo.ImageWidth / pageInfo.PdfWidth;
            float pdfToImageScaleY = pageInfo.ImageHeight / pageInfo.PdfHeight;
            if (pdfToImageScaleX <= 0 || pdfToImageScaleY <= 0)
            {
                return result;
            }

            // 兼容模型返回被 ```json 包裹或前后夹杂说明文本的情况
            string jsonText = modelOutput.Trim()
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "");
            int arrayStart = jsonText.IndexOf('[');
            int arrayEnd = jsonText.LastIndexOf(']');
            if (arrayStart >= 0 && arrayEnd > arrayStart)
            {
                jsonText = jsonText.Substring(arrayStart, arrayEnd - arrayStart + 1);
            }

            var items = JsonConvert.DeserializeObject<List<VisionTranslateItem>>(jsonText);
            if (items == null || items.Count == 0)
            {
                return result;
            }
            float resizedToOriginalScaleX = (float)originalImageWidth / Math.Max(1, resizedWidth);
            float resizedToOriginalScaleY = (float)originalImageHeight / Math.Max(1, resizedHeight);

            int id = 0;
            foreach (var item in items)
            {
                if (item == null || item.bounding_box == null)
                {
                    continue;
                }

                string translated = item.translated?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(translated))
                {
                    continue;
                }

        

                // 统一使用比例坐标 -> 像素坐标转换，避免不同返回格式导致偏移
                var pixelBbox = ToPixelCoordinates(item.bounding_box ?? new List<float>(), resizedWidth, resizedHeight);
                if (pixelBbox == null || pixelBbox.Count < 4)
                {
                    continue;
                }

                float x1 = pixelBbox[0];
                float y1 = pixelBbox[1];
                float x2 = pixelBbox[2];
                float y2 = pixelBbox[3];

                // 将视觉返回边界框按中心放大 0.3（宽高各扩大30%）
                const float bboxExpandRatio = 0.3f;
                float boxWidth = x2 - x1;
                float boxHeight = y2 - y1;
                float expandW = boxWidth * bboxExpandRatio / 2f;
                float expandH = boxHeight * bboxExpandRatio / 2f;
                x1 = Math.Max(0, x1 - expandW);
                y1 = Math.Max(0, y1 - expandH);
                x2 = Math.Min(resizedWidth, x2 + expandW);
                y2 = Math.Min(resizedHeight, y2 + expandH);
                debugRects.Add(new RectangleF(x1, y1, Math.Max(1, x2 - x1), Math.Max(1, y2 - y1)));

                // 模型返回坐标基于压缩图，先换算到原图像素坐标
                float imageX = x1 * resizedToOriginalScaleX;
                float imageY = y1 * resizedToOriginalScaleY;
                float imageWidth = (x2 - x1) * resizedToOriginalScaleX;
                float imageHeight = (y2 - y1) * resizedToOriginalScaleY;

                if (imageWidth <= 1 || imageHeight <= 1)
                {
                    continue;
                }

                // 图像坐标 -> PDF 坐标（CreateTranslatedImageFromJson 使用 PDF 坐标）
                float pdfX = imageX / pdfToImageScaleX;
                float pdfWidth = imageWidth / pdfToImageScaleX;
                float pdfHeight = imageHeight / pdfToImageScaleY;
                float pdfY = pageInfo.PdfHeight - ((imageY + imageHeight) / pdfToImageScaleY);

                // 按 bbox 高度估算基础字体大小（像素高度约 75%）
                float estimatedFontSizeInImage = imageHeight * 0.75f;
                float estimatedPdfFontSize = estimatedFontSizeInImage / pdfToImageScaleY;

                result.Add(new TextBlockInfo
                {
                    Id = id++,
                    Text = translated,
                    X = pdfX,
                    Y = pdfY,
                    Width = pdfWidth,
                    Height = pdfHeight,
                    FontSize = 12,
                    FontName = "Microsoft YaHei",
                    IsBold = false
                });
            }

            // 调试：把传给模型的压缩图按返回 bbox 画框并保存到本地
            // if (debugSourceImage != null && debugRects.Count > 0)
            // {
            //     SaveVisionBoundingBoxDebugImage(debugSourceImage, debugRects, pageNumber);
            // }

            return result;
        }

        /// <summary>
        /// 将视觉模型返回的边界框绘制在传输给模型的图像上，便于调试坐标是否准确。
        /// </summary>
        private void SaveVisionBoundingBoxDebugImage(System.Drawing.Image sourceImage, List<RectangleF> rects, int pageNumber)
        {
            try
            {
                string debugDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_vision_bbox");
                Directory.CreateDirectory(debugDir);

                using Bitmap debugBitmap = new Bitmap(sourceImage);
                using Graphics g = Graphics.FromImage(debugBitmap);
                using Pen pen = new Pen(Color.Red, 2f);
                using Font labelFont = new Font("Microsoft YaHei", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
                using Brush labelBrush = new SolidBrush(Color.Red);

                for (int i = 0; i < rects.Count; i++)
                {
                    var rect = rects[i];
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString((i + 1).ToString(), labelFont, labelBrush, rect.X, Math.Max(0, rect.Y - 14));
                }

                string fileName = $"vision_bbox_p{pageNumber + 1}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string filePath = Path.Combine(debugDir, fileName);
                debugBitmap.Save(filePath, ImageFormat.Png);
            }
            catch
            {
                // 调试存图失败不影响主流程
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

        /// <summary>
        /// 根据 AI 检测到的文本块直接绘制翻译结果（不依赖原始文本块映射）。
        /// </summary>
        private System.Drawing.Image CreateTranslatedImageFromDetectedBlocks(System.Drawing.Image originalImage, List<TextBlockInfo> translatedBlocks, int pageIndex)
        {
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
            using Bitmap sourceBitmap = new Bitmap(originalImage);

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                PageInfo? pageInfo = pageIndex < pageInfos.Count ? pageInfos[pageIndex] : null;
                if (pageInfo == null || pageInfo.PdfWidth == 0 || pageInfo.PdfHeight == 0)
                {
                    return translatedBitmap;
                }

 
                float scaleX = pageInfo.ImageWidth / pageInfo.PdfWidth;
                float scaleY = pageInfo.ImageHeight / pageInfo.PdfHeight;

                // 第一步：使用透明色擦除所有原文区域
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                using (Brush transparentBrush = new SolidBrush(Color.Transparent))
                {
                    foreach (var block in translatedBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(block.Text))
                            continue;

                        float imageX = block.X * scaleX;
                        float pdfTopY = pageInfo.PdfHeight - (block.Y + block.Height);
                        float imageY = pdfTopY * scaleY;
                        float imageWidth = block.Width * scaleX;
                        float imageHeight = block.Height * scaleY * 1.3f;

                        if (imageWidth <= 1 || imageHeight <= 1)
                            continue;

                        // 计算删除区域（稍微扩大以确保完全覆盖）
                        RectangleF deleteRect = new RectangleF(
                            Math.Max(0, imageX - 2),
                            Math.Max(0, imageY - 2),
                            Math.Min(originalImage.Width - (imageX - 2), imageWidth + 4),
                            Math.Min(originalImage.Height - (imageY - 2), imageHeight + 4)
                        );

                        // 直接填充透明色
                        g.FillRectangle(transparentBrush, deleteRect);
                    }
                }

                // 恢复合成模式并设置文本渲染质量
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // 第二步：绘制翻译文本
                {
                    foreach (var block in translatedBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(block.Text))
                            continue;

                        float imageX = block.X * scaleX;
                        float pdfTopY = pageInfo.PdfHeight - (block.Y + block.Height);
                        float imageY = pdfTopY * scaleY;
                        float imageWidth = block.Width * scaleX;
                        float imageHeight = block.Height * scaleY * 1.3f;

                        if (imageWidth <= 1 || imageHeight <= 1)
                            continue;

                        float fontSize = Math.Max(10, Math.Min(block.FontSize * scaleY, 72));
                        FontStyle fontStyle = block.IsBold ? FontStyle.Bold : FontStyle.Regular;
                        string text = block.Text.Trim();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        Font font = new Font("Microsoft YaHei", fontSize, fontStyle, GraphicsUnit.Pixel);
                        
                        // 先测量单行文本宽度
                        float singleLineWidth = MeasureLineWidthWithSpacing(g, text, font, fontSpacing);
                        
                        // 判断文本是否需要换行
                        SizeF textSize;
                        if (singleLineWidth > imageWidth)
                        {
                            // 文本宽度超过区域宽度，需要换行
                            textSize = MeasureTextSizeWithSpacing(g, text, font, imageWidth, fontSpacing);
                            
                            // 如果换行后高度超过区域高度，适度缩小字体（但保持最小可读性）
                            while (textSize.Height > imageHeight * 1.2f && fontSize > 10)
                            {
                                fontSize = fontSize * 0.92f;
                                font.Dispose();
                                font = new Font("Microsoft YaHei", fontSize, fontStyle, GraphicsUnit.Pixel);
                                textSize = MeasureTextSizeWithSpacing(g, text, font, imageWidth, fontSpacing);
                            }
                        }
                        else
                        {
                            // 文本可以单行显示，不需要缩小字体
                            textSize = new SizeF(singleLineWidth, font.GetHeight(g));
                        }
                        
                        RectangleF drawRect = new RectangleF(
                            imageX,
                            imageY,
                            imageWidth,
                            imageHeight
                        );

                        // 多行绘制：支持自动换行
                        Color adaptiveTextColor = GetAdaptiveTextColor(sourceBitmap, drawRect);
                        using Brush textBrush = new SolidBrush(adaptiveTextColor);
                        DrawStringWithSpacing(g, text, font, textBrush, drawRect, fontSpacing);
                        
                        font.Dispose();
                    }
                }
            }

            return translatedBitmap;
        }

        /// <summary>
        /// 使用方向感知的条带插值修补，降低渐变背景中的方块感。
        /// </summary>
        private void FillRectWithBoundaryInterpolation(Bitmap targetBitmap, Bitmap sourceBitmap, RectangleF rect)
        {
            int left = Math.Max(0, (int)Math.Floor(rect.Left));
            int top = Math.Max(0, (int)Math.Floor(rect.Top));
            int right = Math.Min(sourceBitmap.Width - 1, (int)Math.Ceiling(rect.Right));
            int bottom = Math.Min(sourceBitmap.Height - 1, (int)Math.Ceiling(rect.Bottom));

            if (right <= left || bottom <= top)
            {
                return;
            }

            int width = Math.Max(1, right - left);
            int height = Math.Max(1, bottom - top);
            int sampleTopY = Math.Max(0, top - 2);
            int sampleBottomY = Math.Min(sourceBitmap.Height - 1, bottom + 2);
            int sampleLeftX = Math.Max(0, left - 2);
            int sampleRightX = Math.Min(sourceBitmap.Width - 1, right + 2);

            var topStrip = new Color[width + 1];
            var bottomStrip = new Color[width + 1];
            var leftStrip = new Color[height + 1];
            var rightStrip = new Color[height + 1];

            for (int x = 0; x <= width; x++)
            {
                int px = left + x;
                topStrip[x] = sourceBitmap.GetPixel(px, sampleTopY);
                bottomStrip[x] = sourceBitmap.GetPixel(px, sampleBottomY);
            }

            for (int y = 0; y <= height; y++)
            {
                int py = top + y;
                leftStrip[y] = sourceBitmap.GetPixel(sampleLeftX, py);
                rightStrip[y] = sourceBitmap.GetPixel(sampleRightX, py);
            }

            // 轻度平滑边界采样，减少细碎噪点与文字边缘带来的伪影
            topStrip = SmoothColorSamples(topStrip, 3);
            bottomStrip = SmoothColorSamples(bottomStrip, 3);
            leftStrip = SmoothColorSamples(leftStrip, 3);
            rightStrip = SmoothColorSamples(rightStrip, 3);

            Color avgTop = AverageColor(topStrip);
            Color avgBottom = AverageColor(bottomStrip);
            Color avgLeft = AverageColor(leftStrip);
            Color avgRight = AverageColor(rightStrip);

            float verticalDelta = ColorDistance(avgTop, avgBottom);
            float horizontalDelta = ColorDistance(avgLeft, avgRight);
            bool preferVertical = verticalDelta >= horizontalDelta;

            const int feather = 3; // 仅在边缘轻微羽化，中心区域完全覆盖以抹除原字

            for (int y = top; y <= bottom; y++)
            {
                float ty = (float)(y - top) / height;
                int yi = y - top;

                for (int x = left; x <= right; x++)
                {
                    float tx = (float)(x - left) / width;
                    int xi = x - left;

                    Color fill = preferVertical
                        ? LerpColor(topStrip[xi], bottomStrip[xi], ty)
                        : LerpColor(leftStrip[yi], rightStrip[yi], tx);

                    int distToEdge = Math.Min(Math.Min(x - left, right - x), Math.Min(y - top, bottom - y));
                    if (distToEdge < feather)
                    {
                        float edgeAlpha = distToEdge / (float)feather;
                        edgeAlpha = edgeAlpha * edgeAlpha * (3f - 2f * edgeAlpha); // smoothstep
                        Color original = sourceBitmap.GetPixel(x, y);
                        fill = LerpColor(original, fill, edgeAlpha);
                    }

                    targetBitmap.SetPixel(x, y, fill);
                }
            }
        }

        private Color[] SmoothColorSamples(Color[] samples, int radius)
        {
            var result = new Color[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                long r = 0, g = 0, b = 0, count = 0;
                int start = Math.Max(0, i - radius);
                int end = Math.Min(samples.Length - 1, i + radius);
                for (int j = start; j <= end; j++)
                {
                    Color c = samples[j];
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    count++;
                }
                result[i] = count == 0
                    ? samples[i]
                    : Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
            }
            return result;
        }

        private Color AverageColor(Color[] colors)
        {
            if (colors.Length == 0)
            {
                return Color.White;
            }

            long r = 0, g = 0, b = 0;
            foreach (var c in colors)
            {
                r += c.R;
                g += c.G;
                b += c.B;
            }
            return Color.FromArgb((int)(r / colors.Length), (int)(g / colors.Length), (int)(b / colors.Length));
        }

        private Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(r, g, bl);
        }

        private float ColorDistance(Color a, Color b)
        {
            float dr = a.R - b.R;
            float dg = a.G - b.G;
            float db = a.B - b.B;
            return MathF.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>
        /// 根据目标文本框背景亮度自适应黑/白字色，避免黑底黑字看不见。
        /// </summary>
        private Color GetAdaptiveTextColor(Bitmap bitmap, RectangleF rect)
        {
            int left = Math.Max(0, (int)Math.Floor(rect.Left));
            int top = Math.Max(0, (int)Math.Floor(rect.Top));
            int right = Math.Min(bitmap.Width - 1, (int)Math.Ceiling(rect.Right));
            int bottom = Math.Min(bitmap.Height - 1, (int)Math.Ceiling(rect.Bottom));
            if (right <= left || bottom <= top)
            {
                return Color.Black;
            }

            // 中央区域采样，降低边框线和噪点干扰
            int sx = left + (right - left) / 4;
            int sy = top + (bottom - top) / 4;
            int ex = left + (right - left) * 3 / 4;
            int ey = top + (bottom - top) * 3 / 4;
            if (ex <= sx || ey <= sy)
            {
                sx = left; sy = top; ex = right; ey = bottom;
            }

            long lum = 0;
            long cnt = 0;
            for (int y = sy; y <= ey; y += 2)
            {
                for (int x = sx; x <= ex; x += 2)
                {
                    Color c = bitmap.GetPixel(x, y);
                    lum += (long)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    cnt++;
                }
            }

            if (cnt == 0) return Color.Black;
            long avg = lum / cnt;
            return avg < 125 ? Color.White : Color.Black;
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


        // 根据JSON文本块信息创建翻译图像，保留原始图片和文本位置
        private System.Drawing.Image CreateTranslatedImageFromJson(System.Drawing.Image originalImage, List<TextBlockInfo> translatedBlocks, int pageIndex)
        {
            // 创建与原始图像相同大小的位图
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置高质量渲染（优化文本清晰度）
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.HighQuality;
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
                List<PdfImageRegion>? currentPageImageRegions = pageIndex < pageImageRegions.Count ? pageImageRegions[pageIndex] : null;

                // 计算坐标转换比例（PDF点 -> 图像像素）
                float scaleX = pageInfo.ImageWidth / pageInfo.PdfWidth;
                float scaleY = pageInfo.ImageHeight / pageInfo.PdfHeight;

                // 创建ID到翻译文本块的映射
                var translatedDict = translatedBlocks.ToDictionary(tb => tb.Id, tb => tb);

                // 获取原始文本块信息
                List<TextBlockInfo> originalBlocks = pageIndex < pageTextBlocks.Count ? pageTextBlocks[pageIndex] : new List<TextBlockInfo>();

                // 单次遍历：每个块先删除原文，再绘制译文
                using (Brush transparentBrush = new SolidBrush(Color.Transparent))
                using (Brush textBrush = new SolidBrush(Color.Black))
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
                        float imageHeight = originalBlock.Height * scaleY * 1.3f;

                        // 第一步：先删除原文区域（透明填充，且排除图片区域）
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        if (originalBlock.Lines != null && originalBlock.Lines.Count > 0)
                        {
                            // 图文/多行场景：按每行删除，避免按整块删除误伤图片区域
                            foreach (var lineInfo in originalBlock.Lines)
                            {
                                float lineImageX = (float)(lineInfo.Left * scaleX);
                                float linePdfTopY = pageInfo.PdfHeight - ((float)lineInfo.Y + (float)lineInfo.Height);
                                float lineImageY = linePdfTopY * scaleY;
                                float lineWidth = (float)(lineInfo.Width * scaleX);
                                float lineHeight = (float)(lineInfo.Height * scaleY) * 1.3f;

                                RectangleF deleteRect = new RectangleF(
                                    Math.Max(0, lineImageX - 2),
                                    Math.Max(0, lineImageY - 2),
                                    Math.Min(originalImage.Width - (lineImageX - 2), lineWidth + 4),
                                    Math.Min(originalImage.Height - (lineImageY - 2), lineHeight + 4)
                                );
                                FillTransparentRectExcludingImages(
                                    g,
                                    transparentBrush,
                                    deleteRect,
                                    currentPageImageRegions,
                                    pageInfo,
                                    scaleX,
                                    scaleY);
                            }
                        }
                        else
                        {
                            // 普通段落：无行信息时按整块删除
                            RectangleF deleteRect = new RectangleF(
                                Math.Max(0, imageX - 2),
                                Math.Max(0, imageY - 2),
                                Math.Min(originalImage.Width - (imageX - 2), imageWidth + 4),
                                Math.Min(originalImage.Height - (imageY - 2), imageHeight + 4)
                            );
                            FillTransparentRectExcludingImages(
                                g,
                                transparentBrush,
                                deleteRect,
                                currentPageImageRegions,
                                pageInfo,
                                scaleX,
                                scaleY);
                        }

                        // 查找翻译文本，若为空则只保留已删除效果
                        if (!translatedDict.TryGetValue(originalBlock.Id, out var translatedBlock) ||
                            string.IsNullOrWhiteSpace(translatedBlock.Text))
                        {
                            continue;
                        }

                        // 第二步：在原位置绘制翻译文本
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                        // 智能计算字体大小（沿用原文块字体高度）
                        float fontSize;

                        // 普通/长文本：使用原始字体大小
                        fontSize = originalBlock.FontSize * scaleY;
                        fontSize = Math.Max(10, Math.Min(fontSize, 72)); // 最小10px

                        // 根据原始字体是否加粗，决定字体样式
                        FontStyle fontStyle = originalBlock.IsBold ? FontStyle.Bold : FontStyle.Regular;

                        // 使用中文字体
                        Font font = new Font("Microsoft YaHei", fontSize, fontStyle, GraphicsUnit.Pixel);

                        // 先按“实际字符间距”测量单行文本宽度，避免低估导致截断
                        float singleLineWidth = MeasureLineWidthWithSpacing(g, translatedBlock.Text, font, fontSpacing);
                        // 判断文本是否需要换行
                        SizeF textSize;
                        if (singleLineWidth > imageWidth)
                        {
                            // 文本宽度超过区域宽度，需要换行
                            textSize = MeasureTextSizeWithSpacing(g, translatedBlock.Text, font, imageWidth, fontSpacing);
                            
                            // 如果换行后高度超过区域高度，缩小字体（但不要太小）
                            while (textSize.Height > imageHeight * 1.2f && fontSize > 12)
                            {
                                fontSize = fontSize * 0.9f;
                                font.Dispose();
                                font = new Font("Microsoft YaHei", fontSize, fontStyle, GraphicsUnit.Pixel);
                                textSize = MeasureTextSizeWithSpacing(g, translatedBlock.Text, font, imageWidth, fontSpacing);
                            }
                        }
                        else
                        {
                            // 文本可以单行显示，不需要缩小字体
                            textSize = new SizeF(singleLineWidth, font.GetHeight(g));
                        }

                        font.Dispose();
                        font = new Font("Microsoft YaHei", fontSize, fontStyle, GraphicsUnit.Pixel);

                        // 绘制翻译文本（智能判断是否需要按行宽度绘制）
                        bool needLineWidthDraw = IsImageTextMixed(
                            originalBlock.X,
                            originalBlock.X + originalBlock.Width,
                            originalBlock.Y,
                            originalBlock.Y + originalBlock.Height,
                            originalBlock.Lines,
                            currentPageImageRegions);
                        
                        if (needLineWidthDraw && originalBlock.Lines != null)
                        {
                            // 使用行信息进行精确绘制（图文混排场景）
                            DrawTextWithLineWidths(g, translatedBlock.Text, font, textBrush, 
                                imageX, imageY, imageHeight, originalBlock.Lines, scaleX, scaleY, pageInfo, fontSpacing);
                        }
                        else
                        {
                            // 使用整体宽度绘制（普通文本段落场景）
                            RectangleF drawRect = new RectangleF(
                                imageX,
                                imageY,
                                imageWidth,
                                Math.Max(imageHeight, textSize.Height)
                            );
                            DrawStringWithSpacing(g, translatedBlock.Text, font, textBrush, drawRect, fontSpacing);
                        }

                        font.Dispose();
                    }
                }
            }

            return translatedBitmap;
        }

        /// <summary>
        /// 透明擦除文本区域时排除图片边界，避免误删图片像素。
        /// </summary>
        private void FillTransparentRectExcludingImages(
            Graphics g,
            Brush transparentBrush,
            RectangleF deleteRect,
            List<PdfImageRegion>? imageRegions,
            PageInfo pageInfo,
            float scaleX,
            float scaleY)
        {
            if (deleteRect.Width <= 0 || deleteRect.Height <= 0)
            {
                return;
            }

            if (imageRegions == null || imageRegions.Count == 0)
            {
                g.FillRectangle(transparentBrush, deleteRect);
                return;
            }

            using (Region remainingRegion = new Region(deleteRect))
            {
                foreach (var image in imageRegions)
                {
                    RectangleF imageRect = new RectangleF(
                        (float)(image.Left * scaleX),
                        (float)((pageInfo.PdfHeight - image.Top) * scaleY),
                        (float)(image.Width * scaleX),
                        (float)(image.Height * scaleY));

                    if (imageRect.Width <= 0 || imageRect.Height <= 0)
                    {
                        continue;
                    }

                    remainingRegion.Exclude(imageRect);
                }

                g.FillRegion(transparentBrush, remainingRegion);
            }
        }

        /// <summary>
        /// 使用每行的宽度限制绘制文本（避免覆盖右侧图片）
        /// </summary>
        private void DrawTextWithLineWidths(Graphics g, string text, Font font, Brush brush, 
            float startX, float startY, float maxHeight, List<LineInfo> lineInfos, 
            float scaleX, float scaleY, PageInfo pageInfo, float charSpacing = 0f)
        {
            if (string.IsNullOrWhiteSpace(text) || !lineInfos.Any())
                return;

            StringFormat sf = StringFormat.GenericTypographic;
            sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            sf.Trimming = StringTrimming.Word;

            float fontLineHeight = font.GetHeight(g);

            // 按行绘制文本
            string remainingText = text;
            
            foreach (var lineInfo in lineInfos)
            {
                if (string.IsNullOrWhiteSpace(remainingText))
                    break;

                // 计算该行的图像Y坐标（使用行的绝对Y坐标）
                float lineImageX = (float)(lineInfo.Left * scaleX);
                float linePdfTopY = pageInfo.PdfHeight - ((float)lineInfo.Y + (float)lineInfo.Height);
                float lineImageY = linePdfTopY * scaleY;
                
                if (lineImageY + fontLineHeight > startY + maxHeight)
                    break;  // 超出高度限制

                // 计算当前行的宽度限制（使用原始行的宽度）
                float lineWidth = (float)(lineInfo.Width * scaleX);

                // 测量可以在这个宽度内绘制多少文本
                string lineText = FitTextToWidth(g, remainingText, font, lineWidth, sf);

                if (!string.IsNullOrEmpty(lineText))
                {
                    // 绘制当前行（使用该行的绝对Y坐标）
                    DrawLineWithSpacing(g, lineText, font, brush, lineImageX, lineImageY, charSpacing, sf);
                    
                    // 移除已绘制的文本
                    remainingText = remainingText.Substring(lineText.Length).TrimStart();
                }
            }

            // 如果还有剩余文本，继续用最后一行的宽度和位置绘制
            if (!string.IsNullOrWhiteSpace(remainingText) && lineInfos.Any())
            {
                var lastLineInfo = lineInfos.Last();
                float lineWidth = (float)(lastLineInfo.Width * scaleX);
                float actualLineHeight = (float)(lastLineInfo.Height * scaleY);
                float lineX = (float)(lastLineInfo.Left * scaleX);
                
                // 从最后一行的位置继续
                float linePdfTopY = pageInfo.PdfHeight - ((float)lastLineInfo.Y + (float)lastLineInfo.Height);
                float currentY = linePdfTopY * scaleY + actualLineHeight;
                
                while (!string.IsNullOrWhiteSpace(remainingText) && currentY + fontLineHeight <= startY + maxHeight)
                {
                    string lineText = FitTextToWidth(g, remainingText, font, lineWidth, sf);
                    if (string.IsNullOrEmpty(lineText))
                        break;

                    DrawLineWithSpacing(g, lineText, font, brush, lineX, currentY, charSpacing, sf);
                    remainingText = remainingText.Substring(lineText.Length).TrimStart();
                    currentY += actualLineHeight;
                }
            }
        }

        /// <summary>
        /// 计算在指定宽度内能容纳的文本
        /// </summary>
        private string FitTextToWidth(Graphics g, string text, Font font, float maxWidth, StringFormat sf)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // 测量整个文本
            SizeF fullSize = g.MeasureString(text, font, 10000, sf);
            if (fullSize.Width <= maxWidth)
                return text;  // 整个文本都能放下

            // 二分查找能放下的最大字符数
            int left = 0;
            int right = text.Length;
            int bestFit = 0;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                string substring = text.Substring(0, mid);
                SizeF size = g.MeasureString(substring, font, 10000, sf);

                if (size.Width <= maxWidth)
                {
                    bestFit = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // 在单词边界断行（避免切断单词）
            if (bestFit > 0 && bestFit < text.Length)
            {
                // 往回找最近的空格
                int spaceIndex = text.LastIndexOf(' ', bestFit - 1);
                if (spaceIndex > 0 && spaceIndex > bestFit * 0.7) // 不要回退太多
                {
                    bestFit = spaceIndex + 1;
                }
            }

            return bestFit > 0 ? text.Substring(0, bestFit) : "";
        }

        /// <summary>
        /// 测量单行文本宽度（考虑字符间距）。
        /// </summary>
        private float MeasureLineWidthWithSpacing(Graphics g, string text, Font font, float charSpacing)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            if (charSpacing <= 0)
            {
                return g.MeasureString(text, font).Width;
            }

            StringFormat sf = StringFormat.GenericTypographic;
            sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

            float width = 0f;
            foreach (char c in text)
            {
                if (c == '\n' || c == '\r')
                {
                    break;
                }

                SizeF charSize = g.MeasureString(c.ToString(), font, 10000, sf);
                width += charSize.Width + charSpacing;
            }

            return width;
        }

        /// <summary>
        /// 测量文本在指定宽度下的占用尺寸（考虑字符间距与换行）。
        /// </summary>
        private SizeF MeasureTextSizeWithSpacing(Graphics g, string text, Font font, float maxWidth, float charSpacing)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return SizeF.Empty;
            }

            if (charSpacing <= 0)
            {
                return g.MeasureString(text, font, (int)Math.Max(1, maxWidth));
            }

            StringFormat sf = StringFormat.GenericTypographic;
            sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

            float lineHeight = font.GetHeight(g);
            float currentLineWidth = 0f;
            float maxLineWidthUsed = 0f;
            int lineCount = 1;

            foreach (char c in text)
            {
                if (c == '\n' || c == '\r')
                {
                    maxLineWidthUsed = Math.Max(maxLineWidthUsed, currentLineWidth);
                    currentLineWidth = 0f;
                    lineCount++;
                    continue;
                }

                SizeF charSize = g.MeasureString(c.ToString(), font, 10000, sf);
                float charWidth = charSize.Width + charSpacing;

                if (currentLineWidth + charWidth > maxWidth && currentLineWidth > 0f)
                {
                    maxLineWidthUsed = Math.Max(maxLineWidthUsed, currentLineWidth);
                    currentLineWidth = charWidth;
                    lineCount++;
                }
                else
                {
                    currentLineWidth += charWidth;
                }
            }

            maxLineWidthUsed = Math.Max(maxLineWidthUsed, currentLineWidth);
            return new SizeF(maxLineWidthUsed, lineCount * lineHeight);
        }

        // 绘制带字符间距的文本
        private void DrawStringWithSpacing(Graphics g, string text, Font font, Brush brush, RectangleF rect, float charSpacing = 0f)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // 如果字符间距为0，使用默认绘制
            if (charSpacing <= 0)
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word,
                    FormatFlags = StringFormatFlags.LineLimit
                };
                g.DrawString(text, font, brush, rect, format);
                return;
            }

            // 使用字符间距绘制
            StringFormat sf = StringFormat.GenericTypographic;
            sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

            float x = rect.X;
            float y = rect.Y;
            float maxWidth = rect.Width;
            float lineHeight = font.GetHeight(g);

            string currentLine = "";
            float currentLineWidth = 0;

            foreach (char c in text)
            {
                // 处理换行符
                if (c == '\n' || c == '\r')
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        DrawLineWithSpacing(g, currentLine, font, brush, x, y, charSpacing, sf);
                        y += lineHeight;
                        currentLine = "";
                        currentLineWidth = 0;
                    }
                    continue;
                }

                string ch = c.ToString();
                SizeF charSize = g.MeasureString(ch, font, 10000, sf);
                float charWidth = charSize.Width + charSpacing;

                // 如果加上这个字符会超出宽度，先绘制当前行
                if (currentLineWidth + charWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    DrawLineWithSpacing(g, currentLine, font, brush, x, y, charSpacing, sf);
                    y += lineHeight;
                    currentLine = ch;
                    currentLineWidth = charWidth;

                    // 检查是否超出矩形高度
                    if (y + lineHeight > rect.Bottom)
                        break;
                }
                else
                {
                    currentLine += ch;
                    currentLineWidth += charWidth;
                }
            }

            // 绘制最后一行
            if (!string.IsNullOrEmpty(currentLine) && y + lineHeight <= rect.Bottom)
            {
                DrawLineWithSpacing(g, currentLine, font, brush, x, y, charSpacing, sf);
            }
        }

        // 绘制单行带字符间距的文本
        private void DrawLineWithSpacing(Graphics g, string line, Font font, Brush brush, float x, float y, float charSpacing, StringFormat sf)
        {
            float currentX = x;
            foreach (char c in line)
            {
                string ch = c.ToString();
                g.DrawString(ch, font, brush, currentX, y, sf);
                SizeF charSize = g.MeasureString(ch, font, 10000, sf);
                currentX += charSize.Width + charSpacing;
            }
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
        /// <summary>
        /// 转换为像素坐标
        /// </summary>
        public List<float>? ToPixelCoordinates(List<float> bbox, int imageWidth, int imageHeight)
        {
            if (bbox == null || bbox.Count < 4)
                return null;

            int x1 = (int)(bbox[0] / 1000.0 * imageWidth);
            int y1 = (int)(bbox[1] / 1000.0 * imageHeight);
            int x2 = (int)(bbox[2] / 1000.0 * imageWidth);
            int y2 = (int)(bbox[3] / 1000.0 * imageHeight);
            // 确保 x1 < x2 和 y1 < y2
            if (x1 > x2) (x1, x2) = (x2, x1);
            if (y1 > y2) (y1, y2) = (y2, y1);

            return new List<float> { x1, y1, x2, y2 };
        }


        // 根据视觉翻译返回的边界框位置创建翻译图像
        private System.Drawing.Image CreateTranslatedImageFromVision(System.Drawing.Image originalImage, List<VisionTextBlock> visionBlocks, int resizedWidth, int resizedHeight)
        {
            // 创建与原始图像相同大小的位图
            Bitmap translatedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            translatedBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (Graphics g = Graphics.FromImage(translatedBitmap))
            {
                // 设置高质量渲染（优化文本清晰度）
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // 首先绘制原始图像（保留所有图片和布局）
                g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                // 计算缩放比例（压缩后的图像 -> 原始图像）
                float scaleX = (float)originalImage.Width / resizedWidth;
                float scaleY = (float)originalImage.Height / resizedHeight;

                // 设置文本渲染质量（确保文本清晰）
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // 一次循环完成删除原文和绘制译文
                using (Brush transparentBrush = new SolidBrush(Color.Transparent))
                using (Brush textBrush = new SolidBrush(Color.Black))
                {
                    foreach (var visionBlock in visionBlocks)
                    {
                        if (string.IsNullOrWhiteSpace(visionBlock.text))
                            continue;

                        // 将归一化坐标转换为像素坐标
                        List<float> pixelBbox = ToPixelCoordinates(visionBlock.bbox, resizedWidth, resizedHeight);
                        if (pixelBbox == null || pixelBbox.Count < 4)
                            continue;

                        // pixelBbox 格式: [x1, y1, x2, y2]，转换为原始图像坐标
                        float originalX = pixelBbox[0] * scaleX;
                        float originalY = pixelBbox[1] * scaleY;
                        float originalWidth = (pixelBbox[2] - pixelBbox[0]) * scaleX * 1.02f;  // 增加2%
                        float originalHeight = (pixelBbox[3] - pixelBbox[1]) * scaleY * 1.02f;  // 增加2%

                        // 步骤1：删除原始文本（填充透明色）
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        RectangleF deleteRect = new RectangleF(
                            Math.Max(0, originalX - 2),
                            Math.Max(0, originalY - 2),
                            Math.Min(originalImage.Width - (originalX - 2), originalWidth + 4),
                            Math.Min(originalImage.Height - (originalY - 2), originalHeight + 4)
                        );
                        g.FillRectangle(transparentBrush, deleteRect);

                        // 步骤2：绘制翻译文本
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                        // 根据边界框高度估算字体大小
                        float fontSize = originalHeight * 0.8f;
                        fontSize = Math.Max(6, Math.Min(fontSize, 72)); // 限制字体大小范围

                        // 使用中文字体
                        Font font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

                        // 先测量单行文本的实际宽度（不限制宽度）
                        SizeF singleLineSize = g.MeasureString(visionBlock.text, font);
                        
                        // 判断文本是否需要换行
                        SizeF textSize;
                        if (singleLineSize.Width > originalWidth)
                        {
                            // 文本宽度超过区域宽度，需要换行
                            textSize = g.MeasureString(visionBlock.text, font, (int)originalWidth);
                            
                            // 如果换行后高度超过区域高度，缩小字体
                            while (textSize.Height > originalHeight * 1.2f && fontSize > 6)
                            {
                                fontSize = fontSize * 0.9f;
                                font.Dispose();
                                font = new Font("Microsoft YaHei", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                                textSize = g.MeasureString(visionBlock.text, font, (int)originalWidth);
                            }
                        }
                        else
                        {
                            // 文本可以单行显示，不需要缩小字体
                            textSize = singleLineSize;
                        }

                        // 绘制翻译文本
                        RectangleF drawRect = new RectangleF(
                            originalX,
                            originalY,
                            originalWidth,
                            Math.Max(originalHeight, textSize.Height)
                        );

                        DrawStringWithSpacing(g, visionBlock.text, font, textBrush, drawRect, 0.5f);

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
                        int processedCount = 0;

                        // 按页码顺序遍历字典
                        foreach (var kvp in translatedPages.OrderBy(x => x.Key))
                        {
                            int pageIndex = kvp.Key;
                            var pagePath = kvp.Value;

                            if (string.IsNullOrWhiteSpace(pagePath) || !File.Exists(pagePath))
                                continue;

                            processedCount++;
                            PdfSharpCore.Pdf.PdfPage page = document.AddPage();

                            // 使用原始页面的尺寸信息
                            PageInfo? pageInfo = pageIndex < pageInfos.Count ? pageInfos[pageIndex] : null;
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
                            if (processedCount % 10 == 0 || processedCount == totalPages)
                            {
                                int progress = (int)(processedCount * 100.0 / totalPages);
                                if (InvokeRequired)
                                {
                                    BeginInvoke(new Action(() =>
                                    {
                                        UpdateStatus($"正在保存 PDF... ({processedCount} / {totalPages})");
                                        progressBar.Value = Math.Min(progress, 100);
                                    }));
                                }
                                else
                                {
                                    UpdateStatus($"正在保存 PDF... ({processedCount} / {totalPages})");
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

        [JsonProperty("isBold")]
        public bool IsBold { get; set; } = false;

        /// <summary>
        /// 每行的详细信息（用于图文混排时精确绘制，避免覆盖图片）
        /// </summary>
        [JsonProperty("lines")]
        public List<LineInfo> Lines { get; set; } = new List<LineInfo>();
    }

    // PDF页面尺寸信息
    public class PageInfo
    {
        public float PdfWidth { get; set; }
        public float PdfHeight { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
    }

    /// <summary>
    /// PDF 页面中的图片边界框（PDF坐标系）
    /// </summary>
    public class PdfImageRegion
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public double Top { get; set; }
        public double Width => Right - Left;
        public double Height => Top - Bottom;
    }

    // 视觉翻译返回的文本块信息（包含边界框位置）
    public class VisionTextBlock
    {
        [JsonProperty("text")]
        public string text { get; set; } = "";
        /// <summary>
        ///  [50, 733, 937, 878]
        /// </summary>
        [JsonProperty("bbox")]
        public List<float> bbox { get; set; } = new List<float>();
    }

    /// <summary>
    /// 视觉模型翻译返回的结构化项。
    /// </summary>
    public class VisionTranslateItem
    {
        [JsonProperty("original")]
        public string original { get; set; } = "";

        [JsonProperty("translated")]
        public string translated { get; set; } = "";
        /// <summary>
        /// x1,y1,x2,y2 边界框坐标
        /// </summary>

        [JsonProperty("bounding_box")]
        public List<float> bounding_box { get; set; } = new List<float>();
    }

    /// <summary>
    /// 段落信息（包含合并后的文本和位置）
    /// </summary>
    public class ParagraphInfo
    {
        /// <summary>
        /// 段落文本（已合并所有单词）
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 段落左边界X坐标
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 段落底部Y坐标
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 段落宽度（最大行宽）
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 段落高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 段落平均字体大小
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// 字体名称
        /// </summary>
        public string FontName { get; set; } = "Arial";

        /// <summary>
        /// 是否加粗
        /// </summary>
        public bool IsBold { get; set; } = false;

        /// <summary>
        /// 每行的详细信息（用于图文混排时精确绘制）
        /// </summary>
        public List<LineInfo> Lines { get; set; } = new List<LineInfo>();
    }

    /// <summary>
    /// 行信息
    /// </summary>
    public class LineInfo
    {
        public double Y { get; set; }  // 行的Y坐标
        public double Left { get; set; }  // 行左边界
        public double Right { get; set; }  // 行右边界
        public double Width { get; set; }  // 行的实际宽度（避免覆盖图片）
        public double Height { get; set; }  // 行高
    }
}

