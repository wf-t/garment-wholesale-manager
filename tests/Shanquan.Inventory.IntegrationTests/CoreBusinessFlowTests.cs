using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Domain.Common;
using Shanquan.Inventory.Infrastructure.CoreBusiness;
using Shanquan.Inventory.Infrastructure.Persistence;

namespace Shanquan.Inventory.IntegrationTests;

public sealed class CoreBusinessFlowTests
{
    [Fact]
    public async Task Workspace_SalesStatisticsIncludeOrdersBeyondRecentOrderLimit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            var customerId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            setup.Customers.Add(new CustomerEntity { Id = customerId, Code = "C001", Name = "统计客户" });
            setup.Warehouses.Add(new WarehouseEntity { Id = warehouseId, Code = "W001", Name = "主仓" });
            for (var index = 0; index < 101; index++)
            {
                setup.SalesOrders.Add(new SalesOrderEntity
                {
                    Id = Guid.NewGuid(),
                    Number = $"SO202501{index:000}",
                    CustomerId = customerId,
                    WarehouseId = warehouseId,
                    Status = "已签收",
                    TotalQuantity = 2,
                    TotalAmountCents = 12_300,
                    DocumentDate = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
                    CreatedAt = new DateTimeOffset(2025, 1, 15, 0, index % 60, 0, TimeSpan.Zero),
                });
            }
            setup.SalesOrders.Add(new SalesOrderEntity
            {
                Id = Guid.NewGuid(),
                Number = "SO202502001",
                CustomerId = customerId,
                WarehouseId = warehouseId,
                Status = "已签收",
                TotalQuantity = 3,
                TotalAmountCents = 45_600,
                DocumentDate = new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
                CreatedAt = new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            });
            await setup.SaveChangesAsync();
        }

        var service = new CoreBusinessService(new TestDbContextFactory(options));
        var workspace = await service.GetWorkspaceAsync();

        Assert.Equal(100, workspace.SalesOrders.Count);
        var january = Assert.Single(workspace.SalesPeriodSummaries, x => x.Year == 2025 && x.Month == 1);
        Assert.Equal(101, january.OrderCount);
        Assert.Equal(202, january.Quantity);
        Assert.Equal(1_242_300, january.AmountCents);
        var february = Assert.Single(workspace.SalesPeriodSummaries, x => x.Year == 2025 && x.Month == 2);
        Assert.Equal(45_600, february.AmountCents);
    }

    [Fact]
    public async Task CustomerManagement_CreatesUpdatesAndDisablesCustomer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options)) await setup.Database.EnsureCreatedAsync();
        var service = new CoreBusinessService(new TestDbContextFactory(options));

        var customerId = await service.SaveCustomerAsync(new SaveCustomerRequest(
            null, "C009", "新客户", "13800000009", "郑州市", 100_000));
        await service.SaveCustomerAsync(new SaveCustomerRequest(
            customerId, "C009", "新客户有限公司", "13800000009", "郑州市二七区", 200_000));
        await service.SetCustomerActiveAsync(customerId, false);

        await using var verify = new InventoryDbContext(options);
        var customer = await verify.Customers.SingleAsync();
        Assert.Equal("新客户有限公司", customer.Name);
        Assert.Equal("郑州市二七区", customer.Address);
        Assert.Equal(200_000, customer.CreditLimitCents);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public async Task InventoryManagement_CreatesProductSku_ReceivesAndAdjustsStockWithMovements()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Warehouses.Add(new WarehouseEntity { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Code = "W001", Name = "主仓" });
            await setup.SaveChangesAsync();
        }
        var service = new CoreBusinessService(new TestDbContextFactory(options));
        var productId = await service.SaveProductAsync(new SaveProductRequest(
            null, "88001", "测试风衣", "外套", "秋季", 19_900, 11_500));
        var skuId = await service.AddSkuAsync(new AddSkuRequest(productId, "卡其", "M", "88001KQM"));
        var warehouseId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await service.ReceiveStockAsync(new ReceiveStockRequest(warehouseId, skuId, 30, "首批到货"));
        await service.AdjustStockAsync(new AdjustStockRequest(warehouseId, skuId, 28, "盘点少两件"));

        await using var verify = new InventoryDbContext(options);
        Assert.Equal(28, (await verify.StockBalances.SingleAsync()).OnHand);
        var movements = (await verify.StockMovements.ToListAsync()).OrderBy(x => x.CreatedAt).ToList();
        Assert.Equal(2, movements.Count);
        Assert.Equal(30, movements[0].OnHandChange);
        Assert.Equal(-2, movements[1].OnHandChange);
    }

    [Fact]
    public async Task ProductSetup_MaintainsWarehouseCategoriesAndEditableVariant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options)) await setup.Database.EnsureCreatedAsync();
        var service = new CoreBusinessService(new TestDbContextFactory(options));

        var warehouseId = await service.SaveWarehouseAsync("W009", "测试新仓");
        await service.SaveCategoriesAsync(["女裤", "上衣"]);
        var productId = await service.SaveProductAsync(new SaveProductRequest(
            null, "99001", "批量规格测试款", "女裤", "夏季", 12_800, 7_600));
        var mediumSkuId = await service.AddSkuAsync(new AddSkuRequest(productId, "黑色", "M", "99001BLKM", warehouseId));
        await service.AddSkuAsync(new AddSkuRequest(productId, "黑色", "L", "99001BLKL", warehouseId));
        await service.SaveSkuAsync(new SaveSkuRequest(mediumSkuId, "雅黑", "M", "99001DARKM"));
        await service.SaveWarehouseAsync(warehouseId, "W010", "成品仓");
        var colorId = await service.SaveColorNameAsync(null, "浅卡其");
        await service.SaveColorNameAsync(colorId, "深卡其");

        var workspace = await service.GetWorkspaceAsync();
        Assert.Contains(workspace.Categories, x => x == "女裤");
        Assert.Contains(workspace.Warehouses, x => x.Id == warehouseId && x.Code == "W010" && x.Name == "成品仓");
        Assert.Contains(workspace.Colors, x => x.Id == colorId && x.DisplayName == "深卡其");
        Assert.Equal(7_600, workspace.Products.Single().CostPriceCents);
        Assert.Equal(2, workspace.Skus.Count);
        Assert.Contains(workspace.Skus, x => x.Id == mediumSkuId && x.ColorCode == "雅黑" && x.Barcode == "99001DARKM");
        Assert.Equal(2, workspace.Stock.Count);
        Assert.All(workspace.Stock, x => Assert.Equal("夏季", x.Season));
        Assert.All(workspace.Stock, x => Assert.Equal("女裤", x.Category));
    }

    [Fact]
    public async Task CategoryManagement_RenamesCategoryAndUpdatesRelatedProducts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options)) await setup.Database.EnsureCreatedAsync();
        var service = new CoreBusinessService(new TestDbContextFactory(options));

        await service.SaveCategoryAsync("女裤");
        await service.SaveProductAsync(new SaveProductRequest(
            null, "99002", "测试长裤", "女裤", "秋季", 10_800, 6_200));

        await service.RenameCategoryAsync("女裤", "裤装");

        var workspace = await service.GetWorkspaceAsync();
        Assert.DoesNotContain("女裤", workspace.Categories);
        Assert.Contains("裤装", workspace.Categories);
        Assert.Equal("裤装", workspace.Products.Single().Category);
    }

    [Fact]
    public async Task CategoryManagement_DeletesCategoryAndRelatedProducts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        await using (var setup = new InventoryDbContext(options)) await setup.Database.EnsureCreatedAsync();
        var service = new CoreBusinessService(new TestDbContextFactory(options));

        await service.SaveCategoryAsync("1");
        var productId = await service.SaveProductAsync(new SaveProductRequest(
            null, "99003", "保留商品测试", "1", "秋季", 10_800, 6_200));
        await service.AddSkuAsync(new AddSkuRequest(productId, "黑色", "M", "99003BLKM"));

        await service.DeleteCategoryAsync("1");

        var workspace = await service.GetWorkspaceAsync();
        Assert.DoesNotContain("1", workspace.Categories);
        Assert.Empty(workspace.Products);
        Assert.Empty(workspace.Skus);
    }

    [Fact]
    public async Task OrderReservation_ToShipmentAndReceivable_IsAtomicFlow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);

        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Customers.Add(new CustomerEntity { Id = customerId, Code = "C001", Name = "测试客户" });
            setup.Warehouses.Add(new WarehouseEntity { Id = warehouseId, Code = "W001", Name = "主仓" });
            setup.Products.Add(new ProductEntity { Id = productId, StyleNumber = "22717", Name = "测试款", BasePriceCents = 9_200 });
            setup.Skus.Add(new SkuEntity { Id = skuId, ProductId = productId, ColorCode = "-2", SizeCode = "28" });
            setup.StockBalances.Add(new StockBalanceEntity { Id = Guid.NewGuid(), WarehouseId = warehouseId, SkuId = skuId, OnHand = 20 });
            await setup.SaveChangesAsync();
        }

        var service = new CoreBusinessService(factory);
        var orderNumber = await service.CreateAndReserveSalesOrderAsync(new CreateSalesOrderRequest(
            customerId,
            warehouseId,
            [new CreateSalesOrderLine(skuId, 6)],
            DateTimeOffset.Now));

        await using (var afterOrder = new InventoryDbContext(options))
        {
            var order = await afterOrder.SalesOrders.SingleAsync();
            var balance = await afterOrder.StockBalances.SingleAsync();
            Assert.Equal(orderNumber, order.Number);
            Assert.Equal("待配货", order.Status);
            Assert.Equal(55_200, order.TotalAmountCents);
            Assert.Equal(20, balance.OnHand);
            Assert.Equal(6, balance.Reserved);
        }

        var salesOrderId = (await service.GetWorkspaceAsync()).SalesOrders.Single().Id;
        await service.StartPickingAsync(salesOrderId);
        await service.CompletePickingAsync(salesOrderId);
        var shipmentNumber = await service.ShipSalesOrderAsync(salesOrderId);

        await using (var afterShipment = new InventoryDbContext(options))
        {
            var order = await afterShipment.SalesOrders.SingleAsync();
            var balance = await afterShipment.StockBalances.SingleAsync();
            var receivable = await afterShipment.ReceivableEntries.SingleAsync();
            Assert.Equal("已发货", order.Status);
            Assert.Equal(14, balance.OnHand);
            Assert.Equal(0, balance.Reserved);
            Assert.Equal(55_200, receivable.AmountCents);
            Assert.Equal(shipmentNumber, receivable.ReferenceNumber);
        }
    }

    [Fact]
    public async Task CreatingOrder_WithInsufficientStock_DoesNotLeavePartialOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();

        await using (var setup = new InventoryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Customers.Add(new CustomerEntity { Id = customerId, Code = "C001", Name = "测试客户" });
            setup.Warehouses.Add(new WarehouseEntity { Id = warehouseId, Code = "W001", Name = "主仓" });
            setup.Products.Add(new ProductEntity { Id = productId, StyleNumber = "22717", Name = "测试款", BasePriceCents = 9_200 });
            setup.Skus.Add(new SkuEntity { Id = skuId, ProductId = productId, ColorCode = "-2", SizeCode = "28" });
            setup.StockBalances.Add(new StockBalanceEntity { Id = Guid.NewGuid(), WarehouseId = warehouseId, SkuId = skuId, OnHand = 2 });
            await setup.SaveChangesAsync();
        }

        var service = new CoreBusinessService(factory);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.CreateAndReserveSalesOrderAsync(
            new CreateSalesOrderRequest(customerId, warehouseId, [new CreateSalesOrderLine(skuId, 3)], DateTimeOffset.Now)));

        await using var verify = new InventoryDbContext(options);
        Assert.Empty(await verify.SalesOrders.ToListAsync());
        Assert.Equal(0, (await verify.StockBalances.SingleAsync()).Reserved);
    }

    private sealed class TestDbContextFactory(DbContextOptions<InventoryDbContext> options)
        : IDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext() => new(options);
    }
}
