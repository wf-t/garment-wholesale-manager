# 服装进销存批发管理

面向服装批发业务的离线桌面进销存系统。本副本是专门面向 Windows 7 SP1 的兼容分支。

## 当前状态

- Avalonia 11.0.6 + .NET 6 Windows 7 兼容桌面应用
- 模块化单体分层：Desktop / Application / Domain / Infrastructure
- SQLite 本地数据库基础设施
- 金额整数分模型、库存预占与出库领域规则
- 销售开单、草稿恢复、审核锁库、整单发货、库存与应收闭环
- 客户档案新增、编辑、启用/停用及信用额度管理
- 商品款号、颜色尺码规格、采购入库、库存盘点和库存流水
- 销售与库存商品实时搜索
- 客户管理及弹窗式新增编辑表单
- 销售单按仓库校验可用库存，支持颜色尺码矩阵、可编辑售价、运费优惠和收款状态
- 兼容旧版数据库的自动升级及首启演示数据
- Windows 与 macOS 本地运行脚本

## 本地运行

macOS 首次运行：

```bash
./scripts/bootstrap-mac.sh
./scripts/run-mac.sh
```

运行测试：

```bash
./scripts/test.sh
```

生成给普通用户使用的 Windows 7 x64 一键安装包（推荐）：

```bash
./scripts/build-win7-installer.sh
```

生成文件：

```text
releases/服装进销存批发管理-Windows7-安装版.exe
```

用户只需双击这个安装程序。安装程序会自动检查并补齐 KB3063858 和
Microsoft Visual C++ 2015-2019 x64 运行库，然后安装客户端、创建桌面及开始菜单
快捷方式；用户不需要另行安装 .NET，也不需要手动处理依赖。系统需要重启时，
安装程序会在结束页统一提示。

只生成 Windows 7 x64 自包含程序目录（供开发和排查使用）：

```bash
./scripts/publish-windows.sh
```

不建议把程序目录或 ZIP 直接交给普通用户，因为目标电脑仍可能缺少 Windows 7
系统组件。完整说明见 [Windows 7 部署说明](docs/WINDOWS7.md)。

发布 Apple 芯片 Mac 版本：

```bash
./scripts/publish-mac.sh
```

macOS 开发调试包可直接运行：

```bash
./releases/osx-arm64/GarmentWholesaleManager
```

业务数据默认写入当前用户的本地应用数据目录，不会写入项目目录。正式版完全离线运行。

## 关键规则

- 销售审核只锁定库存，发货时才扣实际库存；未收款订单发货后形成客户应收，已收款订单不重复挂账。
- 金额以整数“分”保存，不使用浮点数。
- 库存和客户资金使用追加式流水，已审核单据不直接改写。
- SQLite 文件只能放在本机磁盘，不能放在网络共享盘。

详细架构见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。
