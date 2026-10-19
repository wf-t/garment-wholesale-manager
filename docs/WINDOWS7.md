# Windows 7 部署说明

## 兼容基线

- 操作系统：Windows 7 Service Pack 1，64 位
- 应用框架：.NET 6.0.36，自包含发布
- UI 框架：Avalonia 11.0.6（固定版本）
- 数据库：SQLite，本地文件

Avalonia 11 文档说明应用仍可运行在 Windows 7，但该系统只获得有限支持；
Avalonia 11.0.7 曾引入 Windows 7 启动回归，因此本分支固定使用 11.0.6。
.NET 6 是最后一个可用于 Windows 7 的现代 .NET 版本，并已于 2024-11-12
结束官方支持。Windows 7 本身也已停止安全维护，因此本版本只适合确实无法升级
操作系统的离线环境。

## 给用户的交付方式

### 销售单 A5 打印

Windows 销售单使用原生打印，不再打开浏览器，也不会打印临时文件目录地址。

1. 在销售单预览中点击“打印”，打开软件内的 Windows 打印设置窗口。
2. 选择打印机、纸张、横向／纵向和份数，默认选择打印机提供的 A5 纸和横向。
3. 点击“更新预览”。预览根据所选打印机的纸张与硬件边距排版，可用上一页／下一页检查整单。
4. 点击“确认打印”，程序将同一份排版以及纸张、方向、份数提交到 Windows 打印队列。

更改设置后旧预览失效，需要更新预览才能打印；提交后不会自动重复打印。
这里的选择仅作用于本次打印任务，不修改 Windows 中其他软件使用的打印机默认设置。
默认页边距约 7mm，同时避开打印机不能打印的区域。表格列宽自适应，长文本换行，多页重复表头，合计只打印一次。
如果单行内容过长或列数多到无法清晰打印，会提示调整内容或纸张，不会静默裁掉内容。

若驱动不提供 A5，程序不会偷偷换成 A4；请检查驱动或明确选择其他纸张。
打印完成提示表示任务已提交到 Windows 队列，并不代表纸张已经输出。故障时先检查队列再重打。
首次使用仍需在目标打印机上试打一张，核对纸张放置、方向、最右侧金额和份数。

macOS 保留浏览器打印，仍需在浏览器中关闭页眉页脚。

### 安装包

只把下面这一个文件交给普通用户：

```text
releases/服装进销存批发管理-Windows7-安装版.exe
```

用户双击并同意 Windows 管理员确认后，安装程序会自动完成以下工作：

1. 检查系统是否为 64 位 Windows 7 SP1 或更高版本。
2. Windows 7 缺少 KB3063858 时静默安装该补丁。
3. 缺少 Microsoft Visual C++ 2015-2019 x64 运行库时静默安装运行库。
4. 安装自包含的客户端，不要求用户安装 .NET。
5. 创建桌面快捷方式、开始菜单快捷方式和卸载入口。
6. 如果系统组件要求重启，只在安装结束时统一提示一次。

因此普通用户无需手动下载或判断 KB、VC++ 运行库、.NET。安装完成后双击桌面上的
“服装进销存批发管理”即可使用。

目标系统必须是 Windows 7 SP1 64 位。建议启用 Windows 7 的 Aero/DWM 桌面组合；
关闭桌面组合时，自定义窗口效果可能退化。

Microsoft 官方说明：

- https://learn.microsoft.com/dotnet/core/install/windows#dependencies
- https://support.microsoft.com/en-us/servicing/os/windows/2015/06/ms15-063-vulnerability-in-windows-kernel-could-allow-elevation-of-privilege-june-9-2015

## 生成一键安装包

在项目根目录执行：

```bash
./scripts/build-win7-installer.sh
```

脚本会依次校验内置的微软依赖安装包、发布 Windows 自包含客户端，并生成最终的
单文件安装程序：

```text
releases/服装进销存批发管理-Windows7-安装版.exe
```

安装包中内置的依赖固定为：

- Windows6.1-KB3063858-x64.msu
- Microsoft Visual C++ 2015-2019 Redistributable x64 14.29.30133

构建脚本会校验二者的 SHA-256，文件被替换或损坏时会立即停止，不会生成安装包。

## 只发布程序目录（开发用途）

在项目根目录执行：

```bash
./scripts/publish-windows.sh
```

也可以直接执行：

```bash
DOTNET_CLI_HOME="$PWD/.tools/dotnet-home" \
NUGET_PACKAGES="$PWD/.tools/nuget-packages" \
./.tools/dotnet/dotnet publish \
  src/Shanquan.Inventory.Desktop/Shanquan.Inventory.Desktop.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output releases/win7-x64 \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -p:DebugType=none \
  -p:DebugSymbols=false
```

不要启用单文件或裁剪发布。保留目录内的全部 DLL 与资源文件。这个目录主要用于
开发排查；普通用户应使用上面的一键安装包，因为直接解压运行无法自动补齐目标电脑
缺少的 Windows 系统组件。

## 数据位置

应用数据保存在当前 Windows 用户的本地应用数据目录，不会写入程序目录。
升级程序时可以替换整个程序目录，但不要删除用户本地应用数据。

## 验收建议

最终交付前必须在真实 Windows 7 SP1 x64 电脑上完成一次验收，至少检查：

- 程序启动、窗口拖动和缩放
- 中文输入法与搜索下拉框
- 新建客户、商品、采购入库和库存查询
- 销售开单、配货、发货、快递单号和签收
- 应收收款、日期选择和单据导出/打印
- 退出后重新启动，确认 SQLite 数据正常保留
