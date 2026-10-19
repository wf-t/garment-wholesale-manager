using Microsoft.EntityFrameworkCore;

namespace Shanquan.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<SkuEntity> Skus => Set<SkuEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<ColorEntity> Colors => Set<ColorEntity>();
    public DbSet<StockBalanceEntity> StockBalances => Set<StockBalanceEntity>();
    public DbSet<SalesOrderEntity> SalesOrders => Set<SalesOrderEntity>();
    public DbSet<SalesDraftEntity> SalesDrafts => Set<SalesDraftEntity>();
    public DbSet<SalesOrderLineEntity> SalesOrderLines => Set<SalesOrderLineEntity>();
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<ShipmentLineEntity> ShipmentLines => Set<ShipmentLineEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    public DbSet<ReceivableEntryEntity> ReceivableEntries => Set<ReceivableEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.StyleNumber).IsUnique();
            entity.Property(x => x.StyleNumber).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.Season).HasMaxLength(50);
        });

        modelBuilder.Entity<SkuEntity>(entity =>
        {
            entity.ToTable("skus");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductId, x.ColorCode, x.SizeCode }).IsUnique();
            entity.HasIndex(x => x.Barcode).IsUnique();
        });

        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<WarehouseEntity>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(80);
        });

        modelBuilder.Entity<ColorEntity>(entity =>
        {
            entity.ToTable("colors");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ChineseName, x.EnglishName }).IsUnique();
            entity.Property(x => x.ChineseName).HasMaxLength(40);
            entity.Property(x => x.EnglishName).HasMaxLength(40);
            entity.Property(x => x.Hex).HasMaxLength(9);
        });

        modelBuilder.Entity<StockBalanceEntity>(entity =>
        {
            entity.ToTable("stock_balances");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WarehouseId, x.SkuId }).IsUnique();
            entity.Property(x => x.OnHand).HasColumnName("on_hand");
            entity.Property(x => x.Reserved).HasColumnName("reserved");
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_stock_on_hand_nonnegative", "on_hand >= 0");
                table.HasCheckConstraint("ck_stock_reserved_valid", "reserved >= 0 AND reserved <= on_hand");
            });
        });

        modelBuilder.Entity<SalesOrderEntity>(entity =>
        {
            entity.ToTable("sales_orders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        });

        modelBuilder.Entity<SalesDraftEntity>(entity =>
        {
            entity.ToTable("sales_drafts");
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<SalesOrderLineEntity>(entity =>
        {
            entity.ToTable("sales_order_lines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.SalesOrderId, x.SkuId }).IsUnique();
        });

        modelBuilder.Entity<ShipmentEntity>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => x.SalesOrderId).IsUnique();
            entity.Property(x => x.ExpressTrackingNumber).HasMaxLength(100);
        });

        modelBuilder.Entity<ShipmentLineEntity>(entity =>
        {
            entity.ToTable("shipment_lines");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ShipmentId, x.SalesOrderLineId }).IsUnique();
        });

        modelBuilder.Entity<StockMovementEntity>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WarehouseId, x.SkuId, x.CreatedAt });
        });

        modelBuilder.Entity<ReceivableEntryEntity>(entity =>
        {
            entity.ToTable("receivable_entries");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CustomerId, x.CreatedAt });
            entity.HasIndex(x => new { x.EntryType, x.ReferenceNumber }).IsUnique();
        });
    }
}
