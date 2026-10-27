-- =============================================================
-- Landed Cost Management - test data seed (idempotent)
-- - Ensures 8 standard landed cost components
-- - Creates 20 allocations across the 20 most recent active POs
-- Safe to re-run: existing rows are skipped by CreatedBy marker
-- =============================================================
USE [avancera_EnterpriseTrading];
GO

SET NOCOUNT ON;

DECLARE @TenantId  uniqueidentifier = '00000000-0000-0000-0000-000000000001';
DECLARE @Marker    nvarchar(450)    = 'seed:landed-cost-test';
DECLARE @Now       datetime2        = SYSUTCDATETIME();

-- LandedCostAllocMethod: 0=ByValue, 1=ByWeight, 2=ByVolume, 3=ByQuantity
IF NOT EXISTS (SELECT 1 FROM LandedCostComponents WHERE TenantId = @TenantId AND Name = N'Freight Charges' AND IsDeleted = 0)
BEGIN
    INSERT INTO LandedCostComponents (Id, TenantId, Name, Type, DefaultRate, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
    VALUES
    (NEWID(), @TenantId, N'Freight Charges',      N'Shipping',  0.030, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Customs Duty',         N'Tax',       0.050, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Insurance',            N'Number',    0.005, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Port Handling',        N'Handling',  0.010, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Customs Clearance',    N'Service',   0.008, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Shipping Line Fee',    N'Fee',       0.015, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Inspection Fee',       N'Service',   0.004, @Now, @Marker, NULL, NULL, 0),
    (NEWID(), @TenantId, N'Loading & Offloading', N'Handling',  0.006, @Now, @Marker, NULL, NULL, 0);
END
ELSE
    PRINT 'Landed cost components already present - skipped.';

-- ── 20 allocations over the 20 most recent active purchase orders ──
IF EXISTS (SELECT 1 FROM LandedCostAllocations WHERE TenantId = @TenantId AND CreatedBy = @Marker)
BEGIN
    PRINT 'Landed cost test allocations already present - skipped.';
    RETURN;
END

;WITH Pool AS
(
    SELECT TOP (20) Id AS PurchaseOrderId
    FROM PurchaseOrders
    WHERE TenantId = @TenantId
      AND IsDeleted = 0
      AND Status <> 4          -- 4 = Cancelled
    ORDER BY PurchaseDate DESC, CreatedAt DESC
),
Slot AS
(
    SELECT ROW_NUMBER() OVER (ORDER BY PurchaseOrderId DESC) AS slot, PurchaseOrderId
    FROM Pool
),
Comp AS
(
    SELECT Name, Id,
           ROW_NUMBER() OVER (ORDER BY CASE Name
               WHEN N'Freight Charges'      THEN 0
               WHEN N'Customs Duty'         THEN 1
               WHEN N'Insurance'            THEN 2
               WHEN N'Port Handling'        THEN 3
               WHEN N'Customs Clearance'    THEN 4
               WHEN N'Shipping Line Fee'    THEN 5
               WHEN N'Inspection Fee'       THEN 6
               WHEN N'Loading & Offloading' THEN 7 END) AS rn
    FROM LandedCostComponents
    WHERE TenantId = @TenantId AND IsDeleted = 0
)
INSERT INTO LandedCostAllocations
    (Id, TenantId, PurchaseOrderId, ComponentId, Amount, AllocationMethod, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
SELECT
    NEWID(),
    @TenantId,
    s.PurchaseOrderId,
    c.Id,
    CAST(ROUND(v.Amount, 2) AS decimal(18,2)),
    v.AllocationMethod,
    @Now, @Marker, NULL, NULL, 0
FROM (VALUES
    -- (PO slot, component index, amount, allocation method)
    ( 1, 0, 1250.00, 0),
    ( 1, 3,  350.00, 3),
    ( 2, 1,  780.50, 0),
    ( 2, 4,  220.00, 3),
    ( 3, 2,  180.75, 0),
    ( 3, 5,  465.00, 2),
    ( 4, 0, 1610.00, 2),
    ( 4, 6,  145.30, 0),
    ( 5, 1,  990.00, 0),
    ( 5, 7,  275.00, 3),
    ( 6, 3,  420.80, 3),
    ( 6, 2,  152.40, 0),
    ( 7, 0, 2040.25, 0),
    ( 7, 5,  560.00, 2),
    ( 8, 1, 1130.60, 0),
    ( 8, 4,  310.00, 3),
    ( 9, 6,  128.90, 0),
    (10, 0, 1480.00, 2),
    (11, 3,  395.50, 3),
    (12, 1,  875.00, 0)
) AS v (PoSlot, ComponentIdx, Amount, AllocationMethod)
JOIN Slot AS s ON s.slot = v.PoSlot
JOIN Comp  AS c ON c.rn = v.ComponentIdx + 1;

DECLARE @Inserted int = (SELECT COUNT(1) FROM LandedCostAllocations WHERE TenantId = @TenantId AND CreatedBy = @Marker);
PRINT CONCAT('Inserted ', CAST(@Inserted AS nvarchar(10)), ' landed cost test allocations.');
GO
