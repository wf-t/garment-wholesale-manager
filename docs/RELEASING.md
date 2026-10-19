# 发布指南

## 构建前

确认 SDK 满足 `global.json`，还原依赖，执行测试并记录实际结果。更改数据库结构时，应在备份副本上验证旧数据升级。

```bash
./scripts/test.sh
```

## macOS 上使用现有脚本

```bash
./scripts/publish-windows.sh
./scripts/publish-mac.sh
```

| 目标 | 输出目录 | 入口 |
| --- | --- | --- |
| Windows x64 | `releases/win-x64/` | `GarmentWholesaleManager.exe` |
| Apple 芯片 Mac | `releases/osx-arm64/` | `GarmentWholesaleManager` |

脚本生成自包含目录，用户运行时无需单独安装 .NET。分发时保留整个目录，不能只复制入口文件。

## 系统 SDK / Windows PowerShell

```powershell
dotnet publish src/Shanquan.Inventory.Desktop/Shanquan.Inventory.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output releases/win-x64
```

macOS Apple 芯片可将 runtime 和 output 改为 `osx-arm64`、`releases/osx-arm64`。Intel Mac 的 `osx-x64` 需另行构建与目标机器验证，现有 macOS 脚本只指定 Apple 芯片。

## 发布验证

- 在目标操作系统启动程序，检查首次数据库初始化。
- 使用测试数据走通入库、审核、配货、发货、签收与收款。
- 验证已有数据库升级与备份恢复，检查 CSV 导出。
- 记录版本、目标平台、验证结果与已知问题后，打包上传 GitHub Release 附件。

源码上传本身不创建 Release，也不表示目标平台已验证。

## 包与升级边界

`packaging/macos/Info.plist` 是包元数据，现有 publish 脚本没有创建 `.app`、安装器、签名或公证的步骤。不要将 publish 输出描述为已签名安装包。

`releases/` 与 `artifacts/` 被 Git 忽略。发布包与业务数据库不提交到源码仓库。升级程序前退出应用并备份业务数据；更新后核对库存、客户与应收。
