# Garment Wholesale Manager

服装进销存批发管理系统，面向服装批发业务的离线桌面应用。

英文项目名：**Garment Wholesale Manager**；GitHub 仓库名：`garment-wholesale-manager`；可执行文件名：`GarmentWholesaleManager`。

## 技术与平台

- .NET 10、Avalonia 12.1、CommunityToolkit.Mvvm。
- Entity Framework Core + 本机 SQLite，无需启动 HTTP 服务。
- Windows x64、Apple 芯片 macOS 发布脚本。
- 当前面向单机使用；首次准备开发环境需要联网下载 SDK 和 NuGet 依赖。

## 当前功能

- 客户档案、启停用、信用额度、客户 CSV 导出。
- 商品款号、颜色尺码 SKU、分类与仓库维护。
- 到货入库、盘点调整、库存余额与流水。
- 销售草稿保存恢复、颜色尺码矩阵、单价、运费与优惠。
- 审核锁库、开始配货、完成配货、整单发货、快递单号、签收确认。
- 按收款状态生成应收、客户收款登记、按先后顺序抵扣欠款、客户对账及应收汇总 CSV 导出。

供应商采购单、拆单发货、退换货、完整报表及自动备份仍需后续完善。以当前源码为准，不将规划视为已交付功能。

## 快速开始

```bash
git clone https://github.com/wf-t/garment-wholesale-manager.git
cd garment-wholesale-manager
./scripts/bootstrap-mac.sh
./scripts/run-mac.sh
```

Windows 或已有 .NET SDK 的环境，见[开发指南](docs/DEVELOPMENT.md)。SDK 要求由 `global.json` 定义。

```bash
# 测试
./scripts/test.sh
# Windows x64 发布
./scripts/publish-windows.sh
# Apple 芯片 Mac 发布
./scripts/publish-mac.sh
```

发布输出位于 `releases/`，不纳入 Git。源码仓库不附带预编译安装包，克隆后需要自行构建。

## 文档导航

| 文档 | 内容 |
| --- | --- |
| [操作指南](docs/USER_GUIDE.md) | 基础资料、销售、配货、库存、应收与数据备份 |
| [开发指南](docs/DEVELOPMENT.md) | SDK、安装、运行、测试、目录与排错 |
| [架构说明](docs/ARCHITECTURE.md) | 分层、事务、数据与演进边界 |
| [发布指南](docs/RELEASING.md) | 构建、产物、验证与升级 |
| [贡献说明](CONTRIBUTING.md) | 修改规范与提交要求 |

## 数据规则

可用库存 = 实际库存 − 锁定库存。审核只锁库，发货才扣实际库存；已收款销售不重复生成应收。金额以整数“分”存储。

数据库位于系统 `LocalApplicationData/ShanquanInventory/data/main.db`。实际根目录由 .NET 按操作系统解析，不随仓库英文名更改。备份步骤见操作指南，不能让多台电脑共享同一 SQLite 文件。

## 许可

本仓库暂未指定开源许可证；上传 GitHub 不代表授予开源使用许可。
