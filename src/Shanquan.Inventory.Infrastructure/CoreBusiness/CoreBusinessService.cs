using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Shanquan.Inventory.Application.CoreBusiness;
using Shanquan.Inventory.Domain.Common;
using Shanquan.Inventory.Domain.Inventory;

namespace Shanquan.Inventory.Infrastructure.CoreBusiness;

public sealed class CoreBusinessService(IDbContextFactory<Persistence.InventoryDbContext> contextFactory)
    : ICoreBusinessService
{
    public async Task<BusinessWorkspace> GetWorkspaceAsync(
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var customers = await db.Customers.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(cancellationToken);
        var colors = await db.Colors.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ChineseName).ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.StyleNumber).ToListAsync(cancellationToken);
        var skus = await db.Skus.AsNoTracking().ToListAsync(cancellationToken);
        var balances = await db.StockBalances.AsNoTracking().ToListAsync(cancellationToken);
        var allOrders = await db.SalesOrders.AsNoTracking().ToListAsync(cancellationToken);
        var orders = allOrders.OrderByDescending(x => x.CreatedAt).Take(100).ToList();
        var orderIds = orders.Select(x => x.Id).ToList();
        var orderLines = await db.SalesOrderLines.AsNoTracking()
            .Where(x => orderIds.Contains(x.SalesOrderId))
            .ToListAsync(cancellationToken);
        var shipments = await db.Shipments.AsNoTracking()
            .Where(x => orderIds.Contains(x.SalesOrderId))
            .ToListAsync(cancellationToken);
        var allReceivables = await db.ReceivableEntries.AsNoTracking().ToListAsync(cancellationToken);
        var receivables = allReceivables.OrderByDescending(x => x.CreatedAt).ToList();
        var movements = (await db.StockMovements.AsNoTracking().ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAt).Take(200).ToList();

        var productById = products.ToDictionary(x => x.Id);
        var customerById = customers.ToDictionary(x => x.Id);
        var warehouseById = warehouses.ToDictionary(x => x.Id);
        var shipmentByOrderId = shipments.ToDictionary(x => x.SalesOrderId);
        var balanceBySku = balances
            .GroupBy(x => x.SkuId)
            .ToDictionary(x => x.Key, x => new { OnHand = x.Sum(y => y.OnHand), Reserved = x.Sum(y => y.Reserved) });

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var customerItems = customers.Select(customer =>
        {
            var customerOrders = allOrders.Where(x => x.CustomerId == customer.Id).ToList();
            var lastOrderAt = customerOrders.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.CreatedAt;
            var lastLedgerAt = allReceivables.Where(x => x.CustomerId == customer.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.CreatedAt;
            var lastTransactionAt = lastOrderAt is null ? lastLedgerAt : lastLedgerAt is null ? lastOrderAt : (lastOrderAt.Value > lastLedgerAt.Value ? lastOrderAt : lastLedgerAt);
            return new CustomerListItem(
                customer.Id,
                customer.Code,
                customer.Name,
                customer.Phone ?? "",
                customer.Address ?? "",
                customer.CreditLimitCents,
                allReceivables.Where(x => x.CustomerId == customer.Id).Sum(x => x.AmountCents),
                customerOrders.Sum(x => x.TotalAmountCents),
                customerOrders.Where(x => x.DocumentDate.LocalDateTime >= monthStart).Sum(x => x.TotalAmountCents),
                customer.CreatedAt,
                lastTransactionAt,
                customer.IsActive,
                Math.Max(0, customer.PaymentTermDays));
        }).ToList();

        var productItems = products.Select(product => new ProductListItem(
            product.Id,
            product.StyleNumber,
            product.Name,
            product.Category ?? "",
            product.Season ?? "",
            product.BasePriceCents,
            product.CostPriceCents,
            product.IsActive)).ToList();

        var skuItems = skus
            .Where(sku => productById.ContainsKey(sku.ProductId))
            .Select(sku =>
            {
                var product = productById[sku.ProductId];
                balanceBySku.TryGetValue(sku.Id, out var balance);
                return new SkuListItem(
                    sku.Id,
                    product.Id,
                    product.StyleNumber,
                    product.Name,
                    sku.ColorCode,
                    sku.SizeCode,
                    sku.Barcode ?? "",
                    product.BasePriceCents,
                    product.BasePriceCents,
                    balance?.OnHand ?? 0,
                    balance?.Reserved ?? 0);
            })
            .OrderBy(x => x.StyleNumber).ThenBy(x => x.ColorCode).ThenBy(x => x.SizeCode)
            .ToList();

        var orderItems = orders.Select(order => new SalesOrderListItem(
            order.Id,
            order.CustomerId,
            order.Number,
            customerById.GetValueOrDefault(order.CustomerId)?.Name ?? "未知客户",
            warehouseById.GetValueOrDefault(order.WarehouseId)?.Name ?? "未知仓库",
            order.Status,
            order.TotalQuantity,
            order.TotalAmountCents,
            order.DocumentDate,
            order.CreatedAt,
            order.PaymentStatus,
            order.ApprovedAt,
            order.PickingStartedAt,
            order.ReadyToShipAt,
            order.ShippedAt,
            order.SignedAt,
            shipmentByOrderId.GetValueOrDefault(order.Id)?.ExpressTrackingNumber ?? "")).ToList();

        var skuById = skus.ToDictionary(x => x.Id);
        var shippingLineItems = orderLines
            .Where(line => skuById.ContainsKey(line.SkuId))
            .Select(line =>
            {
                var sku = skuById[line.SkuId];
                var product = productById.GetValueOrDefault(sku.ProductId);
                return new ShippingOrderLineItem(
                    line.SalesOrderId,
                    line.Id,
                    line.SkuId,
                    product?.StyleNumber ?? "未知款号",
                    product?.Name ?? "未知商品",
                    sku.ColorCode,
                    sku.SizeCode,
                    sku.Barcode ?? "",
                    line.Quantity,
                    line.UnitPriceCents,
                    line.AmountCents);
            })
            .OrderBy(x => x.StyleNumber).ThenBy(x => x.ColorCode).ThenBy(x => x.SizeCode)
            .ToList();

        var stockItems = balances
            .Where(balance => productById.ContainsKey(skus.First(x => x.Id == balance.SkuId).ProductId))
            .Select(balance =>
            {
                var sku = skus.First(x => x.Id == balance.SkuId);
                var product = productById[sku.ProductId];
                return new StockListItem(
                    sku.Id,
                    balance.WarehouseId,
                    warehouseById.GetValueOrDefault(balance.WarehouseId)?.Name ?? "未知仓库",
                    product.StyleNumber,
                    product.Name,
                    product.Category ?? "",
                    product.Season ?? "",
                    sku.ColorCode,
                    sku.SizeCode,
                    balance.OnHand,
                    balance.Reserved);
            })
            .OrderBy(x => x.StyleNumber).ThenBy(x => x.Variant)
            .ToList();

        var receivableItems = receivables.Select(entry => new ReceivableListItem(
            entry.CustomerId,
            customerById.GetValueOrDefault(entry.CustomerId)?.Name ?? "未知客户",
            entry.EntryType,
            entry.ReferenceNumber,
            entry.AmountCents,
            entry.CreatedAt,
            entry.DueAt,
            entry.PaymentMethod ?? "",
            entry.Note ?? "")).ToList();
        var openReceivableItems = BuildOpenReceivables(receivables, customerById);
        var salesPeriodSummaries = allOrders
            .GroupBy(x => new { x.DocumentDate.LocalDateTime.Year, x.DocumentDate.LocalDateTime.Month })
            .Select(x => new SalesPeriodSummary(
                x.Key.Year,
                x.Key.Month,
                x.Sum(y => y.TotalAmountCents),
                x.Sum(y => y.TotalQuantity),
                x.Count()))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var movementItems = movements
            .Where(x => skuById.ContainsKey(x.SkuId))
            .Select(movement =>
            {
                var sku = skuById[movement.SkuId];
                var product = productById.GetValueOrDefault(sku.ProductId);
                return new StockMovementListItem(
                    movement.ReferenceNumber,
                    movement.MovementType,
                    warehouseById.GetValueOrDefault(movement.WarehouseId)?.Name ?? "未知仓库",
                    product?.StyleNumber ?? "未知款号",
                    $"{sku.ColorCode}/{sku.SizeCode}",
                    movement.OnHandChange,
                    movement.ReservedChange,
                    movement.OnHandAfter,
                    movement.ReservedAfter,
                    movement.CreatedAt);
            }).ToList();

        return new BusinessWorkspace(
            customerItems,
            warehouses.Select(x => new WarehouseListItem(x.Id, x.Code, x.Name)).ToList(),
            categories,
            colors.Select(x => new ColorListItem(x.Id, x.ChineseName, x.EnglishName, x.Hex)).ToList(),
            productItems,
            skuItems,
            orderItems,
            shippingLineItems,
            stockItems,
            movementItems,
            receivableItems,
            openReceivableItems,
            salesPeriodSummaries);
    }

    public async Task<Guid> SaveCustomerAsync(
        SaveCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new DomainRuleException("客户编号不能为空。");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("客户名称不能为空。");
        if (request.CreditLimitCents < 0) throw new DomainRuleException("信用额度不能小于零。");
        if (request.PaymentTermDays is < 0 or > 3650) throw new DomainRuleException("客户账期必须在 0 到 3650 天之间。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Customers.AnyAsync(x => x.Code == code && x.Id != request.Id, cancellationToken))
            throw new DomainRuleException($"客户编号 {code} 已存在。");

        Persistence.CustomerEntity customer;
        if (request.Id.HasValue)
        {
            customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new DomainRuleException("客户不存在。");
        }
        else
        {
            customer = new Persistence.CustomerEntity
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Customers.Add(customer);
        }

        customer.Code = code;
        customer.Name = name;
        customer.Phone = request.Phone.Trim();
        customer.Address = request.Address.Trim();
        customer.CreditLimitCents = request.CreditLimitCents;
        customer.PaymentTermDays = request.PaymentTermDays;
        customer.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }

    public async Task SetCustomerActiveAsync(
        Guid customerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId, cancellationToken)
            ?? throw new DomainRuleException("客户不存在。");
        customer.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> SaveProductAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var styleNumber = request.StyleNumber.Trim();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(styleNumber)) throw new DomainRuleException("款号不能为空。");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("商品名称不能为空。");
        if (request.BasePriceCents <= 0) throw new DomainRuleException("商品统一价必须大于零。");
        if (request.CostPriceCents <= 0) throw new DomainRuleException("商品成本价必须大于零。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Products.AnyAsync(x => x.StyleNumber == styleNumber && x.Id != request.Id, cancellationToken))
            throw new DomainRuleException($"款号 {styleNumber} 已存在。");

        Persistence.ProductEntity product;
        if (request.Id.HasValue)
        {
            product = await db.Products.SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new DomainRuleException("商品不存在。");
        }
        else
        {
            product = new Persistence.ProductEntity
            {
                Id = Guid.NewGuid(),
                StyleNumber = styleNumber,
                Name = name,
            };
            db.Products.Add(product);
        }

        product.StyleNumber = styleNumber;
        product.Name = name;
        product.Category = request.Category.Trim();
        product.Season = request.Season.Trim();
        product.BasePriceCents = request.BasePriceCents;
        product.CostPriceCents = request.CostPriceCents;
        product.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task<Guid> SaveWarehouseAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
        => await SaveWarehouseAsync(null, code, name, cancellationToken);

    public async Task<Guid> SaveWarehouseAsync(
        Guid? warehouseId,
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        code = code.Trim();
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new DomainRuleException("仓库编号不能为空。");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("仓库名称不能为空。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var warehouseWithCode = await db.Warehouses.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (!warehouseId.HasValue && warehouseWithCode is not null) warehouseId = warehouseWithCode.Id;
        if (warehouseWithCode is not null && warehouseWithCode.Id != warehouseId)
            throw new DomainRuleException($"仓库编号 {code} 已存在。");

        Persistence.WarehouseEntity warehouse;
        if (warehouseId.HasValue)
        {
            warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == warehouseId.Value, cancellationToken)
                ?? throw new DomainRuleException("仓库不存在。");
        }
        else
        {
            warehouse = new Persistence.WarehouseEntity { Id = Guid.NewGuid(), Code = code, Name = name };
            db.Warehouses.Add(warehouse);
        }

        warehouse.Code = code;
        warehouse.Name = name;
        warehouse.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return warehouse.Id;
    }

    public async Task DeleteWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == warehouseId, cancellationToken)
            ?? throw new DomainRuleException("仓库不存在。");
        var used = await db.StockBalances.AnyAsync(x => x.WarehouseId == warehouseId, cancellationToken)
            || await db.StockMovements.AnyAsync(x => x.WarehouseId == warehouseId, cancellationToken)
            || await db.SalesOrders.AnyAsync(x => x.WarehouseId == warehouseId, cancellationToken);
        if (used) throw new DomainRuleException("该仓库已有库存或业务流水，不能删除；可以保留仓库并修改名称。");
        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        category = category.Trim();
        if (string.IsNullOrWhiteSpace(category)) throw new DomainRuleException("品类名称不能为空。");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = (await db.Categories.ToListAsync(cancellationToken))
            .SingleOrDefault(x => string.Equals(x.Name, category, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            db.Categories.Add(new Persistence.CategoryEntity { Id = Guid.NewGuid(), Name = category });
        else
            existing.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RenameCategoryAsync(
        string currentName,
        string newName,
        CancellationToken cancellationToken = default)
    {
        currentName = currentName.Trim();
        newName = newName.Trim();
        if (string.IsNullOrWhiteSpace(newName)) throw new DomainRuleException("品类名称不能为空。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var categories = await db.Categories.ToListAsync(cancellationToken);
        var category = categories.SingleOrDefault(x => string.Equals(x.Name, currentName, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainRuleException("品类不存在。");
        if (categories.Any(x => !string.Equals(x.Name, currentName, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleException($"品类“{newName}”已存在。");
        if (category.Name == newName) return;

        var products = await db.Products
            .Where(x => x.Category == category.Name)
            .ToListAsync(cancellationToken);
        foreach (var product in products) product.Category = newName;
        category.Name = newName;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CategoryUsage> GetCategoryUsageAsync(string category, CancellationToken cancellationToken = default)
    {
        category = category.Trim();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var productIds = await db.Products.Where(x => x.Category == category).Select(x => x.Id).ToListAsync(cancellationToken);
        var skuIds = await db.Skus.Where(x => productIds.Contains(x.ProductId)).Select(x => x.Id).ToListAsync(cancellationToken);
        var hasSales = skuIds.Count > 0 && await db.SalesOrderLines.AnyAsync(x => skuIds.Contains(x.SkuId), cancellationToken);
        return new CategoryUsage(category, productIds.Count, hasSales);
    }

    public async Task DeleteCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        category = category.Trim();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var products = await db.Products
            .Where(x => x.Category == category)
            .ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToList();
        var skus = await db.Skus
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var skuIds = skus.Select(x => x.Id).ToList();

        if (skuIds.Count > 0 && await db.SalesOrderLines.AnyAsync(x => skuIds.Contains(x.SkuId), cancellationToken))
            throw new DomainRuleException("该品类下的商品已经产生销售单，不能删除。");

        if (skuIds.Count > 0)
        {
            var movements = await db.StockMovements.Where(x => skuIds.Contains(x.SkuId)).ToListAsync(cancellationToken);
            var balances = await db.StockBalances.Where(x => skuIds.Contains(x.SkuId)).ToListAsync(cancellationToken);
            db.StockMovements.RemoveRange(movements);
            db.StockBalances.RemoveRange(balances);
            db.Skus.RemoveRange(skus);
        }
        db.Products.RemoveRange(products);
        var categoryEntity = await db.Categories.SingleOrDefaultAsync(x => x.Name == category, cancellationToken)
            ?? throw new DomainRuleException("品类不存在或已被删除。");
        db.Categories.Remove(categoryEntity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Guid> SaveColorAsync(
        string chineseName,
        string englishName,
        string hex,
        CancellationToken cancellationToken = default)
    {
        chineseName = chineseName.Trim();
        englishName = englishName.Trim().ToUpperInvariant();
        hex = hex.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(chineseName) || string.IsNullOrWhiteSpace(englishName))
            throw new DomainRuleException("颜色的中文名和英文名不能为空。");
        if (hex.Length is not (7 or 9) || !hex.StartsWith('#')) throw new DomainRuleException("颜色值格式不正确。");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var color = await db.Colors.SingleOrDefaultAsync(x => x.ChineseName == chineseName && x.EnglishName == englishName, cancellationToken);
        if (color is null)
        {
            color = new Persistence.ColorEntity { Id = Guid.NewGuid(), ChineseName = chineseName, EnglishName = englishName, Hex = hex };
            db.Colors.Add(color);
        }
        else
        {
            color.Hex = hex;
            color.IsActive = true;
        }
        await db.SaveChangesAsync(cancellationToken);
        return color.Id;
    }

    public async Task<Guid> SaveColorNameAsync(
        Guid? colorId,
        string name,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException("颜色名称不能为空。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var colors = await db.Colors.ToListAsync(cancellationToken);
        if (colors.Any(x => x.Id != colorId &&
            (string.Equals(x.ChineseName, name, StringComparison.OrdinalIgnoreCase)
             || string.Equals(x.EnglishName, name, StringComparison.OrdinalIgnoreCase))))
            throw new DomainRuleException($"颜色“{name}”已存在。");

        Persistence.ColorEntity color;
        string[] previousNames = [];
        if (colorId.HasValue)
        {
            color = colors.SingleOrDefault(x => x.Id == colorId.Value)
                ?? throw new DomainRuleException("颜色不存在。");
            previousNames = [color.ChineseName, color.EnglishName, $"{color.ChineseName} / {color.EnglishName.ToUpperInvariant()}"];
        }
        else
        {
            color = new Persistence.ColorEntity
            {
                Id = Guid.NewGuid(),
                ChineseName = name,
                EnglishName = name,
                Hex = "#F2F0EE",
            };
            db.Colors.Add(color);
        }

        color.ChineseName = name;
        color.EnglishName = name;
        color.IsActive = true;
        if (previousNames.Length > 0)
        {
            var skus = await db.Skus.ToListAsync(cancellationToken);
            foreach (var sku in skus.Where(x => previousNames.Contains(x.ColorCode, StringComparer.OrdinalIgnoreCase)))
                sku.ColorCode = name;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return color.Id;
    }

    public async Task DeleteColorAsync(Guid colorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var color = await db.Colors.SingleOrDefaultAsync(x => x.Id == colorId, cancellationToken)
            ?? throw new DomainRuleException("颜色不存在。");
        if (await db.Skus.AnyAsync(x => x.ColorCode.Contains(color.ChineseName) || x.ColorCode.Contains(color.EnglishName), cancellationToken))
            throw new DomainRuleException("该颜色已有商品规格使用，不能删除。");
        db.Colors.Remove(color);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCategoriesAsync(
        IReadOnlyList<string> categories,
        CancellationToken cancellationToken = default)
    {
        var normalized = categories.Select(x => x.Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Categories.ToListAsync(cancellationToken);
        foreach (var category in existing)
            category.IsActive = normalized.Contains(category.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in normalized.Where(name => existing.All(x => !string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))))
            db.Categories.Add(new Persistence.CategoryEntity { Id = Guid.NewGuid(), Name = name });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddSkuAsync(AddSkuRequest request, CancellationToken cancellationToken = default)
    {
        var color = request.ColorCode.Trim();
        var size = request.SizeCode.Trim();
        var barcode = request.Barcode.Trim();
        if (string.IsNullOrWhiteSpace(color)) throw new DomainRuleException("颜色不能为空。");
        if (string.IsNullOrWhiteSpace(size)) throw new DomainRuleException("尺码不能为空。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken))
            throw new DomainRuleException("商品不存在或已停用。");
        if (await db.Skus.AnyAsync(
                x => x.ProductId == request.ProductId && x.ColorCode == color && x.SizeCode == size,
                cancellationToken))
            throw new DomainRuleException("该商品的颜色和尺码组合已经存在。");
        if (!string.IsNullOrWhiteSpace(barcode) && await db.Skus.AnyAsync(x => x.Barcode == barcode, cancellationToken))
            throw new DomainRuleException("条码已经存在。");

        var sku = new Persistence.SkuEntity
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            ColorCode = color,
            SizeCode = size,
            Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
        };
        db.Skus.Add(sku);
        if (request.WarehouseId.HasValue)
        {
            if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId.Value && x.IsActive, cancellationToken))
                throw new DomainRuleException("仓库不存在或已停用。");
            db.StockBalances.Add(new Persistence.StockBalanceEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = request.WarehouseId.Value,
                SkuId = sku.Id,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return sku.Id;
    }

    public async Task SaveSkuAsync(SaveSkuRequest request, CancellationToken cancellationToken = default)
    {
        var color = request.ColorCode.Trim();
        var size = request.SizeCode.Trim();
        var barcode = request.Barcode.Trim();
        if (string.IsNullOrWhiteSpace(color)) throw new DomainRuleException("颜色不能为空。");
        if (string.IsNullOrWhiteSpace(size)) throw new DomainRuleException("尺码不能为空。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sku = await db.Skus.SingleOrDefaultAsync(x => x.Id == request.SkuId, cancellationToken)
            ?? throw new DomainRuleException("商品规格不存在。");
        if (await db.Skus.AnyAsync(x => x.Id != sku.Id && x.ProductId == sku.ProductId && x.ColorCode == color && x.SizeCode == size, cancellationToken))
            throw new DomainRuleException("该商品的颜色和尺码组合已经存在。");
        if (!string.IsNullOrWhiteSpace(barcode) && await db.Skus.AnyAsync(x => x.Id != sku.Id && x.Barcode == barcode, cancellationToken))
            throw new DomainRuleException("条码已经存在。");

        sku.ColorCode = color;
        sku.SizeCode = size;
        sku.Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ReceiveStockAsync(
        ReceiveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0) throw new DomainRuleException("入库数量必须大于零。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await EnsureWarehouseAndSkuAsync(db, request.WarehouseId, request.SkuId, cancellationToken);

        var entity = await db.StockBalances.SingleOrDefaultAsync(
            x => x.WarehouseId == request.WarehouseId && x.SkuId == request.SkuId,
            cancellationToken);
        if (entity is null)
        {
            entity = new Persistence.StockBalanceEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = request.WarehouseId,
                SkuId = request.SkuId,
            };
            db.StockBalances.Add(entity);
        }

        var balance = new StockBalance(entity.WarehouseId, entity.SkuId, entity.OnHand, entity.Reserved);
        balance.Receive(request.Quantity);
        entity.OnHand = balance.OnHand;
        entity.Version++;
        var number = CreateNumber("RK", DateTimeOffset.UtcNow);
        db.StockMovements.Add(new Persistence.StockMovementEntity
        {
            Id = Guid.NewGuid(),
            WarehouseId = request.WarehouseId,
            SkuId = request.SkuId,
            MovementType = string.IsNullOrWhiteSpace(request.Note) ? "采购入库" : $"采购入库 · {request.Note.Trim()}",
            OnHandChange = request.Quantity,
            ReservedChange = 0,
            OnHandAfter = balance.OnHand,
            ReservedAfter = balance.Reserved,
            ReferenceNumber = number,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return number;
    }

    public async Task<string> AdjustStockAsync(
        AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetOnHand < 0) throw new DomainRuleException("盘点后的实际库存不能小于零。");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainRuleException("库存调整必须填写原因。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await EnsureWarehouseAndSkuAsync(db, request.WarehouseId, request.SkuId, cancellationToken);
        var entity = await db.StockBalances.SingleOrDefaultAsync(
            x => x.WarehouseId == request.WarehouseId && x.SkuId == request.SkuId,
            cancellationToken) ?? throw new DomainRuleException("该仓库还没有此商品库存，请先办理入库。");
        if (request.TargetOnHand < entity.Reserved)
            throw new DomainRuleException($"盘点数量不能低于已锁定库存 {entity.Reserved} 件。");

        var change = request.TargetOnHand - entity.OnHand;
        if (change == 0) throw new DomainRuleException("盘点数量与当前库存相同，无需调整。");
        entity.OnHand = request.TargetOnHand;
        entity.Version++;
        var number = CreateNumber("PD", DateTimeOffset.UtcNow);
        db.StockMovements.Add(new Persistence.StockMovementEntity
        {
            Id = Guid.NewGuid(),
            WarehouseId = request.WarehouseId,
            SkuId = request.SkuId,
            MovementType = $"库存盘点 · {request.Reason.Trim()}",
            OnHandChange = change,
            ReservedChange = 0,
            OnHandAfter = entity.OnHand,
            ReservedAfter = entity.Reserved,
            ReferenceNumber = number,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return number;
    }

    public async Task<SalesDraftData?> GetSalesDraftAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var drafts = await db.SalesDrafts.AsNoTracking().ToListAsync(cancellationToken);
        var draft = drafts.OrderByDescending(x => x.UpdatedAt).FirstOrDefault();
        if (draft is null) return null;
        List<CreateSalesOrderLine> lines;
        try
        {
            lines = JsonSerializer.Deserialize<List<CreateSalesOrderLine>>(draft.LinesJson) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }
        return new SalesDraftData(
            draft.CustomerId,
            draft.WarehouseId,
            lines,
            draft.DocumentDate,
            draft.FreightCents,
            draft.DiscountCents,
            draft.PaymentStatus,
            draft.UpdatedAt);
    }

    public async Task SaveSalesDraftAsync(SaveSalesDraftRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSettlement(request.FreightCents, request.DiscountCents, request.PaymentStatus);
        if (request.CustomerId == Guid.Empty) throw new DomainRuleException("请选择客户后再保存草稿。");
        if (request.WarehouseId == Guid.Empty) throw new DomainRuleException("请选择仓库后再保存草稿。");
        if (request.Lines.Count == 0) throw new DomainRuleException("草稿至少需要一件商品。");
        if (request.Lines.Any(x => x.Quantity <= 0)) throw new DomainRuleException("草稿中的销售数量必须大于零。");
        if (request.Lines.Any(x => x.UnitPriceCents is <= 0)) throw new DomainRuleException("草稿中的销售单价必须大于零。");
        if (request.Lines.GroupBy(x => x.SkuId).Any(x => x.Count() > 1))
            throw new DomainRuleException("草稿中存在重复的商品规格。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken))
            throw new DomainRuleException("客户不存在或已停用。");
        if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive, cancellationToken))
            throw new DomainRuleException("仓库不存在或已停用。");
        var skuIds = request.Lines.Select(x => x.SkuId).Distinct().ToList();
        if (await db.Skus.CountAsync(x => skuIds.Contains(x.Id), cancellationToken) != skuIds.Count)
            throw new DomainRuleException("草稿中存在无效商品规格。");
        var drafts = await db.SalesDrafts.ToListAsync(cancellationToken);
        var draft = drafts.OrderByDescending(x => x.UpdatedAt).FirstOrDefault();
        if (draft is null)
        {
            draft = new Persistence.SalesDraftEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                WarehouseId = request.WarehouseId,
                PaymentStatus = request.PaymentStatus,
                DocumentDate = request.DocumentDate,
                LinesJson = "[]",
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.SalesDrafts.Add(draft);
        }
        draft.CustomerId = request.CustomerId;
        draft.WarehouseId = request.WarehouseId;
        draft.DocumentDate = request.DocumentDate;
        draft.FreightCents = request.FreightCents;
        draft.DiscountCents = request.DiscountCents;
        draft.PaymentStatus = request.PaymentStatus;
        draft.LinesJson = JsonSerializer.Serialize(request.Lines);
        draft.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearSalesDraftAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var drafts = await db.SalesDrafts.ToListAsync(cancellationToken);
        db.SalesDrafts.RemoveRange(drafts);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> CreateAndReserveSalesOrderAsync(
        CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var groupedLines = request.Lines.GroupBy(x => x.SkuId).ToList();
        if (groupedLines.Any(group => group
                .Where(x => x.UnitPriceCents.HasValue)
                .Select(x => x.UnitPriceCents)
                .Distinct()
                .Count() > 1))
            throw new DomainRuleException("同一商品规格不能设置多个销售单价。");

        var requestedLines = groupedLines
            .Select(group => new CreateSalesOrderLine(
                group.Key,
                group.Sum(x => x.Quantity),
                group.FirstOrDefault(x => x.UnitPriceCents.HasValue)?.UnitPriceCents))
            .ToList();

        if (request.CustomerId == Guid.Empty) throw new DomainRuleException("请选择客户。");
        if (request.WarehouseId == Guid.Empty) throw new DomainRuleException("请选择仓库。");
        if (requestedLines.Count == 0) throw new DomainRuleException("销售单至少需要一件商品。");
        if (requestedLines.Any(x => x.Quantity <= 0)) throw new DomainRuleException("销售数量必须大于零。");
        if (requestedLines.Any(x => x.UnitPriceCents is <= 0)) throw new DomainRuleException("销售单价必须大于零。");
        ValidateSettlement(request.FreightCents, request.DiscountCents, request.PaymentStatus);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new DomainRuleException("客户不存在或已停用。");
        if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive, cancellationToken))
            throw new DomainRuleException("仓库不存在或已停用。");

        var skuIds = requestedLines.Select(x => x.SkuId).ToList();
        var skus = await db.Skus.Where(x => skuIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (skus.Count != skuIds.Count) throw new DomainRuleException("销售单中存在无效商品规格。");

        var productIds = skus.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken);
        var balances = await db.StockBalances
            .Where(x => x.WarehouseId == request.WarehouseId && skuIds.Contains(x.SkuId))
            .ToDictionaryAsync(x => x.SkuId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var number = CreateNumber("XS", now);
        var order = new Persistence.SalesOrderEntity
        {
            Id = Guid.NewGuid(),
            Number = number,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            Status = "待配货",
            FreightCents = request.FreightCents,
            DiscountCents = request.DiscountCents,
            PaymentStatus = request.PaymentStatus,
            DocumentDate = request.DocumentDate,
            CreatedAt = now,
            ApprovedAt = now,
        };

        foreach (var requestedLine in requestedLines)
        {
            if (!balances.TryGetValue(requestedLine.SkuId, out var balanceEntity))
                throw new DomainRuleException("所选仓库没有该商品库存。");

            var balance = new StockBalance(balanceEntity.WarehouseId, balanceEntity.SkuId, balanceEntity.OnHand, balanceEntity.Reserved);
            balance.Reserve(requestedLine.Quantity);
            balanceEntity.Reserved = balance.Reserved;
            balanceEntity.Version++;

            var sku = skus.Single(x => x.Id == requestedLine.SkuId);
            if (!products.TryGetValue(sku.ProductId, out var product))
                throw new DomainRuleException("商品不存在或已停用。");
            var unitPrice = requestedLine.UnitPriceCents ?? product.BasePriceCents;
            if (unitPrice <= 0) throw new DomainRuleException($"款号 {product.StyleNumber} 尚未设置有效价格。");
            var amount = checked(unitPrice * requestedLine.Quantity);

            db.SalesOrderLines.Add(new Persistence.SalesOrderLineEntity
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                SkuId = requestedLine.SkuId,
                Quantity = requestedLine.Quantity,
                UnitPriceCents = unitPrice,
                AmountCents = amount,
            });
            db.StockMovements.Add(new Persistence.StockMovementEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = request.WarehouseId,
                SkuId = requestedLine.SkuId,
                MovementType = "销售锁库",
                OnHandChange = 0,
                ReservedChange = requestedLine.Quantity,
                OnHandAfter = balance.OnHand,
                ReservedAfter = balance.Reserved,
                ReferenceNumber = number,
                CreatedAt = now,
            });

            order.TotalQuantity = checked(order.TotalQuantity + requestedLine.Quantity);
            order.GoodsAmountCents = checked(order.GoodsAmountCents + amount);
        }

        if (order.DiscountCents > checked(order.GoodsAmountCents + order.FreightCents))
            throw new DomainRuleException("优惠金额不能大于商品金额与运费之和。");
        order.TotalAmountCents = checked(order.GoodsAmountCents + order.FreightCents - order.DiscountCents);
        order.ReceivedAmountCents = order.PaymentStatus == "已收款" ? order.TotalAmountCents : 0;

        if (order.PaymentStatus == "未收款" && customer.CreditLimitCents > 0)
        {
            var currentReceivable = await db.ReceivableEntries
                .Where(x => x.CustomerId == customer.Id)
                .SumAsync(x => (long?)x.AmountCents, cancellationToken) ?? 0;
            var pendingReceivable = await db.SalesOrders
                .Where(x => x.CustomerId == customer.Id
                            && (x.Status == "待配货" || x.Status == "配货中" || x.Status == "待发货" || x.Status == "已锁库")
                            && x.PaymentStatus == "未收款")
                .SumAsync(x => (long?)(x.TotalAmountCents - x.ReceivedAmountCents), cancellationToken) ?? 0;
            if (checked(currentReceivable + pendingReceivable + order.TotalAmountCents) > customer.CreditLimitCents)
                throw new DomainRuleException("本单欠款将超过客户信用额度，请调整金额或选择已收款。");
        }

        db.SalesOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return number;
    }

    public async Task StartPickingAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.SalesOrders.SingleOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken)
            ?? throw new DomainRuleException("销售单不存在。");
        if (order.Status is not ("待配货" or "已锁库"))
            throw new DomainRuleException("只有待配货销售单可以开始配货。");
        order.Status = "配货中";
        order.PickingStartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompletePickingAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.SalesOrders.SingleOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken)
            ?? throw new DomainRuleException("销售单不存在。");
        if (order.Status != "配货中") throw new DomainRuleException("只有配货中的销售单可以完成配货。");
        var lines = await db.SalesOrderLines.Where(x => x.SalesOrderId == salesOrderId).ToListAsync(cancellationToken);
        if (lines.Count == 0) throw new DomainRuleException("销售单没有商品明细。");
        order.Status = "待发货";
        order.ReadyToShipAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ShipSalesOrderAsync(
        Guid salesOrderId,
        string? expressTrackingNumber = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedExpressTrackingNumber = expressTrackingNumber?.Trim() ?? "";
        if (normalizedExpressTrackingNumber.Length > 100)
            throw new DomainRuleException("快递单号不能超过 100 个字符。");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var order = await db.SalesOrders.SingleOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken)
            ?? throw new DomainRuleException("销售单不存在。");
        if (order.Status != "待发货") throw new DomainRuleException("只有完成配货的待发货销售单可以发货。");

        var lines = await db.SalesOrderLines.Where(x => x.SalesOrderId == order.Id).ToListAsync(cancellationToken);
        if (lines.Count == 0) throw new DomainRuleException("销售单没有可发货明细。");

        var skuIds = lines.Select(x => x.SkuId).ToList();
        var balances = await db.StockBalances
            .Where(x => x.WarehouseId == order.WarehouseId && skuIds.Contains(x.SkuId))
            .ToDictionaryAsync(x => x.SkuId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var shipmentNumber = CreateNumber("FH", now);
        var shipment = new Persistence.ShipmentEntity
        {
            Id = Guid.NewGuid(),
            Number = shipmentNumber,
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,
            WarehouseId = order.WarehouseId,
            TotalQuantity = order.TotalQuantity,
            TotalAmountCents = order.TotalAmountCents,
            ExpressTrackingNumber = normalizedExpressTrackingNumber.Length == 0 ? null : normalizedExpressTrackingNumber,
            ShippedAt = now,
        };

        foreach (var line in lines)
        {
            if (!balances.TryGetValue(line.SkuId, out var balanceEntity))
                throw new DomainRuleException("库存记录缺失，无法发货。");

            var quantity = line.Quantity - line.ShippedQuantity;
            var balance = new StockBalance(balanceEntity.WarehouseId, balanceEntity.SkuId, balanceEntity.OnHand, balanceEntity.Reserved);
            balance.ShipReserved(quantity);
            balanceEntity.OnHand = balance.OnHand;
            balanceEntity.Reserved = balance.Reserved;
            balanceEntity.Version++;
            line.ShippedQuantity = line.Quantity;

            db.ShipmentLines.Add(new Persistence.ShipmentLineEntity
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                SalesOrderLineId = line.Id,
                SkuId = line.SkuId,
                Quantity = quantity,
            });
            db.StockMovements.Add(new Persistence.StockMovementEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = order.WarehouseId,
                SkuId = line.SkuId,
                MovementType = "销售发货",
                OnHandChange = -quantity,
                ReservedChange = -quantity,
                OnHandAfter = balance.OnHand,
                ReservedAfter = balance.Reserved,
                ReferenceNumber = shipmentNumber,
                CreatedAt = now,
            });
        }

        order.Status = "已发货";
        order.ShippedAt = now;
        db.Shipments.Add(shipment);
        var receivableAmount = order.TotalAmountCents - order.ReceivedAmountCents;
        if (receivableAmount > 0)
        {
            var paymentTermDays = await db.Customers.Where(x => x.Id == order.CustomerId)
                .Select(x => x.PaymentTermDays)
                .SingleAsync(cancellationToken);
            db.ReceivableEntries.Add(new Persistence.ReceivableEntryEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = order.CustomerId,
                EntryType = "销售应收",
                AmountCents = receivableAmount,
                ReferenceNumber = shipmentNumber,
                CreatedAt = now,
                DueAt = now.AddDays(Math.Max(0, paymentTermDays)),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return shipmentNumber;
    }

    public async Task ConfirmSalesOrderReceiptAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.SalesOrders.SingleOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken)
            ?? throw new DomainRuleException("销售单不存在。");
        if (order.Status != "已发货") throw new DomainRuleException("只有已发货销售单可以确认客户签收。");
        order.Status = "已签收";
        order.SignedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> RecordCustomerPaymentAsync(
        RecordCustomerPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AmountCents <= 0) throw new DomainRuleException("收款金额必须大于零。");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new DomainRuleException("客户不存在。");
        var balance = await db.ReceivableEntries.Where(x => x.CustomerId == request.CustomerId)
            .SumAsync(x => (long?)x.AmountCents, cancellationToken) ?? 0;
        if (balance <= 0) throw new DomainRuleException("该客户当前没有未结欠款。");
        if (request.AmountCents > balance) throw new DomainRuleException("收款金额不能大于客户当前欠款。");
        var number = CreateNumber("SK", request.ReceivedAt);
        db.ReceivableEntries.Add(new Persistence.ReceivableEntryEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            EntryType = "收款",
            AmountCents = -request.AmountCents,
            ReferenceNumber = number,
            CreatedAt = request.ReceivedAt,
            PaymentMethod = request.PaymentMethod.Trim(),
            Note = request.Note.Trim(),
        });
        await db.SaveChangesAsync(cancellationToken);
        return number;
    }

    private static IReadOnlyList<OpenReceivableListItem> BuildOpenReceivables(
        IReadOnlyList<Persistence.ReceivableEntryEntity> entries,
        IReadOnlyDictionary<Guid, Persistence.CustomerEntity> customerById)
    {
        var result = new List<OpenReceivableListItem>();
        foreach (var customerEntries in entries.GroupBy(x => x.CustomerId))
        {
            var availablePayments = -customerEntries.Where(x => x.AmountCents < 0).Sum(x => x.AmountCents);
            foreach (var charge in customerEntries.Where(x => x.AmountCents > 0).OrderBy(x => x.CreatedAt))
            {
                var applied = Math.Min(availablePayments, charge.AmountCents);
                availablePayments -= applied;
                var outstanding = charge.AmountCents - applied;
                if (outstanding <= 0) continue;
                var customer = customerById.GetValueOrDefault(charge.CustomerId);
                var dueAt = charge.DueAt ?? charge.CreatedAt.AddDays(Math.Max(0, customer?.PaymentTermDays ?? 30));
                result.Add(new OpenReceivableListItem(
                    charge.CustomerId,
                    customer?.Name ?? "未知客户",
                    charge.ReferenceNumber,
                    outstanding,
                    charge.CreatedAt,
                    dueAt));
            }
        }
        return result.OrderBy(x => x.DueAt).ThenBy(x => x.CreatedAt).ToList();
    }

    private static string CreateNumber(string prefix, DateTimeOffset timestamp)
        => $"{prefix}{timestamp:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    private static void ValidateSettlement(long freightCents, long discountCents, string paymentStatus)
    {
        if (freightCents < 0) throw new DomainRuleException("运费不能小于零。");
        if (discountCents < 0) throw new DomainRuleException("优惠金额不能小于零。");
        if (paymentStatus is not ("已收款" or "未收款"))
            throw new DomainRuleException("收款状态无效。");
    }

    private static async Task EnsureWarehouseAndSkuAsync(
        Persistence.InventoryDbContext db,
        Guid warehouseId,
        Guid skuId,
        CancellationToken cancellationToken)
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
            throw new DomainRuleException("仓库不存在或已停用。");
        if (!await db.Skus.AnyAsync(x => x.Id == skuId, cancellationToken))
            throw new DomainRuleException("商品规格不存在。");
    }
}
