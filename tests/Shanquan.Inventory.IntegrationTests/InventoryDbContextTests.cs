using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shanquan.Inventory.Infrastructure.Persistence;

namespace Shanquan.Inventory.IntegrationTests;

public sealed class InventoryDbContextTests
{
    [Fact]
    public async Task EnsureCreated_CreatesCoreTablesAndAcceptsValidStock()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new InventoryDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var product = new ProductEntity { Id = Guid.NewGuid(), StyleNumber = "22717", Name = "千行四面弹八分裤" };
        var warehouse = new WarehouseEntity { Id = Guid.NewGuid(), Code = "WH-02", Name = "世贸二楼" };
        var sku = new SkuEntity { Id = Guid.NewGuid(), ProductId = product.Id, ColorCode = "BLACK", SizeCode = "28" };
        db.AddRange(product, warehouse, sku);
        db.StockBalances.Add(new StockBalanceEntity { Id = Guid.NewGuid(), WarehouseId = warehouse.Id, SkuId = sku.Id, OnHand = 20, Reserved = 6 });

        await db.SaveChangesAsync();

        Assert.Equal(14, await db.StockBalances.Select(x => x.OnHand - x.Reserved).SingleAsync());
    }
}
