# PDF 翻译工具 - Qwen3-VL-8B

这是一个基于 Windows Forms 的 PDF 翻译工具，使用 Qwen3-VL-8B 视觉语言模型通过 llama.cpp 进行 PDF 内容的智能翻译。

## 功能特点

- ✅ **高清预览**: 2000px 高分辨率渲染，文字图像清晰锐利 ⭐ NEW
- ✅ **现代化UI**: ReaLTaiizor Metro 主题，专业美观
- ✅ **PDF 预览**: 加载并浏览 PDF 文件的每一页
- ✅ **智能翻译**: 使用 Qwen3-VL-8B 模型识别并翻译 PDF 内容
- ✅ **实时预览**: 翻译过程中实时查看翻译结果
- ✅ **双面板对比**: 左右对比原文和译文
- ✅ **保持格式**: 尽可能保持原有文档的布局和格式
- ✅ **批量处理**: 自动翻译整个 PDF 文档的所有页面
- ✅ **高性能**: 基于 Google PDFium，快速稳定
- ✅ **导出功能**: 将翻译后的内容保存为新的 PDF 文件

## 系统要求

- Windows 10/11
- .NET 8.0 或更高版本
- llama.cpp server 运行中（端口 8033）
- Qwen3-VL-8B 模型

## 使用前准备

### 1. 启动 llama.cpp server

在使用本工具之前，请确保 llama.cpp server 已经启动。使用以下命令启动服务器：

```bash
llama-server --jinja -c 32768 -ngl 50 -b 4096 -ub 2048 -fa on -t 16 -tb 16 --mlock --port 8033 -m D:\work\API\SF.Laundry.Solution\LLamaSharp\LLama.Examples\Assets\Qwen3-VL-8B\Qwen3VL-8B-Instruct-Q4_K_M.gguf --alias "Qwen3 VL 8B" --mmproj D:\work\API\SF.Laundry.Solution\LLamaSharp\LLama.Examples\Assets\Qwen3-VL-8B\mmproj-Qwen3VL-8B-Instruct-F16.gguf --mmproj-offload --host 127.0.0.1 --image-max-tokens 1024 --image-min-tokens 512

llama-server --jinja -ngl 40 -b 4096 -ub 2048 -fa on -t 20 -tb 16 --mlock --port 8033 -m D:\work\API\SF.Laundry.Solution\LLamaSharp\LLama.Examples\Assets\Qwen3-VL-8B\Qwen3VL-8B-Instruct-Q4_K_M.gguf --alias "Qwen3 VL 8B" --mmproj D:\work\API\SF.Laundry.Solution\LLamaSharp\LLama.Examples\Assets\Qwen3-VL-8B\mmproj-Qwen3VL-8B-Instruct-F16.gguf --mmproj-offload --host 127.0.0.1 --image-max-tokens 1024 --image-min-tokens 512

```

确保服务器在 `http://127.0.0.1:8033` 上运行。

### 2. 安装依赖

项目使用以下 NuGet 包（**全部免费且可商业化使用**）：

- `Docnet.Core` (MIT) - PDF 渲染引擎（基于 Google PDFium）⭐ NEW
- `PdfSharpCore` (MIT) - PDF 创建和处理
- `Newtonsoft.Json` (MIT) - JSON 序列化
- `ReaLTaiizor` (MIT) - 现代化 UI 控件库

这些包会在构建时自动安装。所有依赖库都采用宽松的开源许可证，**完全支持 .NET 8**，可以**免费用于商业项目**。

## 使用方法

1. **启动应用程序**
   - 运行 `PdfTranslate.exe`

2. **加载 PDF 文件**
   - 点击"选择 PDF 文件"按钮
   - 选择要翻译的 PDF 文件
   - 文件加载后会显示第一页预览

3. **浏览 PDF 页面**
   - 使用"上一页"和"下一页"按钮浏览文档
   - 左侧显示原始 PDF 页面

4. **开始翻译**
   - 点击"开始翻译"按钮
   - 工具会逐页翻译 PDF 内容
   - 右侧实时显示翻译后的页面
   - 进度条显示翻译进度

5. **保存翻译结果**
   - 翻译完成后，点击"保存翻译 PDF"按钮
   - 选择保存位置和文件名
   - 翻译后的 PDF 将被保存

## 界面说明

### 专业 ReaLTaiizor UI ⭐ NEW
本工具采用**ReaLTaiizor**专业UI库，Metro主题设计：
- 🎨 **MetroButton** - 现代化扁平按钮
- ✨ **MetroLabel** - 简洁文本标签
- 🎯 **MetroProgressBar** - 平滑进度条
- 💡 **HeaderLabel** - 醒目标题
- 🖱️ **流畅动画** - 内置过渡效果
- 🎭 **多主题支持** - Light/Dark/Custom

