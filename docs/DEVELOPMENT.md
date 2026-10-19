# 开发指南

## 环境

- Git。
- .NET 10 SDK。`global.json` 指定 `10.0.400` 并允许 `latestFeature` 向后选择功能带；仅安装运行时无法编译。
- Windows 或 macOS 桌面环境。首次安装 SDK、还原依赖需要网络。

## macOS 脚本

在仓库根目录执行：

```bash
./scripts/bootstrap-mac.sh
./scripts/run-mac.sh
./scripts/test.sh
```

脚本使用仓库内 `.tools/dotnet`，设置本地 CLI 目录及 NuGet 缓存。bootstrap 在该路径没有 SDK 时通过官方安装脚本获取 10.0 通道 SDK。

若已有 `.tools/dotnet` 但版本不符合 `global.json`，bootstrap 不会自动替换它，需要安装兼容 SDK 或使用下面的系统 SDK 命令。

## 系统 SDK / Windows PowerShell

```powershell
dotnet --info
dotnet restore Shanquan.Inventory.slnx
dotnet build Shanquan.Inventory.slnx --configuration Release
dotnet run --project src/Shanquan.Inventory.Desktop/Shanquan.Inventory.Desktop.csproj
dotnet test Shanquan.Inventory.slnx --configuration Release
```

上面的 `dotnet` 命令同样可在 macOS 终端执行。Windows 无需运行 Bash 脚本。

## 目录

```text
src/        Desktop、Application、Domain、Infrastructure
tests/      领域与集成测试
docs/       操作、开发、架构与发布说明
scripts/    本地运行、测试与发布脚本
packaging/  macOS 包元数据
```

`.tools/`、`bin/`、`obj/`、`releases/`、`artifacts/` 和数据库按 `.gitignore` 排除。

## 验证重点

领域测试覆盖金额和库存约束；集成测试包含客户维护、商品 SKU、分类、到货盘点以及销售库存流程。现有测试不代表所有 UI 路径和业务功能都已覆盖。

涉及配货与发货时，应同时核对当前状态流转；旧测试中的直接发货调用可能与当前业务前置条件不符，不能未经运行就声称测试通过。

## 常见问题

- SDK 不匹配：对照 `global.json` 与 `dotnet --list-sdks`；脚本环境可用 `.tools/dotnet/dotnet --list-sdks` 检查。
- 依赖还原失败：检查网络及 NuGet 源，重试 `dotnet restore`。
- SQLite 被锁定：退出其他应用实例，确认数据库在本机磁盘。
- 找不到发布程序：`releases/` 不随源码分发，先按[发布指南](RELEASING.md)生成。
- 调整数据结构：核对兼容性 SQL，并使用已有数据库副本验证升级。
