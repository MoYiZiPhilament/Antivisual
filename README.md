# Antivisual

一个基于 .NET 10 WPF 与 SkiaSharp 的视频封面生成器。左侧编辑曲目信息和图层设置，右侧实时预览，一次导出 16:9 与 4:3 两种 PNG。

## 功能

- 五层合成：模糊背景、双侧装饰、文本栏底图、双行文字、曲目封面。
- 两种 PSD 基准画布：1920×1080 与 1920×1440。
- 默认字体 Felix Titling，支持系统字体切换和自定义光泽颜色。
- 第三层固定使用项目内 `TextBG_6.png`，采用 PSD 专用定位。
- 非方形封面直接拉伸为 1:1，使用宽幅浓阴影与近身接触阴影。
- 中文深色界面、圆角浅色描边窗口与自绘顶栏，自动保存文字、素材、字体、颜色、开关和预览比例。
- 四边及四角应用内平滑缩放；拖拽时使用缓存预览，停止后自动恢复高清渲染。
- 重复导出自动追加时间戳，不覆盖已有作品。

## 运行环境

- Windows 10 19041 或更高版本
- .NET 10 SDK / Runtime
- 推荐安装 Felix Titling 字体；缺失时渲染器会回退到 Segoe UI 和系统默认字体

```powershell
dotnet restore Antivisual.sln
dotnet run --project Antivisual.csproj
```

运行测试：

```powershell
dotnet test Antivisual.sln
```

## 默认素材位置

默认素材已内置到项目的 `Assets` 目录，并会在构建时复制到应用输出目录：

- 背景：`Assets\Backgrounds\megarex_signal.jpg`
- 侧饰：`Assets\Side\lacrymira.png`、`Assets\Side\nell.png`
- 文字底图：`Assets\Text\TextBG_6.png`（第三层固定使用）

背景和封面可通过文件选择器更换。侧饰修改后，点击“重新扫描侧饰素材”。旧版本保存的外部默认素材路径会自动迁移到项目内素材，自定义封面路径保持不变。缺失或损坏的素材会跳过对应图层，其余内容继续渲染。

## 导出与设置

- 输出目录：当前用户的 `文档\Antivisual`（支持文档目录重定向）。
- 文件名：`曲目名_16x9.png` 与 `曲目名_4x3.png`。
- 设置文件：`%LocalAppData%\Antivisual\settings.json`。
- 预览与导出共用 `CoverRenderer`，导出使用冻结的同一设置快照。

## 项目结构

- `Models`：不可变设置、比例与素材项。
- `Rendering`：PSD 坐标、五层 SkiaSharp 渲染和 PNG 编码。
- `Services`：素材枚举、设置持久化、双比例导出。
- `ViewModels`：编辑状态、命令和自动保存。
- `Antivisual.Tests`：布局、文件名、渲染、颜色变化及重复导出测试。

界面遵循项目内安装的 `emil-design-eng` Skill，使用克制动效、明确反馈和统一的深色控件样式。Photoshop 缎面与辉光由 SkiaSharp 近似实现，并非 Photoshop 图层样式的逐像素复刻。