### 自定义标题栏 ⭐ NEW
- **深色标题栏**: 专业的深灰黑背景 (#1E1E1E)
- **窗口控制**: 最小化 (─) | 最大化 (□) | 关闭 (✕)
- **可拖动**: 点击标题栏任意位置拖动窗口
- **标题**: 🌐 PDF 智能翻译工具 - Qwen3-VL-8B

### 操作区
- **📁 选择 PDF 文件**: 加载要翻译的 PDF（蓝色按钮）

### 左侧面板 - 📄 原始文档
- **◀ 上一页**: 查看上一页（蓝色按钮）
- **下一页 ▶**: 查看下一页（蓝色按钮）
- **页面信息**: 显示当前页码 / 总页数
- **预览区**: 高质量显示原始 PDF 页面

### 右侧面板 - ✨ 翻译结果
- **🚀 开始翻译**: 开始翻译整个 PDF 文档（橙色按钮）
- **💾 保存 PDF**: 将翻译结果保存为新文件（绿色按钮）
- **已翻译页面**: 显示翻译进度
- **预览区**: 实时显示翻译后的页面内容

### 底部状态栏
- **✓ 状态信息**: 显示当前操作状态（带 emoji 提示）
- **进度条**: 可视化显示翻译进度

详细的技术文档请查看：
- **[Docnet.Core使用说明.md](Docnet.Core使用说明.md)** - PDF 渲染库使用指南 ⭐ NEW
- **[ReaLTaiizor使用说明.md](ReaLTaiizor使用说明.md)** - UI 库使用指南
- [现代化UI设计v2.md](现代化UI设计v2.md) - v2.0 设计文档
- [UI设计说明.md](UI设计说明.md) - v1.2 设计文档
- [UI更新日志.md](UI更新日志.md) - 更新历史

## 技术架构

### 核心技术
- **Windows Forms**: UI 框架
- **ReaLTaiizor** (MIT): 现代化 UI 控件库
- **Docnet.Core** (MIT): PDF 渲染引擎（基于 Google PDFium）⭐ NEW
- **PdfSharpCore** (MIT): PDF 创建和处理
- **HttpClient**: 与 llama.cpp API 通信

### 翻译流程
1. 将 PDF 页面渲染为高分辨率图像（300 DPI）
2. 将图像转换为 Base64 编码
3. 发送到 Qwen3-VL-8B 模型进行视觉识别和翻译
4. 接收翻译后的文本
5. 创建新的页面图像，保持原有布局
6. 将所有翻译页面组合成新的 PDF 文件

## API 配置

默认 API 端点: `http://127.0.0.1:8033/v1/chat/completions`

如需修改 API 端点或其他配置，请在 `Form1.cs` 中修改以下常量：

```csharp
private const string LLAMA_API_URL = "http://127.0.0.1:8033/v1/chat/completions";
```

## 注意事项

1. **模型加载**: 确保 Qwen3-VL-8B 模型已正确加载到 llama.cpp server
2. **内存使用**: 翻译大型 PDF 文件会占用较多内存
3. **翻译时间**: 每页翻译时间取决于页面复杂度和模型性能
4. **格式保持**: 当前版本将内容转换为文本格式，复杂布局可能需要手动调整
5. **网络连接**: 确保本地 llama.cpp server 可访问

## 未来改进

- [ ] 支持保持更复杂的 PDF 格式（表格、多列布局等）
- [ ] 支持 OCR 识别扫描版 PDF
- [ ] 支持批量翻译多个 PDF 文件
- [ ] 添加翻译质量评估
- [ ] 支持多种翻译语言选择
- [ ] 优化内存使用和性能

## 故障排除

### 问题：无法连接到 llama.cpp server
**解决方案**: 
- 检查 llama.cpp server 是否正在运行
- 验证端口 8033 是否正确
- 检查防火墙设置

### 问题：翻译速度很慢
**解决方案**:
- 确保使用 GPU 加速（-ngl 参数）
- 减少 PDF 渲染分辨率
- 检查系统资源使用情况

### 问题：翻译质量不佳
**解决方案**:
- 调整 prompt 提示词
- 增加 max_tokens 参数
- 使用更高质量的模型

## 许可证

本项目采用 MIT 许可证，**完全免费且可用于商业项目**。

### 依赖库许可证
- **Docnet.Core**: MIT License ✅ 可商业使用 ✅ 支持 .NET 8 ⭐ NEW
- **ReaLTaiizor**: MIT License ✅ 可商业使用 ✅ 支持 .NET 8
- **PdfSharpCore**: MIT License ✅ 可商业使用
- **Newtonsoft.Json**: MIT License ✅ 可商业使用

所有依赖库都采用宽松的开源许可证，**完全支持 .NET 8 及以上版本**，您可以放心地将本工具用于商业项目，无需支付任何许可费用。

## 联系方式

如有问题或建议，请提交 Issue。

