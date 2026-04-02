using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IOMS.Infrastructure.Identity;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // Create roles
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var tenantId = Guid.Parse(AppConstants.DefaultTenantId);

        // Create admin user
        var adminEmail = "admin@ioms.local";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Department = "IT",
                TenantId = tenantId,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // Seed Chart of Accounts
        if (!await context.Accounts.IgnoreQueryFilters().AnyAsync(a => a.TenantId == tenantId))
        {
            var accounts = new List<Account>
            {
                new() { Code = AccountCodes.Cash, Name = "Cash", AccountType = AccountType.Asset, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.AccountsReceivable, Name = "Accounts Receivable", AccountType = AccountType.Asset, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.Inventory, Name = "Inventory", AccountType = AccountType.Asset, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.AccountsPayable, Name = "Accounts Payable", AccountType = AccountType.Liability, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.OwnersEquity, Name = "Owner's Equity", AccountType = AccountType.Equity, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.SalesRevenue, Name = "Sales Revenue", AccountType = AccountType.Revenue, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.SalesReturns, Name = "Sales Returns", AccountType = AccountType.Revenue, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.CostOfGoodsSold, Name = "Cost of Goods Sold", AccountType = AccountType.Expense, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.OperatingExpenses, Name = "Operating Expenses", AccountType = AccountType.Expense, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.InventoryAdjustment, Name = "Inventory Adjustment", AccountType = AccountType.Expense, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
                new() { Code = AccountCodes.ExchangeGainLoss, Name = "Exchange Gain/Loss", AccountType = AccountType.Expense, TenantId = tenantId, IsSystemAccount = true, CreatedBy = "system" },
            };
            context.Accounts.AddRange(accounts);
        }

        // Seed default currency
        if (!await context.Currencies.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId))
        {
            context.Currencies.Add(new Currency
            {
                Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2,
                IsBaseCurrency = true, TenantId = tenantId, CreatedBy = "system"
            });
        }

        // Seed default warehouse
        if (!await context.Warehouses.IgnoreQueryFilters().AnyAsync(w => w.TenantId == tenantId))
        {
            context.Warehouses.Add(new Warehouse
            {
                Name = "Main Warehouse", Code = "WH-001", Location = "Default Location",
                TenantId = tenantId, CreatedBy = "system"
            });
        }

        // Seed default category
        if (!await context.Categories.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId))
        {
            context.Categories.Add(new Category
            {
                Name = "General", Description = "Default product category",
                TenantId = tenantId, CreatedBy = "system"
            });
        }

        // Seed default UoM
        if (!await context.UnitsOfMeasure.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId))
        {
            context.UnitsOfMeasure.AddRange(
                new UnitOfMeasure { Name = "Each", Abbreviation = "ea", IsBaseUnit = true, TenantId = tenantId, CreatedBy = "system" },
                new UnitOfMeasure { Name = "Box", Abbreviation = "box", IsBaseUnit = false, TenantId = tenantId, CreatedBy = "system" },
                new UnitOfMeasure { Name = "Carton", Abbreviation = "ctn", IsBaseUnit = false, TenantId = tenantId, CreatedBy = "system" },
                new UnitOfMeasure { Name = "Kilogram", Abbreviation = "kg", IsBaseUnit = false, TenantId = tenantId, CreatedBy = "system" },
                new UnitOfMeasure { Name = "Liter", Abbreviation = "L", IsBaseUnit = false, TenantId = tenantId, CreatedBy = "system" }
            );
        }

        // Seed default tax jurisdiction & rate
        if (!await context.TaxJurisdictions.IgnoreQueryFilters().AnyAsync(t => t.TenantId == tenantId))
        {
            var jurisdiction = new TaxJurisdiction
            {
                Name = "Default", Code = "DEF", Country = "US",
                TenantId = tenantId, CreatedBy = "system"
            };
            context.TaxJurisdictions.Add(jurisdiction);
            await context.SaveChangesAsync();

            context.TaxRates.Add(new TaxRate
            {
                Name = "Standard VAT", Rate = 10m, TaxType = TaxType.VAT,
                TaxJurisdictionId = jurisdiction.Id,
                EffectiveFrom = new DateTime(2024, 1, 1),
                TenantId = tenantId, CreatedBy = "system"
            });
        }

        await context.SaveChangesAsync();

        // Seed 20 dummy records in all major tables
        await SeedDummyDataAsync(context, tenantId);
    }

    private static async Task SeedDummyDataAsync(AppDbContext context, Guid tenantId)
    {
        // Check if dummy data already seeded (use Products as indicator)
        if (await context.Products.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId) >= 20)
            return;

        var cb = "system";
        var rng = new Random(42); // fixed seed for reproducibility

        // --- Currencies (add more beyond default USD) ---
        var usd = await context.Currencies.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId && c.Code == "USD");
        var extraCurrencies = new List<Currency>();
        string[][] currData = [["EUR","Euro","€"],["GBP","British Pound","£"],["JPY","Japanese Yen","¥"],["CAD","Canadian Dollar","C$"],["AUD","Australian Dollar","A$"],["CHF","Swiss Franc","Fr"],["INR","Indian Rupee","₹"],["CNY","Chinese Yuan","¥"],["BRL","Brazilian Real","R$"],["KRW","South Korean Won","₩"],["SGD","Singapore Dollar","S$"],["MXN","Mexican Peso","$"],["NZD","New Zealand Dollar","NZ$"],["SEK","Swedish Krona","kr"],["NOK","Norwegian Krone","kr"],["DKK","Danish Krone","kr"],["HKD","Hong Kong Dollar","HK$"],["ZAR","South African Rand","R"],["AED","UAE Dirham","د.إ"]];
        if (!await context.Currencies.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId && c.Code == "EUR"))
        {
            foreach (var cd in currData)
                extraCurrencies.Add(new Currency { Code = cd[0], Name = cd[1], Symbol = cd[2], DecimalPlaces = cd[0] == "JPY" || cd[0] == "KRW" ? 0 : 2, TenantId = tenantId, CreatedBy = cb });
            context.Currencies.AddRange(extraCurrencies);
            await context.SaveChangesAsync();
        }

        // --- Categories (20 total incl existing "General") ---
        var existingCat = await context.Categories.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId);
        var catNames = new[] { "Electronics", "Furniture", "Office Supplies", "Raw Materials", "Packaging", "Tools & Hardware", "Safety Equipment", "Cleaning Supplies", "Automotive Parts", "Medical Supplies", "Food & Beverages", "Textiles", "Chemicals", "Machinery", "Plumbing", "Electrical", "Building Materials", "Garden & Outdoor", "Sports Equipment" };
        var categories = new List<Category> { existingCat };
        if (await context.Categories.IgnoreQueryFilters().CountAsync(c => c.TenantId == tenantId) < 20)
        {
            foreach (var cn in catNames)
            {
                var cat = new Category { Name = cn, Description = $"{cn} products", TenantId = tenantId, CreatedBy = cb };
                categories.Add(cat);
            }
            context.Categories.AddRange(categories.Skip(1));
            await context.SaveChangesAsync();
        }
        else
        {
            categories = await context.Categories.IgnoreQueryFilters().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).ToListAsync();
        }

        // --- Warehouses (20 total incl existing) ---
        var warehouses = await context.Warehouses.IgnoreQueryFilters().Where(w => w.TenantId == tenantId).ToListAsync();
        if (warehouses.Count < 20)
        {
            var whNames = new[] { "North Warehouse", "South Warehouse", "East Warehouse", "West Warehouse", "Downtown Depot", "Airport Storage", "Port Facility", "Cold Storage A", "Cold Storage B", "Overflow Storage", "Returns Center", "Distribution Hub", "Regional DC-1", "Regional DC-2", "Regional DC-3", "Express Bay", "Bulk Storage", "Hazmat Store", "Tech Vault" };
            for (int i = 0; i < whNames.Length; i++)
            {
                var wh = new Warehouse { Name = whNames[i], Code = $"WH-{i + 2:D3}", Location = $"Location {i + 2}", Address = $"{100 + i * 10} Industrial Ave, City {i + 1}", TenantId = tenantId, CreatedBy = cb };
                warehouses.Add(wh);
            }
            context.Warehouses.AddRange(warehouses.Skip(1));
            await context.SaveChangesAsync();
        }

        // --- Products (20) ---
        var products = new List<Product>();
        if (await context.Products.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId) < 20)
        {
            string[][] prodData = [
                ["Wireless Mouse","SKU-0001","8901234560001"],["Mechanical Keyboard","SKU-0002","8901234560002"],["USB-C Hub","SKU-0003","8901234560003"],
                ["27\" Monitor","SKU-0004","8901234560004"],["Desk Lamp","SKU-0005","8901234560005"],["Ergonomic Chair","SKU-0006","8901234560006"],
                ["Standing Desk","SKU-0007","8901234560007"],["Webcam HD","SKU-0008","8901234560008"],["Noise-Cancelling Headphones","SKU-0009","8901234560009"],
                ["Laptop Stand","SKU-0010","8901234560010"],["Printer Paper A4","SKU-0011","8901234560011"],["Whiteboard Marker Set","SKU-0012","8901234560012"],
                ["Cable Management Kit","SKU-0013","8901234560013"],["External SSD 1TB","SKU-0014","8901234560014"],["Wireless Charger","SKU-0015","8901234560015"],
                ["Power Strip 6-Outlet","SKU-0016","8901234560016"],["Desk Organizer","SKU-0017","8901234560017"],["Anti-Fatigue Mat","SKU-0018","8901234560018"],
                ["Document Scanner","SKU-0019","8901234560019"],["Label Printer","SKU-0020","8901234560020"]
            ];
            decimal[] costs = [12.50m,35.00m,18.00m,180.00m,15.00m,220.00m,350.00m,25.00m,95.00m,20.00m,3.50m,5.00m,8.00m,55.00m,12.00m,10.00m,7.50m,22.00m,120.00m,65.00m];
            decimal[] sells = [24.99m,69.99m,34.99m,349.99m,29.99m,449.99m,699.99m,49.99m,199.99m,39.99m,7.99m,12.99m,16.99m,109.99m,24.99m,19.99m,14.99m,44.99m,249.99m,129.99m];
            for (int i = 0; i < 20; i++)
            {
                products.Add(new Product
                {
                    Name = prodData[i][0], SKU = prodData[i][1], Barcode = prodData[i][2],
                    Description = $"High quality {prodData[i][0].ToLower()}",
                    CostPrice = costs[i], SellingPrice = sells[i],
                    ReorderLevel = rng.Next(5, 25), MinimumOrderQuantity = rng.Next(1, 5),
                    CategoryId = categories[i % categories.Count].Id,
                    Weight = Math.Round((decimal)(rng.NextDouble() * 10 + 0.1), 2),
                    Volume = Math.Round((decimal)(rng.NextDouble() * 5 + 0.1), 2),
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
        else
        {
            products = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tenantId).OrderBy(p => p.SKU).Take(20).ToListAsync();
        }

        // --- Customers (20) ---
        var customers = new List<Customer>();
        if (await context.Customers.IgnoreQueryFilters().CountAsync(c => c.TenantId == tenantId) < 20)
        {
            string[] custNames = ["Acme Corp","Global Industries","TechStart LLC","Prime Solutions","Nexus Trading","Blue Sky Imports","Green Valley Co","Summit Enterprises","Pacific Rim Ltd","Atlas Distributors","Metro Supplies Inc","Horizon Group","Phoenix Materials","Silverline Corp","Diamond Services","Quantum Retail","Edge Computing Co","Pinnacle Foods","Coastal Shipping","Harbor Logistics"];
            for (int i = 0; i < 20; i++)
            {
                customers.Add(new Customer
                {
                    Name = custNames[i],
                    Email = $"contact@{custNames[i].ToLower().Replace(" ", "").Replace(".", "").Replace("'", "")}.com",
                    Phone = $"+1-555-{1000 + i:D4}",
                    Address = $"{100 + i * 5} Commerce St",
                    City = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "Austin", "Jacksonville", "Fort Worth", "Columbus", "Charlotte", "Indianapolis", "San Francisco", "Seattle", "Denver", "Nashville", "Portland" }[i],
                    State = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "TX", "FL", "TX", "OH", "NC", "IN", "CA", "WA", "CO", "TN", "OR" }[i],
                    Country = "US",
                    PostalCode = $"{10001 + i * 100}",
                    CreditLimit = (i + 1) * 5000m,
                    PaymentTerms = new[] { "Net 30", "Net 15", "Net 60", "Net 45", "COD" }[i % 5],
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();
        }
        else
        {
            customers = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).Take(20).ToListAsync();
        }

        // --- Suppliers (20) ---
        var suppliers = new List<Supplier>();
        if (await context.Suppliers.IgnoreQueryFilters().CountAsync(s => s.TenantId == tenantId) < 20)
        {
            string[] suppNames = ["TechParts Intl","OfficePlus Co","FurniPro Mfg","RawMat Supply","PackRight Inc","ToolMaster Ltd","SafeGuard Corp","CleanPro Supplies","AutoParts Direct","MedEquip Wholesale","Gourmet Dist","TextileCraft","ChemSource Ltd","HeavyDuty Mfg","PlumbWorks Co","ElectroParts Inc","BuildRight Supply","GreenGarden Co","SportsGear Mfg","TechVault Supply"];
            for (int i = 0; i < 20; i++)
            {
                suppliers.Add(new Supplier
                {
                    Name = suppNames[i],
                    Email = $"sales@{suppNames[i].ToLower().Replace(" ", "").Replace(".", "").Replace("'", "")}.com",
                    Phone = $"+1-555-{2000 + i:D4}",
                    Address = $"{200 + i * 10} Supplier Blvd",
                    City = "Industry City",
                    State = "CA", Country = "US", PostalCode = $"{90001 + i * 10}",
                    PaymentTerms = new[] { "Net 30", "Net 45", "Net 60", "Net 15", "COD" }[i % 5],
                    LeadTimeDays = rng.Next(3, 21),
                    Rating = Math.Round(3.0m + (decimal)(rng.NextDouble() * 2), 1),
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            context.Suppliers.AddRange(suppliers);
            await context.SaveChangesAsync();
        }
        else
        {
            suppliers = await context.Suppliers.IgnoreQueryFilters().Where(s => s.TenantId == tenantId).OrderBy(s => s.Name).Take(20).ToListAsync();
        }

        // --- Inventory (20 records - one per product in main warehouse) ---
        if (await context.Inventories.IgnoreQueryFilters().CountAsync(inv => inv.TenantId == tenantId) < 20)
        {
            var mainWh = warehouses.First();
            for (int i = 0; i < 20; i++)
            {
                context.Inventories.Add(new Inventory
                {
                    ProductId = products[i].Id,
                    WarehouseId = mainWh.Id,
                    Quantity = rng.Next(10, 500),
                    ReservedQuantity = rng.Next(0, 10),
                    LastStockDate = DateTime.UtcNow.AddDays(-rng.Next(1, 60)),
                    BinLocation = $"A{i / 5 + 1}-R{i % 5 + 1}-S{rng.Next(1, 10)}",
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Stock Movements (20) ---
        if (await context.StockMovements.IgnoreQueryFilters().CountAsync(m => m.TenantId == tenantId) < 20)
        {
            var types = new[] { StockMovementType.In, StockMovementType.Out, StockMovementType.Adjustment, StockMovementType.Transfer, StockMovementType.In };
            for (int i = 0; i < 20; i++)
            {
                context.StockMovements.Add(new StockMovement
                {
                    ProductId = products[i % 20].Id,
                    WarehouseId = warehouses[i % warehouses.Count].Id,
                    Type = types[i % types.Length],
                    Quantity = rng.Next(1, 100),
                    Reference = $"SM-{2026}{i + 1:D4}",
                    Notes = $"Stock movement #{i + 1}",
                    MovementDate = DateTime.UtcNow.AddDays(-rng.Next(1, 90)),
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Sales Orders (20 with items) ---
        var salesOrders = new List<SalesOrder>();
        if (await context.SalesOrders.IgnoreQueryFilters().CountAsync(o => o.TenantId == tenantId) < 20)
        {
            var statuses = new[] { OrderStatus.Pending, OrderStatus.Approved, OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Packed };
            for (int i = 0; i < 20; i++)
            {
                var items = new List<SalesOrderItem>();
                var itemCount = rng.Next(1, 4);
                decimal subTotal = 0;
                for (int j = 0; j < itemCount; j++)
                {
                    var prod = products[(i + j) % 20];
                    var qty = rng.Next(1, 10);
                    var lineTotal = prod.SellingPrice * qty;
                    subTotal += lineTotal;
                    items.Add(new SalesOrderItem
                    {
                        ProductId = prod.Id, Quantity = qty, UnitPrice = prod.SellingPrice,
                        TaxRate = 10m, TaxAmount = lineTotal * 0.10m, LineTotal = lineTotal,
                        TenantId = tenantId, CreatedBy = cb
                    });
                }
                var taxAmt = subTotal * 0.10m;
                var so = new SalesOrder
                {
                    OrderNumber = $"SO-{2026}{i + 1:D4}",
                    CustomerId = customers[i % 20].Id,
                    WarehouseId = warehouses[0].Id,
                    OrderDate = DateTime.UtcNow.AddDays(-rng.Next(1, 120)),
                    Status = statuses[i % statuses.Length],
                    SubTotal = subTotal, TaxAmount = taxAmt, TotalAmount = subTotal + taxAmt,
                    ShippingAddress = customers[i % 20].Address,
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(rng.Next(5, 30)),
                    Items = items,
                    TenantId = tenantId, CreatedBy = cb
                };
                salesOrders.Add(so);
            }
            context.SalesOrders.AddRange(salesOrders);
            await context.SaveChangesAsync();
        }
        else
        {
            salesOrders = await context.SalesOrders.IgnoreQueryFilters().Where(o => o.TenantId == tenantId).OrderBy(o => o.OrderNumber).Take(20).ToListAsync();
        }

        // --- Purchase Orders (20 with items) ---
        var purchaseOrders = new List<PurchaseOrder>();
        if (await context.PurchaseOrders.IgnoreQueryFilters().CountAsync(o => o.TenantId == tenantId) < 20)
        {
            var statuses = new[] { PurchaseOrderStatus.Draft, PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Approved, PurchaseOrderStatus.Received, PurchaseOrderStatus.PartiallyReceived };
            for (int i = 0; i < 20; i++)
            {
                var items = new List<PurchaseOrderItem>();
                var itemCount = rng.Next(1, 4);
                decimal subTotal = 0;
                for (int j = 0; j < itemCount; j++)
                {
                    var prod = products[(i + j) % 20];
                    var qty = rng.Next(5, 50);
                    var lineTotal = prod.CostPrice * qty;
                    subTotal += lineTotal;
                    items.Add(new PurchaseOrderItem
                    {
                        ProductId = prod.Id, Quantity = qty, UnitPrice = prod.CostPrice,
                        ReceivedQuantity = statuses[i % statuses.Length] == PurchaseOrderStatus.Received ? qty : 0,
                        TaxRate = 10m, TaxAmount = lineTotal * 0.10m, LineTotal = lineTotal,
                        TenantId = tenantId, CreatedBy = cb
                    });
                }
                var taxAmt = subTotal * 0.10m;
                var po = new PurchaseOrder
                {
                    OrderNumber = $"PO-{2026}{i + 1:D4}",
                    SupplierId = suppliers[i % 20].Id,
                    WarehouseId = warehouses[0].Id,
                    OrderDate = DateTime.UtcNow.AddDays(-rng.Next(1, 120)),
                    Status = statuses[i % statuses.Length],
                    SubTotal = subTotal, TaxAmount = taxAmt, TotalAmount = subTotal + taxAmt,
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(rng.Next(7, 45)),
                    Items = items,
                    TenantId = tenantId, CreatedBy = cb
                };
                purchaseOrders.Add(po);
            }
            context.PurchaseOrders.AddRange(purchaseOrders);
            await context.SaveChangesAsync();
        }
        else
        {
            purchaseOrders = await context.PurchaseOrders.IgnoreQueryFilters().Where(o => o.TenantId == tenantId).OrderBy(o => o.OrderNumber).Take(20).ToListAsync();
        }

        // --- Sales Quotes (20 with items) ---
        if (await context.SalesQuotes.IgnoreQueryFilters().CountAsync(q => q.TenantId == tenantId) < 20)
        {
            var statuses = new[] { QuoteStatus.Draft, QuoteStatus.Sent, QuoteStatus.Accepted, QuoteStatus.Rejected, QuoteStatus.Expired };
            for (int i = 0; i < 20; i++)
            {
                var items = new List<SalesQuoteItem>();
                var prod = products[i % 20];
                var qty = rng.Next(1, 15);
                var lineTotal = prod.SellingPrice * qty;
                items.Add(new SalesQuoteItem
                {
                    ProductId = prod.Id, Quantity = qty, UnitPrice = prod.SellingPrice,
                    TaxRate = 10m, TaxAmount = lineTotal * 0.10m, LineTotal = lineTotal,
                    TenantId = tenantId, CreatedBy = cb
                });
                context.SalesQuotes.Add(new SalesQuote
                {
                    QuoteNumber = $"QT-{2026}{i + 1:D4}",
                    CustomerId = customers[i % 20].Id,
                    Status = statuses[i % statuses.Length],
                    QuoteDate = DateTime.UtcNow.AddDays(-rng.Next(1, 60)),
                    ValidUntil = DateTime.UtcNow.AddDays(rng.Next(15, 60)),
                    SubTotal = lineTotal, TaxAmount = lineTotal * 0.10m, TotalAmount = lineTotal * 1.10m,
                    Items = items,
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Invoices (20) ---
        var invoices = new List<Invoice>();
        if (await context.Invoices.IgnoreQueryFilters().CountAsync(inv => inv.TenantId == tenantId) < 20)
        {
            var invStatuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.Paid, InvoiceStatus.PartiallyPaid, InvoiceStatus.Overdue };
            for (int i = 0; i < 20; i++)
            {
                var isSales = i < 10;
                var subTotal = (i + 1) * 250m;
                var taxAmt = subTotal * 0.10m;
                var totalAmt = subTotal + taxAmt;
                var inv = new Invoice
                {
                    InvoiceNumber = $"INV-{2026}{i + 1:D4}",
                    InvoiceType = isSales ? InvoiceType.Sales : InvoiceType.Purchase,
                    Status = invStatuses[i % invStatuses.Length],
                    SalesOrderId = isSales ? salesOrders[i].Id : null,
                    PurchaseOrderId = !isSales ? purchaseOrders[i - 10].Id : null,
                    CustomerId = isSales ? customers[i].Id : null,
                    SupplierId = !isSales ? suppliers[i - 10].Id : null,
                    InvoiceDate = DateTime.UtcNow.AddDays(-rng.Next(1, 90)),
                    DueDate = DateTime.UtcNow.AddDays(rng.Next(-15, 45)),
                    SubTotal = subTotal, TaxAmount = taxAmt, TotalAmount = totalAmt,
                    PaidAmount = invStatuses[i % invStatuses.Length] == InvoiceStatus.Paid ? totalAmt : invStatuses[i % invStatuses.Length] == InvoiceStatus.PartiallyPaid ? totalAmt * 0.5m : 0m,
                    TenantId = tenantId, CreatedBy = cb
                };
                invoices.Add(inv);
            }
            context.Invoices.AddRange(invoices);
            await context.SaveChangesAsync();
        }
        else
        {
            invoices = await context.Invoices.IgnoreQueryFilters().Where(inv => inv.TenantId == tenantId).OrderBy(inv => inv.InvoiceNumber).Take(20).ToListAsync();
        }

        // --- Payments (20) ---
        if (await context.Payments.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId) < 20)
        {
            var methods = new[] { PaymentMethod.BankTransfer, PaymentMethod.Cash, PaymentMethod.Check, PaymentMethod.CreditCard, PaymentMethod.Other };
            for (int i = 0; i < 20; i++)
            {
                var isReceipt = i < 10;
                context.Payments.Add(new Payment
                {
                    PaymentNumber = $"PAY-{2026}{i + 1:D4}",
                    PaymentType = isReceipt ? PaymentType.Receipt : PaymentType.Payment,
                    InvoiceId = invoices[i].Id,
                    CustomerId = isReceipt ? customers[i].Id : null,
                    SupplierId = !isReceipt ? suppliers[i - 10].Id : null,
                    Amount = (i + 1) * 150m,
                    PaymentDate = DateTime.UtcNow.AddDays(-rng.Next(1, 60)),
                    PaymentMethod = methods[i % methods.Length],
                    Reference = $"REF-{rng.Next(10000, 99999)}",
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Journal Entries (20 with lines) ---
        if (await context.JournalEntries.IgnoreQueryFilters().CountAsync(j => j.TenantId == tenantId) < 20)
        {
            var accounts = await context.Accounts.IgnoreQueryFilters().Where(a => a.TenantId == tenantId).ToListAsync();
            var cashAcct = accounts.First(a => a.Code == AccountCodes.Cash);
            var arAcct = accounts.First(a => a.Code == AccountCodes.AccountsReceivable);
            var revenueAcct = accounts.First(a => a.Code == AccountCodes.SalesRevenue);
            var cogsAcct = accounts.First(a => a.Code == AccountCodes.CostOfGoodsSold);
            var invAcct = accounts.First(a => a.Code == AccountCodes.Inventory);
            var apAcct = accounts.First(a => a.Code == AccountCodes.AccountsPayable);

            for (int i = 0; i < 20; i++)
            {
                var amount = (i + 1) * 500m;
                Guid debitAcctId, creditAcctId;
                string desc;
                if (i % 4 == 0) { debitAcctId = arAcct.Id; creditAcctId = revenueAcct.Id; desc = $"Sales revenue entry #{i + 1}"; }
                else if (i % 4 == 1) { debitAcctId = cashAcct.Id; creditAcctId = arAcct.Id; desc = $"Cash receipt #{i + 1}"; }
                else if (i % 4 == 2) { debitAcctId = cogsAcct.Id; creditAcctId = invAcct.Id; desc = $"COGS entry #{i + 1}"; }
                else { debitAcctId = apAcct.Id; creditAcctId = cashAcct.Id; desc = $"Supplier payment #{i + 1}"; }

                context.JournalEntries.Add(new JournalEntry
                {
                    Reference = $"JE-{2026}{i + 1:D4}",
                    EntryDate = DateTime.UtcNow.AddDays(-rng.Next(1, 120)),
                    Description = desc,
                    Lines = new List<JournalEntryLine>
                    {
                        new() { AccountId = debitAcctId, Debit = amount, Credit = 0, Description = desc, TenantId = tenantId, CreatedBy = cb },
                        new() { AccountId = creditAcctId, Debit = 0, Credit = amount, Description = desc, TenantId = tenantId, CreatedBy = cb }
                    },
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Price Lists (20) ---
        if (await context.PriceLists.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId) < 20)
        {
            for (int i = 0; i < 20; i++)
            {
                var pl = new PriceList
                {
                    Name = $"Price List {i + 1}",
                    IsDefault = i == 0,
                    EffectiveFrom = DateTime.UtcNow.AddDays(-90),
                    EffectiveTo = DateTime.UtcNow.AddDays(270),
                    TenantId = tenantId, CreatedBy = cb
                };
                context.PriceLists.Add(pl);
            }
            await context.SaveChangesAsync();
        }

        // --- Delivery Notes (20) ---
        if (await context.DeliveryNotes.IgnoreQueryFilters().CountAsync(d => d.TenantId == tenantId) < 20)
        {
            for (int i = 0; i < 20; i++)
            {
                context.DeliveryNotes.Add(new DeliveryNote
                {
                    DeliveryNoteNumber = $"DN-{2026}{i + 1:D4}",
                    SalesOrderId = salesOrders[i % salesOrders.Count].Id,
                    Date = DateTime.UtcNow.AddDays(-rng.Next(1, 60)),
                    ShippedBy = new[] { "FedEx", "UPS", "DHL", "USPS", "Local Courier" }[i % 5],
                    TrackingNumber = $"TRK{rng.Next(100000000, 999999999)}",
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Shipments (20) ---
        if (await context.Shipments.IgnoreQueryFilters().CountAsync(s => s.TenantId == tenantId) < 20)
        {
            var shipStatuses = new[] { ShipmentStatus.Pending, ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Delivered, ShipmentStatus.InTransit };
            for (int i = 0; i < 20; i++)
            {
                context.Shipments.Add(new Shipment
                {
                    SalesOrderId = salesOrders[i % salesOrders.Count].Id,
                    CarrierName = new[] { "FedEx", "UPS", "DHL", "USPS", "Maersk" }[i % 5],
                    TrackingNumber = $"SHP{rng.Next(100000000, 999999999)}",
                    ShipDate = DateTime.UtcNow.AddDays(-rng.Next(1, 30)),
                    EstimatedDelivery = DateTime.UtcNow.AddDays(rng.Next(1, 15)),
                    Status = shipStatuses[i % shipStatuses.Length],
                    FreightCost = Math.Round(15m + (decimal)(rng.NextDouble() * 200), 2),
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }

        // --- Tax Jurisdictions & Rates (bring to 20 each) ---
        var jurisdictions = await context.TaxJurisdictions.IgnoreQueryFilters().Where(t => t.TenantId == tenantId).ToListAsync();
        if (jurisdictions.Count < 20)
        {
            string[][] jurisdData = [["Federal","FED","US",""],["New York State","NYS","US","NY"],["California","CAS","US","CA"],["Texas","TXS","US","TX"],["Florida","FLS","US","FL"],["Illinois","ILS","US","IL"],["Pennsylvania","PAS","US","PA"],["Ohio","OHS","US","OH"],["Georgia","GAS","US","GA"],["Michigan","MIS","US","MI"],["Canada Federal","CAN","CA",""],["UK VAT","GBV","GB",""],["EU Standard","EUS","EU",""],["Japan CT","JPT","JP",""],["Australia GST","AUG","AU",""],["India GST","ING","IN",""],["Singapore GST","SGG","SG",""],["Brazil ISS","BRI","BR",""],["Mexico IVA","MXI","MX",""]];
            for (int i = 0; i < jurisdData.Length; i++)
            {
                var tj = new TaxJurisdiction
                {
                    Name = jurisdData[i][0], Code = jurisdData[i][1],
                    Country = jurisdData[i][2], State = string.IsNullOrEmpty(jurisdData[i][3]) ? null : jurisdData[i][3],
                    TenantId = tenantId, CreatedBy = cb
                };
                jurisdictions.Add(tj);
                context.TaxJurisdictions.Add(tj);
            }
            await context.SaveChangesAsync();

            // Add tax rates for each new jurisdiction
            var taxTypes = new[] { TaxType.VAT, TaxType.GST, TaxType.SalesTax, TaxType.ServiceTax };
            decimal[] rates = [7m, 8.875m, 7.25m, 6.25m, 6m, 6.25m, 6m, 4m, 5.75m, 6m, 5m, 20m, 21m, 10m, 10m, 18m, 9m, 5m, 16m];
            for (int i = 0; i < jurisdData.Length; i++)
            {
                context.TaxRates.Add(new TaxRate
                {
                    Name = $"{jurisdData[i][0]} Tax",
                    Rate = rates[i],
                    TaxType = taxTypes[i % taxTypes.Length],
                    TaxJurisdictionId = jurisdictions[i + 1].Id, // skip the "Default" one
                    EffectiveFrom = new DateTime(2024, 1, 1),
                    TenantId = tenantId, CreatedBy = cb
                });
            }
            await context.SaveChangesAsync();
        }
    }
}
