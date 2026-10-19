using Microsoft.EntityFrameworkCore;
using Shanquan.Inventory.Application.Abstractions;

namespace Shanquan.Inventory.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IDbContextFactory<InventoryDbContext> contextFactory,
    AppPaths paths) : IAppDatabaseInitializer
{
    public async Task<DatabaseStartupResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        paths.EnsureDirectories();
        var existed = File.Exists(paths.DatabasePath);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous = FULL;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;", cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await ApplyCompatibilitySchemaAsync(db, cancellationToken);

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        var sqliteVersion = (string?)await command.ExecuteScalarAsync(cancellationToken) ?? "unknown";

        return new DatabaseStartupResult(paths.DatabasePath, sqliteVersion, !existed);
    }

    private static async Task ApplyCompatibilitySchemaAsync(
        InventoryDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(db, "products", "BasePriceCents", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE products ADD COLUMN BasePriceCents INTEGER NOT NULL DEFAULT 0;",
                cancellationToken);
        }

        await AddColumnIfMissingAsync(db, "products", "CostPriceCents", "INTEGER NOT NULL DEFAULT 0", cancellationToken);

        await AddColumnIfMissingAsync(db, "products", "Category", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "products", "Season", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "customers", "Phone", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "customers", "Address", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "customers", "Notes", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "customers", "PaymentTermDays", "INTEGER NOT NULL DEFAULT 30", cancellationToken);
        await AddColumnIfMissingAsync(
            db,
            "customers",
            "CreatedAt",
            "TEXT NOT NULL DEFAULT '2026-01-01T00:00:00+00:00'",
            cancellationToken);

        string[] statements =
        [
            """
            CREATE TABLE IF NOT EXISTS categories (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_categories_Name ON categories (Name);",
            """
            CREATE TABLE IF NOT EXISTS colors (
                Id TEXT NOT NULL PRIMARY KEY,
                ChineseName TEXT NOT NULL,
                EnglishName TEXT NOT NULL,
                Hex TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_colors_ChineseName_EnglishName ON colors (ChineseName, EnglishName);",
            """
            CREATE TABLE IF NOT EXISTS sales_orders (
                Id TEXT NOT NULL PRIMARY KEY,
                Number TEXT NOT NULL,
                CustomerId TEXT NOT NULL,
                WarehouseId TEXT NOT NULL,
                Status TEXT NOT NULL,
                TotalQuantity INTEGER NOT NULL,
                GoodsAmountCents INTEGER NOT NULL DEFAULT 0,
                FreightCents INTEGER NOT NULL DEFAULT 0,
                DiscountCents INTEGER NOT NULL DEFAULT 0,
                TotalAmountCents INTEGER NOT NULL,
                ReceivedAmountCents INTEGER NOT NULL DEFAULT 0,
                PaymentStatus TEXT NOT NULL DEFAULT '未收款',
                DocumentDate TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ApprovedAt TEXT NULL,
                PickingStartedAt TEXT NULL,
                ReadyToShipAt TEXT NULL,
                ShippedAt TEXT NULL,
                SignedAt TEXT NULL
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_sales_orders_Number ON sales_orders (Number);",
            "CREATE INDEX IF NOT EXISTS IX_sales_orders_CustomerId_CreatedAt ON sales_orders (CustomerId, CreatedAt);",
            """
            CREATE TABLE IF NOT EXISTS sales_drafts (
                Id TEXT NOT NULL PRIMARY KEY,
                CustomerId TEXT NOT NULL,
                WarehouseId TEXT NOT NULL,
                FreightCents INTEGER NOT NULL,
                DiscountCents INTEGER NOT NULL,
                PaymentStatus TEXT NOT NULL,
                DocumentDate TEXT NOT NULL,
                LinesJson TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS sales_order_lines (
                Id TEXT NOT NULL PRIMARY KEY,
                SalesOrderId TEXT NOT NULL,
                SkuId TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                ShippedQuantity INTEGER NOT NULL,
                UnitPriceCents INTEGER NOT NULL,
                AmountCents INTEGER NOT NULL
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_sales_order_lines_SalesOrderId_SkuId ON sales_order_lines (SalesOrderId, SkuId);",
            """
            CREATE TABLE IF NOT EXISTS shipments (
                Id TEXT NOT NULL PRIMARY KEY,
                Number TEXT NOT NULL,
                SalesOrderId TEXT NOT NULL,
                CustomerId TEXT NOT NULL,
                WarehouseId TEXT NOT NULL,
                TotalQuantity INTEGER NOT NULL,
                TotalAmountCents INTEGER NOT NULL,
                ExpressTrackingNumber TEXT NULL,
                ShippedAt TEXT NOT NULL
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_shipments_Number ON shipments (Number);",
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_shipments_SalesOrderId ON shipments (SalesOrderId);",
            """
            CREATE TABLE IF NOT EXISTS shipment_lines (
                Id TEXT NOT NULL PRIMARY KEY,
                ShipmentId TEXT NOT NULL,
                SalesOrderLineId TEXT NOT NULL,
                SkuId TEXT NOT NULL,
                Quantity INTEGER NOT NULL
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_shipment_lines_ShipmentId_SalesOrderLineId ON shipment_lines (ShipmentId, SalesOrderLineId);",
            """
            CREATE TABLE IF NOT EXISTS stock_movements (
                Id TEXT NOT NULL PRIMARY KEY,
                WarehouseId TEXT NOT NULL,
                SkuId TEXT NOT NULL,
                MovementType TEXT NOT NULL,
                OnHandChange INTEGER NOT NULL,
                ReservedChange INTEGER NOT NULL,
                OnHandAfter INTEGER NOT NULL,
                ReservedAfter INTEGER NOT NULL,
                ReferenceNumber TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                DueAt TEXT NULL,
                PaymentMethod TEXT NULL,
                Note TEXT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS IX_stock_movements_WarehouseId_SkuId_CreatedAt ON stock_movements (WarehouseId, SkuId, CreatedAt);",
            """
            CREATE TABLE IF NOT EXISTS receivable_entries (
                Id TEXT NOT NULL PRIMARY KEY,
                CustomerId TEXT NOT NULL,
                EntryType TEXT NOT NULL,
                AmountCents INTEGER NOT NULL,
                ReferenceNumber TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """,
            "CREATE INDEX IF NOT EXISTS IX_receivable_entries_CustomerId_CreatedAt ON receivable_entries (CustomerId, CreatedAt);",
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_receivable_entries_EntryType_ReferenceNumber ON receivable_entries (EntryType, ReferenceNumber);",
        ];

        foreach (var statement in statements)
            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);

        await AddColumnIfMissingAsync(db, "sales_orders", "GoodsAmountCents", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "FreightCents", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "DiscountCents", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "ReceivedAmountCents", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "PaymentStatus", "TEXT NOT NULL DEFAULT '未收款'", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "DocumentDate", "TEXT NOT NULL DEFAULT '2026-01-01T00:00:00+00:00'", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "PickingStartedAt", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "ReadyToShipAt", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_orders", "SignedAt", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "sales_drafts", "DocumentDate", "TEXT NOT NULL DEFAULT '2026-01-01T00:00:00+00:00'", cancellationToken);
        await AddColumnIfMissingAsync(db, "shipments", "ExpressTrackingNumber", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "receivable_entries", "DueAt", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "receivable_entries", "PaymentMethod", "TEXT NULL", cancellationToken);
        await AddColumnIfMissingAsync(db, "receivable_entries", "Note", "TEXT NULL", cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE sales_orders SET Status = '待配货' WHERE Status = '已锁库';",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE sales_orders SET DocumentDate = CreatedAt WHERE DocumentDate = '2026-01-01T00:00:00+00:00';",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE sales_drafts SET DocumentDate = UpdatedAt WHERE DocumentDate = '2026-01-01T00:00:00+00:00';",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE receivable_entries SET DueAt = datetime(CreatedAt, '+30 days') WHERE EntryType = '销售应收' AND DueAt IS NULL;",
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT OR IGNORE INTO categories (Id, Name, IsActive) SELECT lower(hex(randomblob(16))), Category, 1 FROM products WHERE Category IS NOT NULL AND trim(Category) <> '';",
            cancellationToken);

    }

    private static async Task<bool> ColumnExistsAsync(
        InventoryDbContext db,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task AddColumnIfMissingAsync(
        InventoryDbContext db,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(db, table, column, cancellationToken)) return;
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedDemoDataAsync(
        InventoryDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.Customers.AnyAsync(cancellationToken) || await db.Products.AnyAsync(cancellationToken))
            return;

        var warehouse = new WarehouseEntity
        {
            Id = Guid.CreateVersion7(),
            Code = "W001",
            Name = "世贸二楼仓",
        };
        var customer = new CustomerEntity
        {
            Id = Guid.CreateVersion7(),
            Code = "C001",
            Name = "郑州盛轩服饰",
            CreditLimitCents = 5_000_000,
            Phone = "13598884468",
            Address = "郑州市世贸商城",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var secondCustomer = new CustomerEntity
        {
            Id = Guid.CreateVersion7(),
            Code = "C002",
            Name = "洛阳晴川女装",
            CreditLimitCents = 3_000_000,
            Phone = "13800001234",
            Address = "洛阳市西工区",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var products = new[]
        {
            new ProductEntity { Id = Guid.CreateVersion7(), StyleNumber = "22717", Name = "千仞四面弹入A裤", Category = "女裤", Season = "四季", BasePriceCents = 9_200, CostPriceCents = 5_500 },
            new ProductEntity { Id = Guid.CreateVersion7(), StyleNumber = "22718", Name = "千仞四面弹夏神裤", Category = "女裤", Season = "夏季", BasePriceCents = 9_100, CostPriceCents = 5_300 },
            new ProductEntity { Id = Guid.CreateVersion7(), StyleNumber = "33021", Name = "轻柔雪纺衬衫", Category = "上衣", Season = "春夏", BasePriceCents = 7_900, CostPriceCents = 4_500 },
        };

        db.Warehouses.Add(warehouse);
        db.Categories.AddRange(
            new CategoryEntity { Id = Guid.CreateVersion7(), Name = "女裤" },
            new CategoryEntity { Id = Guid.CreateVersion7(), Name = "上衣" });
        db.Customers.AddRange(customer, secondCustomer);
        db.Products.AddRange(products);

        foreach (var product in products)
        {
            foreach (var color in new[] { "-2", "-5" })
            {
                foreach (var size in new[] { "27", "28", "29", "30", "31", "32" })
                {
                    var sku = new SkuEntity
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = product.Id,
                        ColorCode = color,
                        SizeCode = size,
                        Barcode = $"{product.StyleNumber}{color.TrimStart('-')}{size}",
                    };
                    db.Skus.Add(sku);
                    db.StockBalances.Add(new StockBalanceEntity
                    {
                        Id = Guid.CreateVersion7(),
                        WarehouseId = warehouse.Id,
                        SkuId = sku.Id,
                        OnHand = 20,
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
