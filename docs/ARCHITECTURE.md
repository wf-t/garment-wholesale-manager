# 架构基线

## 平台

- UI：Avalonia 12.1，Windows/macOS 共用 XAML 和 ViewModel。
- Runtime：.NET 10 LTS。
- Windows：首发 `win-x64` 自包含包。
- macOS：当前开发机直接运行，后期可发布 `osx-arm64` 和 `osx-x64`。
- 数据库：本机 SQLite；不启动本地 HTTP 服务。

平台专用代码只能放在 Infrastructure 的适配器中，包括打印机、文件选择器、安装包和系统权限。Domain 与 Application 禁止引用 Windows 或 macOS API。

## 分层

```text
Desktop        Avalonia 页面、ViewModel、导航、交互
Application    用例、DTO、端口、事务编排
Domain         商品、价格、销售、库存、应收规则
Infrastructure SQLite、文件、打印、备份、日志
```

依赖方向：Desktop → Application；Infrastructure → Application/Domain；Application → Domain。Domain 不依赖其他项目。

## 数据不变量

- 可用库存 = 实际库存 - 锁定库存。
- 锁定库存不能小于 0，也不能超过实际库存。
- 发货不能超过订单未发数量。
- 退货不能超过原发货可退数量。
- 库存流水合计必须等于库存余额。
- 客户资金流水合计必须等于客户余额。
- 金额一律按整数分保存。

## 首期模块

商品与价格、客户、供应商与采购、销售、配货发货、库存、应收收款、报表、系统管理。

当前已完成的可操作闭环包括：客户档案、商品款号与 SKU、采购到货入库、盘点调整、销售草稿、按仓库选择颜色尺码、销售审核锁库、整单发货、库存流水，以及按收款状态生成发货应收。供应商采购单、拆单发货、收款核销、退换货和报表仍属于后续迭代。

## 演进边界

首期假设单台电脑。未来多台终端同时操作时，保留 Domain/Application，把 SQLite Infrastructure 替换为局域网应用服务和 PostgreSQL/SQL Server。禁止多台电脑直接共享同一个 SQLite 文件。
