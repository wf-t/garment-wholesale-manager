# 架构说明

## 平台与部署

Garment Wholesale Manager 是使用 Avalonia 12.1 与 .NET 10 的离线桌面应用。Windows x64 与 macOS Apple 芯片由现有脚本构建；SQLite 仅放在本机磁盘，不启动本地 HTTP 服务。

## 分层与依赖

```text
Desktop         页面、ViewModel、交互、依赖注入入口
Application     ICoreBusinessService、DTO、基础设施接口
Domain          Money、StockBalance、SalesOrder 领域规则
Infrastructure  EF Core、SQLite、业务服务实现、数据库初始化
```

Desktop 引用 Application 和 Infrastructure，在启动入口注册服务；Infrastructure 引用 Application / Domain；Application 引用 Domain。Domain 不引用 UI 或基础设施。

当前业务编排主要实现在 Infrastructure 的 `CoreBusinessService`，接口位于 Application。不要把目标架构中的职责误认为已完成的代码迁移。平台 API 应留在 UI 或基础设施边界内。

## 核心流程

1. 销售审核校验仓库、SKU、可用库存及信用额度，并建立库存预占。
2. 开始配货 → 完成配货 → 待发货 → 整单发货 → 确认签收。
3. 发货事务同步写入发货单、库存余额、库存流水和待收金额对应的应收流水。
4. 客户收款以负金额流水记录；应收展示按发生时间将收款抵扣最早欠款。

UI / 持久化使用的中文状态与 Domain 的 `SalesOrderStatus` 枚举并非一一映射。当前不支持按指定应收单自由选择核销。

## 数据规则

- 可用库存 = 实际库存 − 锁定库存；锁定量不得小于零或大于实际库存。
- 发货扣减实际库存并释放相应预占；当前业务流程为整单发货。
- 金额按整数分保存；应收余额由正负金额流水汇总。
- 业务服务通过 EF Core 访问 SQLite；销售创建与发货等流程使用显式事务。

## 数据初始化与目录

`AppPaths` 解析系统 `LocalApplicationData`，在 `ShanquanInventory` 下建立 `data`、`backups`、`logs` 目录，数据库为 `data/main.db`。目录创建不等于已实现自动备份或文件日志功能。

`DatabaseInitializer` 调用 `EnsureCreatedAsync` 和兼容性 SQL，启用 WAL、外键与 FULL 同步。升级前必须备份。源码保留演示数据方法，但当前初始化入口未调用它，不能承诺首启自动填充演示数据。

## 已实现与规划

已实现功能见 [README](../README.md)。供应商采购单、拆单发货、退换货、完整报表与自动备份尚需完善。

未来多终端部署需要通过应用服务访问服务端数据库，不能直接共享 SQLite 文件。
