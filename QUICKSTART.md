# 快速开始指南

## 5 分钟快速上手

### 第 1 步：准备环境

1. **确保已安装 .NET 8.0**
   ```bash
   dotnet --version
   ```
   如果未安装，请从 [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) 下载

2. **启动 llama.cpp server**
   ```bash
   # 在 llama.cpp 目录下执行
   llama-server --jinja -c 32768 -ngl 50 -b 4096 -ub 2048 -fa on -t 16 -tb 16 --mlock --port 8033 -m "你的模型路径/Qwen3VL-8B-Instruct-Q4_K_M.gguf" --alias "Qwen3 VL 8B" --mmproj "你的模型路径/mmproj-Qwen3VL-8B-Instruct-F16.gguf" --mmproj-offload --host 127.0.0.1 --image-max-tokens 1024 --image-min-tokens 512
   ```

3. **测试 API 是否运行**
   
   打开浏览器访问: `http://127.0.0.1:8033`
   
   如果看到 llama.cpp 的界面，说明服务器运行成功 ✅

### 第 2 步：编译项目

```bash
cd D:\work\APP\FreePdfTranslate\PdfTranslate
dotnet restore
dotnet build
```

### 第 3 步：运行程序

```bash
dotnet run
```

或者直接运行编译后的 exe 文件：
```
.\bin\Debug\net8.0-windows\PdfTranslate.exe
```

### 第 4 步：翻译 PDF

1. 点击 **"选择 PDF 文件"** 按钮
2. 选择要翻译的 PDF 文件
3. 浏览 PDF 内容（可使用上一页/下一页按钮）
4. 点击 **"开始翻译"** 按钮
5. 等待翻译完成（可在右侧实时查看翻译结果）
6. 点击 **"保存翻译 PDF"** 按钮保存结果

## 界面说明

```
┌─────────────────────────────────────────────────────────────┐
│  PDF 翻译工具 - Qwen3-VL-8B                                  │
├──────────────────────┬──────────────────────────────────────┤
│  原始 PDF            │  翻译结果                            │
│                      │                                      │
│ [选择PDF] [<] [>]    │ [开始翻译] [保存PDF]                 │
│  页面: 1/10          │  翻译页面: 0/10                      │
│ ┌──────────────────┐ │ ┌──────────────────────────────────┐ │
│ │                  │ │ │                                  │ │
│ │   PDF 预览区     │ │ │    翻译结果预览区                │ │
│ │                  │ │ │                                  │ │
│ └──────────────────┘ │ └──────────────────────────────────┘ │
├──────────────────────┴──────────────────────────────────────┤
│ 状态：就绪          [进度条]                                │
└─────────────────────────────────────────────────────────────┘
```

## 常见问题排查

### ❌ 问题 1：无法连接到 llama.cpp server

**症状**: 点击"开始翻译"后报错"连接失败"

**解决方案**:
```bash
# 1. 检查服务器是否运行
curl http://127.0.0.1:8033

# 2. 检查端口是否被占用
netstat -ano | findstr :8033

# 3. 重启 llama.cpp server
```

### ❌ 问题 2：翻译速度很慢

**症状**: 每页翻译需要很长时间

**解决方案**:
- 确保使用了 GPU 加速（`-ngl 50` 参数）
- 检查显存是否充足
- 降低 PDF 渲染分辨率（修改代码中的 DPI 参数）
- 使用更小的模型（如 Q4 量化版本）

### ❌ 问题 3：编译失败

**症状**: `dotnet build` 报错

**解决方案**:
```bash
# 清理并重新构建
dotnet clean
dotnet restore
dotnet build
```

### ❌ 问题 4：保存 PDF 失败

**症状**: 点击"保存翻译 PDF"后报错

**解决方案**:
- 检查目标文件夹是否有写入权限
- 确保目标文件未被其他程序打开
- 检查磁盘空间是否充足

## 配置选项

### 修改 API 端点

如果您的 llama.cpp server 运行在不同的地址或端口，请修改 `Form1.cs`:

```csharp
private const string LLAMA_API_URL = "http://您的地址:端口/v1/chat/completions";
```

### 调整翻译质量

在 `TranslatePageWithVision` 方法中修改参数：

```csharp
max_tokens = 2000,      // 增加以获得更详细的翻译
temperature = 0.3       // 降低以获得更稳定的翻译
```

### 修改 PDF 渲染分辨率

在 `DisplayCurrentPage` 和 `TranslatePageWithVision` 方法中修改 DPI：

```csharp
var image = pdfDocument.Render(currentPageIndex, 300, 300, PdfRenderFlags.CorrectFromDpi);
// 将 300 改为更高的值（如 600）以获得更高质量
// 或改为更低的值（如 150）以提高速度
```

## 性能优化建议

### 硬件建议
- **CPU**: 8 核心或以上
- **GPU**: NVIDIA GPU with 8GB+ VRAM（用于 GPU 加速）
- **内存**: 16GB 或以上
- **存储**: SSD（提高 PDF 读写速度）

### 软件优化
1. **使用 GPU 加速**: 确保 `-ngl 50` 参数正确设置
2. **批处理**: 一次性翻译整个文档，而不是单页翻译
3. **缓存**: 已翻译的页面会被缓存，切换页面时无需重新翻译
4. **并行处理**: 可以修改代码支持多页并行翻译（需要更多内存）

## 下一步

- 📖 阅读 [README.md](README.md) 了解详细功能
- 💼 阅读 [COMMERCIAL_USE.md](COMMERCIAL_USE.md) 了解商业使用
- 🔧 修改代码以适应您的特定需求
- 🚀 集成到您的工作流程中

## 获取帮助

- 提交 Issue: 在 GitHub 上报告问题
- 查看日志: 检查状态栏的错误信息
- 社区支持: 查找相关的技术论坛

---

**祝您使用愉快！** 🎉

如有问题，请随时提问。

