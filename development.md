# Enterprise Inventory & Order Management System (IOMS)

## Product Development Document

**Version:** 1.0
**Last Updated:** April 2, 2026
**Status:** Active Development
**Document Owner:** Product Engineering Team

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Product Vision & Goals](#2-product-vision--goals)
3. [Scope & Boundaries](#3-scope--boundaries)
4. [Stakeholders](#4-stakeholders)
5. [Technology Stack](#5-technology-stack)
6. [System Architecture](#6-system-architecture)
7. [Module Specifications](#7-module-specifications)
8. [Domain Model & Database Design](#8-domain-model--database-design)
9. [Application Layer Design](#9-application-layer-design)
10. [Infrastructure & Cross-Cutting Concerns](#10-infrastructure--cross-cutting-concerns)
11. [Authentication & Authorization](#11-authentication--authorization)
12. [UI Screens & User Flows](#12-ui-screens--user-flows)
13. [Key Business Workflows](#13-key-business-workflows)
14. [API & Service Contracts](#14-api--service-contracts)
15. [Non-Functional Requirements](#15-non-functional-requirements)
16. [Development Phases & Roadmap](#16-development-phases--roadmap)
17. [Risk Assessment & Mitigation](#17-risk-assessment--mitigation)
18. [Testing Strategy](#18-testing-strategy)
19. [Deployment Strategy](#19-deployment-strategy)
20. [Dependencies & Third-Party Packages](#20-dependencies--third-party-packages)
21. [Glossary](#21-glossary)

---

## 1. Executive Summary

The Enterprise Inventory & Order Management System (IOMS) is a centralized platform designed to manage stock, procurement, sales orders, warehouse operations, and financial accounting across multiple departments and locations. The system provides real-time accuracy, scalability, and full auditability for enterprise-grade operations.

**Target Market:** Mid-to-large enterprises requiring unified inventory, order, and accounting management.

**Comparable Systems:** SAP Business One, Oracle NetSuite, Microsoft Dynamics 365.

---

## 2. Product Vision & Goals

### Vision Statement

> Deliver a modern, modular, and scalable inventory and order management platform that unifies procurement, sales, warehousing, and accounting into a single real-time system — enabling enterprises to reduce operational overhead, improve order accuracy, and gain financial visibility.

### Strategic Goals

| ID    | Goal                                       | Success Metric                                    |
| ----- | ------------------------------------------ | ------------------------------------------------- |
| G-001 | Centralize inventory across all warehouses | 100% stock visibility across all locations         |
| G-002 | Automate order-to-cash lifecycle           | Order processing time reduced by 60%               |
| G-003 | Double-entry accounting integration        | Zero manual journal entries for standard workflows  |
| G-004 | Multi-tenant SaaS readiness               | Support 50+ isolated tenants on shared infra        |
| G-005 | Real-time operational dashboards           | Sub-second data refresh on all KPI dashboards       |

---

## 3. Scope & Boundaries

### In Scope

- Product catalog management (SKU, barcode, variants, categories)
- Multi-warehouse inventory tracking with bin/location support
- Sales order lifecycle (creation → approval → fulfillment → delivery)
- Purchase order lifecycle (requisition → PO → GRN → invoice)
- Customer and supplier management
- Double-entry accounting engine (GL, AR, AP, Trial Balance, P&L, Balance Sheet)
- Role-based access control with audit logging
- Real-time notifications and dashboard updates
- Multi-tenant data isolation
- Tax management engine (VAT/GST/Sales Tax with jurisdictional rules)
- Multi-currency support with exchange rate management
- Pricing and discount engine (price lists, volume/tiered discounts, promotional pricing)
- Quotation / sales quote management with quote-to-order conversion
- Credit note and debit note workflows
- AR/AP aging analysis (30/60/90/120 day buckets)
- Bank reconciliation
- Shipping and logistics (delivery notes, packing slips, carrier tracking)
- Stocktake / cycle count workflows
- Unit of measure (UoM) conversion (buy in cases, sell in units)
- Kitting / simple assembly (bundle products without full BOM)
- Document template engine (invoices, POs, delivery notes, quotes)
- Email and SMS transactional notifications
- Bulk import/export (CSV/Excel for products, customers, stock, opening balances)
- Configurable approval workflow engine
- Full-text search across products, orders, customers, suppliers
- Scheduled/automated report generation and delivery
- Custom fields / user-defined fields per entity
- Product images and document attachments
- Purchase returns (return-to-supplier workflow)
- Supplier quotation / RFQ process
- GDPR / data privacy compliance (data subject requests, right to erasure)
- Data archival and retention policies
- Webhook / outbound event system for third-party consumers
- API rate limiting and throttling

### Out of Scope (v1.0)

- Direct eCommerce storefront integration
- Third-party ERP/POS connector APIs (Phase 3)
- Mobile native application (iOS/Android)
- AI-based demand forecasting engine (Phase 3)
- Barcode scanner hardware integration (Phase 3)
- Multi-language / i18n support (Phase 3)
- Manufacturing / BOM management
- Built-in payment gateway processing
- CRM functionality beyond basic customer records
- Shipping carrier rate comparison and label generation
- Drop shipping (direct supplier-to-customer fulfillment)
- Consignment inventory management
- Supplier self-service portal
- SOX compliance framework
- Budget management / budget vs. actuals

---

## 4. Stakeholders

| Role                  | Responsibility                                          |
| --------------------- | ------------------------------------------------------- |
| Product Owner         | Defines priorities, acceptance criteria, and roadmap     |
| Engineering Lead      | Architecture decisions, code reviews, technical direction |
| Backend Developer(s)  | Domain logic, services, EF Core, MediatR handlers        |
| Frontend Developer(s) | Blazor UI components, layouts, user flows                |
| QA Engineer           | Test planning, execution, regression testing             |
| DevOps Engineer       | CI/CD pipeline, deployment, monitoring                   |
| End Users             | Warehouse staff, accountants, managers, administrators   |

---

## 5. Technology Stack

| Layer           | Technology                            | Justification                              |
| --------------- | ------------------------------------- | ------------------------------------------ |
| UI Framework    | Blazor Server + MudBlazor             | Rich interactive UI, single codebase (.NET) |
| Backend         | .NET 8 (ASP.NET Core)                | Enterprise-grade, high performance          |
| ORM             | Entity Framework Core (Code First)    | Rapid development, migration support        |
| Database        | SQL Server                            | ACID compliance, enterprise-standard        |
| Authentication  | ASP.NET Core Identity (Cookie-based)  | Built-in, no external IdP required          |
| Real-time       | SignalR                               | Native .NET WebSocket abstraction           |
| Validation      | FluentValidation                      | Declarative, testable validation rules      |
| Object Mapping  | AutoMapper                            | Consistent DTO ↔ entity mapping             |
| CQRS/Mediator   | MediatR                               | Decoupled command/query handling            |

---

## 6. System Architecture

### Pattern: Modular Monolith (Clean Architecture)

Blazor Server calls application services directly via dependency injection — no separate Web API layer.

```
┌─────────────────────────────────────────────────┐
│                  Blazor Server UI                │
│              (IOMS.Web — MudBlazor)              │
└──────────────────┬──────────────────────────────┘
                   │ DI (MediatR)
┌──────────────────▼──────────────────────────────┐
│              Application Layer                   │
│  (IOMS.Application — Commands, Queries, DTOs)    │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│                Domain Layer                      │
│    (IOMS.Domain — Entities, Enums, Rules)        │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│            Infrastructure Layer                  │
│ (IOMS.Infrastructure — EF Core, Repos, Identity) │
└──────────────────┬──────────────────────────────┘
                   │
              [ SQL Server ]
```

### Solution Structure

```
Enterprise.IOMS/
├── IOMS.Web/                  → Blazor Server (UI Layer)
│   ├── Pages/                 → Razor pages & components
│   ├── Shared/                → Layouts, navigation
│   ├── Hubs/                  → SignalR hubs
│   └── Program.cs             → DI configuration & startup
│
├── IOMS.Application/          → Business Logic
│   ├── Commands/              → Write operations (MediatR)
│   ├── Queries/               → Read operations (MediatR)
│   ├── DTOs/                  → Data transfer objects
│   ├── Interfaces/            → Service contracts
│   ├── Validators/            → FluentValidation rules
│   ├── Mappings/              → AutoMapper profiles
│   └── Behaviors/             → MediatR pipeline behaviors
│
├── IOMS.Domain/               → Core Business Domain
│   ├── Entities/              → Product, SalesOrder, Account, etc.
│   ├── Enums/                 → OrderStatus, AccountType, etc.
│   └── Exceptions/            → Domain-specific exceptions
│
├── IOMS.Infrastructure/       → Data Access & External Services
│   ├── Data/                  → AppDbContext, migrations
│   ├── Repositories/          → Generic & specialized repos
│   ├── Identity/              → ASP.NET Core Identity config
│   └── Services/              → External service integrations
│
├── IOMS.Shared/               → Cross-Cutting Utilities
│   ├── Constants/             → App-wide constants
│   ├── Helpers/               → Utility functions
│   └── Extensions/            → Extension methods
│
└── tests/
    ├── IOMS.UnitTests/        → Unit tests (xUnit + Moq)
    └── IOMS.IntegrationTests/ → Integration tests (TestContainers)
```

---

## 7. Module Specifications

### 7.1 Inventory Management

**Purpose:** Track all products and stock levels across warehouses in real time.

| Feature                    | Priority | Description                                                    |
| -------------------------- | -------- | -------------------------------------------------------------- |
| Product CRUD               | P0       | Create, read, update, delete products with SKU, barcode, price |
| Category management        | P0       | Hierarchical product categorization                            |
| Real-time stock tracking   | P0       | Live quantity per product per warehouse                         |
| Multi-warehouse support    | P0       | Stock tracked independently per warehouse                      |
| Stock transfer             | P1       | Move stock between warehouses with movement log                |
| Low stock alerts           | P1       | Configurable thresholds with notification triggers             |
| Reorder point automation   | P1       | Auto-generate purchase requisition at reorder level            |
| Batch/serial tracking      | P2       | Track inventory by batch or serial number                      |
| Expiry management          | P2       | FIFO/FEFO picking enforcement for perishable goods             |

**Acceptance Criteria:**
- Stock quantities update within 1 second of order/receipt events.
- Stock transfer creates paired debit/credit movement records.
- Low stock alerts fire when quantity drops below configured threshold.

---

### 7.2 Order Management

**Purpose:** Manage the full sales order lifecycle from creation to delivery.

| Feature                | Priority | Description                                               |
| ---------------------- | -------- | --------------------------------------------------------- |
| Sales order creation   | P0       | Create orders with customer, items, quantities, prices    |
| Order status workflow  | P0       | Pending → Approved → Packed → Shipped → Delivered         |
| Stock deduction        | P0       | Automatic inventory reduction on order approval           |
| Partial fulfillment    | P1       | Ship available items, backorder the rest                  |
| Returns (RMA)          | P1       | Return merchandise authorization with stock re-entry      |
| Order cancellation     | P1       | Cancel orders with automatic stock restoration            |
| Order approval chain   | P2       | Manager approval required above configurable threshold    |

**Acceptance Criteria:**
- Order cannot be approved if insufficient stock exists.
- Cancellation restores reserved stock and reverses journal entries.
- Partial fulfillment updates order status to "Partially Shipped."

---

### 7.3 Purchase Management

**Purpose:** Handle procurement from requisition through goods receipt and supplier payment.

| Feature                    | Priority | Description                                          |
| -------------------------- | -------- | ---------------------------------------------------- |
| Purchase order creation    | P0       | Create POs linked to suppliers with line items       |
| Goods receiving note (GRN) | P0       | Record received goods, increase inventory            |
| Supplier management        | P0       | Supplier profiles with contact and payment terms     |
| Purchase requisition       | P1       | Internal request for procurement approval            |
| Supplier invoice tracking  | P1       | Match supplier invoices to POs and GRNs              |
| Supplier performance       | P2       | Track delivery times, defect rates, reliability      |

**Acceptance Criteria:**
- GRN automatically increases warehouse stock for received items.
- PO total must match sum of line items.
- Supplier invoice cannot exceed PO amount.

---

### 7.4 Customer & Supplier Management

**Purpose:** Maintain profiles, credit limits, and payment terms for customers and suppliers.

| Feature                  | Priority | Description                                      |
| ------------------------ | -------- | ------------------------------------------------ |
| Customer profiles        | P0       | Name, email, phone, address, credit limit        |
| Supplier profiles        | P0       | Name, contact info, payment terms                |
| Credit limit enforcement | P1       | Block orders exceeding customer credit limit     |
| Payment terms config     | P1       | Net 30, Net 60, COD, etc.                        |

---

### 7.5 Warehouse Management

**Purpose:** Organize physical warehouse operations including locations, movements, and picking.

| Feature               | Priority | Description                                        |
| --------------------- | -------- | -------------------------------------------------- |
| Multi-warehouse setup | P0       | Define warehouses with name and location            |
| Stock movement logs   | P0       | Full history of all stock in/out/transfer events    |
| Bin/location tracking | P1       | Assign products to specific bins within warehouse   |
| Picking & packing     | P2       | Generate pick lists for order fulfillment           |

---

### 7.6 Accounting System (Double-Entry)

**Purpose:** Maintain a complete, auditable double-entry bookkeeping system integrated with operations.

| Feature                     | Priority | Description                                            |
| --------------------------- | -------- | ------------------------------------------------------ |
| Chart of Accounts           | P0       | Asset, Liability, Equity, Revenue, Expense accounts    |
| Journal entries              | P0       | Manual entry with enforced Debit = Credit rule         |
| Auto-posting from orders    | P0       | Sales → DR: AR, CR: Revenue; Purchase → DR: Inv, CR: AP |
| General Ledger              | P0       | Complete transaction ledger per account                 |
| Accounts Receivable (AR)    | P1       | Track customer outstanding balances                    |
| Accounts Payable (AP)       | P1       | Track supplier outstanding payments                    |
| Trial Balance report        | P1       | Sum of debits must equal sum of credits                |
| Profit & Loss statement     | P1       | Revenue minus expenses for a period                    |
| Balance Sheet               | P1       | Assets = Liabilities + Equity at a point in time       |
| Invoice & payment reconcile | P2       | Match payments to invoices                             |

**Standard Chart of Accounts:**

| Code | Account             | Type      |
| ---- | ------------------- | --------- |
| 1000 | Cash                | Asset     |
| 1100 | Accounts Receivable | Asset     |
| 1200 | Inventory           | Asset     |
| 2000 | Accounts Payable    | Liability |
| 3000 | Owner's Equity      | Equity    |
| 4000 | Sales Revenue       | Revenue   |
| 5000 | Cost of Goods Sold  | Expense   |
| 5100 | Operating Expenses  | Expense   |

**Acceptance Criteria:**
- Every journal entry must balance (total debits = total credits).
- Trial Balance must always balance; imbalance blocks report generation.
- Auto-posted entries are immutable; corrections require reversal entries.

---

### 7.7 Reporting & Analytics

**Purpose:** Provide actionable business intelligence through dashboards and exportable reports.

| Report                     | Priority | Description                                    |
| -------------------------- | -------- | ---------------------------------------------- |
| Sales performance dashboard| P0       | Revenue by period, top customers, order volume |
| Inventory valuation        | P1       | Total stock value by warehouse                 |
| Fast/slow-moving items     | P1       | Identify high and low turnover products        |
| Profit margin analysis     | P1       | Margin per product, category, customer         |
| Purchase vs sales trend    | P2       | Spending vs revenue comparison over time        |
| Forecasting analytics      | P2       | Projected demand based on historical data       |

---

### 7.8 Role & Security System

**Purpose:** Enforce access control, track user actions, and protect sensitive data.

| Feature                   | Priority | Description                                      |
| ------------------------- | -------- | ------------------------------------------------ |
| Role-based access (RBAC)  | P0       | Admin, Manager, Staff roles with permissions     |
| Department permissions    | P1       | Restrict access by department/module             |
| Audit logging             | P0       | Record every create, update, delete with user ID |
| Session management        | P1       | Secure cookie handling, session timeout          |

---

### 7.9 Tax Management

**Purpose:** Calculate, apply, and report taxes across all sales and purchase transactions.

| Feature                     | Priority | Description                                                |
| --------------------------- | -------- | ---------------------------------------------------------- |
| Tax rate configuration      | P0       | Define tax rates (VAT, GST, Sales Tax) with effective dates |
| Tax zone / jurisdiction     | P0       | Map tax rates to geographic regions or customer/supplier zones |
| Tax calculation on orders   | P0       | Auto-calculate tax per line item on sales and purchase orders |
| Tax exemption handling      | P1       | Support tax-exempt customers and products with exemption codes |
| Tax-inclusive pricing       | P1       | Support prices that include tax (extract tax from gross price) |
| Compound tax rules          | P2       | Support taxes calculated on top of other taxes              |
| Tax reporting               | P1       | Generate tax summary reports by period, jurisdiction, and rate |
| Tax audit trail             | P1       | Immutable record of tax applied on every transaction        |

**Acceptance Criteria:**
- Every sales/purchase order line item has a computed tax amount.
- Tax reports reconcile exactly with journal entries.
- Changing a tax rate does not retroactively affect closed transactions.

---

### 7.10 Pricing & Discount Engine

**Purpose:** Manage flexible pricing strategies including volume discounts, customer-specific pricing, and promotions.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Price lists                   | P0       | Define multiple price lists (e.g., retail, wholesale, VIP)   |
| Customer-specific pricing     | P1       | Assign price lists to individual customers or customer groups |
| Volume / tiered discounts     | P1       | Automatic discounts based on order quantity thresholds        |
| Promotional pricing           | P1       | Time-limited price overrides with start/end dates            |
| Contract pricing              | P2       | Agreed rates with suppliers/customers over a fixed period     |
| Minimum order quantities      | P1       | Enforce MOQ per product for sales and purchase orders         |
| Discount approval workflow    | P2       | Discounts above a threshold require manager approval          |
| Line-level & order-level disc | P1       | Support discounts on individual items and on order totals     |

**Acceptance Criteria:**
- Price list hierarchy resolves correctly (contract > customer-specific > promotional > default).
- Expired promotions are automatically excluded from price calculations.
- Discount approval blocks order confirmation until approved.

---

### 7.11 Quotation / Sales Quote Management

**Purpose:** Enable the quote-to-order pipeline for enterprise sales workflows.

| Feature                     | Priority | Description                                                  |
| --------------------------- | -------- | ------------------------------------------------------------ |
| Quote creation              | P0       | Create quotes with customer, items, quantities, prices, validity period |
| Quote status workflow       | P0       | Draft → Sent → Accepted → Converted / Rejected / Expired    |
| Quote-to-order conversion   | P0       | Convert an accepted quote into a sales order with one click  |
| Quote versioning            | P1       | Track revisions when a quote is amended before acceptance    |
| Quote validity period       | P1       | Auto-expire quotes after configurable number of days         |
| Quote PDF generation        | P1       | Generate branded PDF quotes for email/download               |

**Acceptance Criteria:**
- Converting a quote to an order pre-fills all line items and pricing.
- Expired quotes cannot be converted to orders.
- Quote revisions maintain full version history.

---

### 7.12 Shipping & Logistics

**Purpose:** Manage delivery scheduling, document generation, and shipment tracking.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Delivery note generation      | P0       | Auto-generate delivery notes from packed sales orders        |
| Packing slip generation       | P0       | Printable packing slips with item details and quantities     |
| Shipment tracking             | P1       | Record tracking numbers, carrier name, and expected delivery |
| Delivery scheduling           | P1       | Assign estimated delivery dates to sales orders              |
| Freight cost allocation       | P1       | Record and allocate shipping costs to orders                 |
| Shipping address management   | P1       | Multiple shipping addresses per customer                     |
| Proof of delivery             | P2       | Record delivery confirmation with signature/photo capture    |

**Acceptance Criteria:**
- Delivery note is auto-generated when order status changes to Shipped.
- Freight costs are included in order total and reflected in journal entries.
- Tracking information is visible to the user on the order detail screen.

---

### 7.13 Stocktake & Cycle Count

**Purpose:** Enable physical inventory counting to reconcile system stock with actual warehouse stock.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Stocktake creation            | P0       | Create a stocktake session for a warehouse or zone           |
| Count sheet generation        | P0       | Generate printable count sheets with expected quantities     |
| Count entry                   | P0       | Record actual counted quantities per product/location        |
| Variance report               | P0       | Compare system qty vs counted qty with variance highlighting |
| Stocktake approval & posting  | P0       | Approve variances to auto-adjust stock and post journal entries |
| Cycle count scheduling        | P1       | Schedule recurring partial counts (ABC-based frequency)      |
| Blind count mode              | P2       | Hide expected quantities during counting to avoid bias       |

**Acceptance Criteria:**
- Stocktake adjustments create corresponding StockMovement records (Type: Adjustment).
- Variance approval posts journal entries (DR/CR Inventory Adjustment account).
- In-progress stocktakes do not block normal warehouse operations.

---

### 7.14 Unit of Measure (UoM) Management

**Purpose:** Support purchasing, stocking, and selling products in different units of measure.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| UoM definition                | P0       | Define units (each, box, carton, pallet, kg, liter, etc.)   |
| UoM conversion rules          | P0       | Define conversion factors (1 carton = 12 each)              |
| Purchase UoM vs stock UoM     | P1       | Buy in cartons, stock in individual units automatically      |
| Sales UoM vs stock UoM        | P1       | Sell in different units than stocked units                   |
| UoM on order line items       | P1       | Select UoM per line item; system converts to stock UoM       |

**Acceptance Criteria:**
- Stock quantities always stored in base UoM.
- Order line items display in the selected UoM but deduct/add stock in base UoM.
- UoM conversion is applied consistently across purchase, sales, and inventory.

---

### 7.15 Document & Communication System

**Purpose:** Generate, customize, and distribute business documents; manage transactional notifications.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Document template engine      | P0       | Customizable templates for invoices, POs, quotes, delivery notes |
| PDF generation                | P0       | Generate print-ready PDF for all document types              |
| Email notifications           | P0       | Transactional emails (order confirmation, shipment, payment receipt) |
| SMS notifications             | P2       | Optional SMS alerts for critical events (low stock, delivery) |
| Document attachment support   | P1       | Attach files (PDF, images) to orders, products, suppliers    |
| Product image management      | P1       | Upload and manage product images (thumbnails, gallery)       |
| Notification preferences      | P1       | Per-user control over notification channels and event types  |
| Email template customization  | P1       | Customizable email body templates per notification type      |

**Acceptance Criteria:**
- All generated PDFs include tenant branding (logo, company details, footer).
- Email delivery failures are logged and retryable.
- Users can opt out of non-critical notifications.

---

### 7.16 Credit Note & Debit Note

**Purpose:** Handle financial adjustments for returns, overpayments, disputes, and corrections.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Credit note creation          | P0       | Issue credit notes against sales invoices (full or partial)  |
| Debit note creation           | P0       | Issue debit notes against purchase invoices                  |
| Link to RMA / return          | P1       | Auto-generate credit note from approved RMA                  |
| Apply credit to future orders | P1       | Allow outstanding credit to offset future invoice balances   |
| Credit/debit note journal     | P0       | Auto-post reversal journal entries for credit/debit notes    |

**Acceptance Criteria:**
- Credit notes reduce AR balance; debit notes reduce AP balance.
- Credit note total cannot exceed the original invoice total.
- Auto-posted journal entries are immutable.

---

### 7.17 Bank Reconciliation

**Purpose:** Match recorded payments with bank statement entries to ensure cash account accuracy.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Bank statement import         | P1       | Import bank statements (CSV/OFX format)                      |
| Auto-matching                 | P1       | Automatically match statement lines to recorded payments by amount/reference |
| Manual matching               | P1       | Manually match unmatched statement lines to payments          |
| Reconciliation report         | P1       | Show matched, unmatched, and discrepancy summary              |
| Reconciliation approval       | P2       | Approve reconciliation to lock the period                     |

**Acceptance Criteria:**
- All matched entries are marked as reconciled in the payment ledger.
- Unmatched items are highlighted for manual resolution.
- Reconciled periods cannot be modified without admin override.

---

### 7.18 Multi-Currency Support

**Purpose:** Enable international operations with automatic currency conversion and multi-currency reporting.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Currency definitions          | P0       | Define currencies with ISO code, symbol, and decimal places  |
| Exchange rate management      | P0       | Maintain exchange rates with effective dates                  |
| Transaction currency          | P0       | Record sales/purchase orders in the customer/supplier currency |
| Auto-conversion to base       | P0       | Convert all transactions to the base/functional currency for accounting |
| Realized gain/loss            | P1       | Calculate exchange gain/loss on payment vs. invoice rate     |
| Unrealized gain/loss          | P2       | Revalue open AR/AP balances at period-end exchange rates     |
| Multi-currency reports        | P1       | Display reports in base currency with original currency reference |

**Acceptance Criteria:**
- All GL entries are recorded in base currency with the original transaction currency preserved.
- Exchange gain/loss is auto-posted to the designated GL account.
- Changing exchange rates does not retroactively affect closed transactions.

---

### 7.19 Bulk Import/Export

**Purpose:** Enable mass data operations for onboarding, migration, and periodic data management.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Product bulk import (CSV/Excel)| P0      | Import product catalog with validation and error reporting   |
| Customer/supplier import      | P0       | Bulk import customer and supplier records                    |
| Opening stock import          | P0       | Import opening inventory balances per warehouse              |
| Opening balance import        | P1       | Import chart of accounts opening balances for go-live        |
| Export to CSV/Excel           | P0       | Export any data table to CSV or Excel formats                |
| Import validation report      | P0       | Detailed row-by-row error report before committing import    |
| Import rollback               | P1       | Undo a bulk import if errors are discovered post-import      |

**Acceptance Criteria:**
- Import validates all rows before committing any records.
- Duplicate detection (by SKU, email, etc.) prevents accidental duplicates.
- Import audit log records who imported what and when.

---

### 7.20 Configurable Approval Workflows

**Purpose:** Enable flexible, rule-based approval chains for enterprise governance.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Workflow rule definition      | P1       | Define approval rules based on document type, amount thresholds, department |
| Multi-level approval chains   | P1       | Support sequential and parallel approver chains              |
| Approval delegation           | P2       | Delegate approval authority during absence                   |
| Approval notifications        | P1       | Notify approvers via in-app and email when action is required |
| Approval history & audit      | P1       | Full audit trail of who approved/rejected and when           |
| Escalation rules              | P2       | Auto-escalate to next approver if no response within SLA    |

**Acceptance Criteria:**
- Documents requiring approval are blocked from progressing until all approvals are obtained.
- Approval rules are evaluated dynamically at submission time.
- Delegation expires automatically at the configured end date.

---

### 7.21 Webhook & Event System

**Purpose:** Enable third-party systems to subscribe to real-time events from IOMS.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Webhook registration          | P1       | Register external URLs to receive event payloads             |
| Event catalog                 | P1       | Defined events: order.created, order.shipped, stock.low, payment.received, etc. |
| Retry with exponential backoff| P1       | Retry failed webhook deliveries up to N times                |
| Webhook delivery log          | P1       | Log all webhook attempts with status (success/fail/retry)    |
| Webhook secret / HMAC signing | P0       | Sign payloads with shared secret for receiver verification   |
| Event filtering               | P2       | Subscribers can filter events by type, entity, or criteria   |

**Acceptance Criteria:**
- Webhook payloads include event type, timestamp, tenant ID, and resource data.
- Failed deliveries do not block the originating workflow.
- Secrets are stored encrypted and never exposed in logs.

---

### 7.22 Full-Text Search

**Purpose:** Provide fast, unified search across all major entities.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Global search bar             | P0       | Unified search across products, orders, customers, suppliers |
| Product search                | P0       | Search by name, SKU, barcode, description, category          |
| Order search                  | P0       | Search by order number, customer name, status                |
| Search suggestions / autocomplete | P1   | Real-time suggestions as user types                          |
| Advanced filters              | P1       | Combine search with filters (date range, status, warehouse)  |
| Search indexing               | P1       | Background indexing for sub-200ms search response times      |

**Acceptance Criteria:**
- Global search returns results across entity types within 200ms.
- Search results are tenant-isolated.
- Index is updated within 5 seconds of data changes.

---

### 7.23 GDPR & Data Privacy

**Purpose:** Ensure compliance with data privacy regulations (GDPR, CCPA, etc.).

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Data subject access request   | P1       | Export all personal data for a customer/user upon request     |
| Right to erasure              | P1       | Anonymize or delete personal data while preserving financial records |
| Consent management            | P2       | Record and manage consent for marketing communications       |
| Data retention policies       | P1       | Configurable retention periods with automatic archival/purge  |
| Privacy audit log             | P1       | Log all data access and export events for compliance         |

**Acceptance Criteria:**
- Data export is generated in a machine-readable format (JSON/CSV) within 48 hours.
- Erasure anonymizes customer PII but retains transaction records for accounting compliance.
- Retention policies auto-archive records past the configured period.

---

### 7.24 Scheduled Reports

**Purpose:** Automate report generation and distribution to stakeholders.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Report scheduling             | P1       | Schedule any report for daily/weekly/monthly generation      |
| Email delivery                | P1       | Auto-email generated reports (PDF/Excel) to recipients       |
| Report queue & history        | P1       | Track scheduled report execution with success/failure status |
| Custom recipient lists        | P2       | Define distribution lists per scheduled report               |

**Acceptance Criteria:**
- Scheduled reports execute at the configured time without manual intervention.
- Failed report deliveries are retried and logged.
- Recipients only receive reports they have permission to view.

---

### 7.25 Custom Fields / User-Defined Fields

**Purpose:** Allow tenants to extend entity data models without code changes.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Custom field definition       | P1       | Admins define custom fields per entity (product, customer, order, etc.) |
| Field types support           | P1       | Text, number, date, dropdown, checkbox, multi-select         |
| Custom field validation       | P2       | Required/optional, min/max, regex patterns                   |
| Custom fields on UI forms     | P1       | Dynamically rendered on entity create/edit forms             |
| Custom fields in exports      | P2       | Include custom field values in CSV/Excel exports             |
| Custom fields in search       | P2       | Search and filter by custom field values                     |

**Acceptance Criteria:**
- Adding a custom field does not require application redeployment.
- Custom field values are stored efficiently (EAV or JSON column strategy).
- Custom fields are tenant-isolated.

---

### 7.26 Purchase Returns (Return-to-Supplier)

**Purpose:** Handle returning defective, damaged, or incorrect goods to suppliers.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Purchase return creation      | P0       | Create a return against a purchase order/GRN                 |
| Return status workflow        | P0       | Draft → Approved → Shipped → Completed                      |
| Stock deduction on return     | P0       | Deduct stock from warehouse on return approval               |
| Debit note auto-generation    | P1       | Auto-create debit note linked to the purchase return         |
| Return journal auto-posting   | P0       | DR: Accounts Payable, CR: Inventory                          |
| Supplier notification         | P1       | Notify supplier of the return via email                      |

**Acceptance Criteria:**
- Purchase return cannot exceed original GRN received quantities.
- Stock movement (Type: Return) is created with supplier reference.
- Debit note total matches the return value.

---

### 7.27 Supplier Quotation / RFQ

**Purpose:** Formalize the request-for-quotation process before creating purchase orders.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| RFQ creation                  | P1       | Send requests for quotation to one or more suppliers         |
| Supplier response entry       | P1       | Record supplier quoted prices, lead times, and terms         |
| Quotation comparison          | P1       | Side-by-side comparison of supplier responses                |
| RFQ-to-PO conversion         | P1       | Convert the selected supplier quotation into a purchase order |
| RFQ status tracking           | P1       | Draft → Sent → Received → Awarded → Closed                  |

**Acceptance Criteria:**
- RFQ can be sent to multiple suppliers for the same items.
- Comparison view highlights best price, shortest lead time.
- Converting to PO pre-fills supplier, items, and quoted prices.

---

### 7.28 Landed Cost Calculation

**Purpose:** Accurately capture the total cost of inventory including freight, duties, and handling charges.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Landed cost definition        | P1       | Define cost components (freight, customs duty, insurance, handling) |
| Cost allocation methods       | P1       | Allocate landed costs by value, weight, volume, or quantity  |
| Apply to GRN / PO             | P1       | Add landed cost components when receiving goods              |
| Inventory cost update          | P1       | Adjust product unit cost to include landed cost allocation   |
| Landed cost journal entries   | P1       | Post journal entries for each landed cost component          |

**Acceptance Criteria:**
- Inventory valuation reflects the fully landed cost per unit.
- Landed cost allocation is traceable to the source PO/GRN.
- COGS calculations use the landed cost, not just the supplier price.

---

### 7.29 Kitting / Simple Assembly

**Purpose:** Support bundling multiple products into kits without full manufacturing/BOM functionality.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Kit definition                | P1       | Define a kit product with component products and quantities  |
| Kit assembly                  | P1       | Assemble kits (deduct components, create kit stock)          |
| Kit disassembly               | P2       | Disassemble kits back into component products                |
| Kit pricing                   | P1       | Kit price can differ from sum of component prices            |
| Kit stock tracking            | P1       | Track assembled kit inventory as a distinct SKU              |

**Acceptance Criteria:**
- Assembling a kit deducts component stock and creates kit stock atomically.
- Kit components are validated for sufficient stock before assembly.
- Kit cost is calculated from the sum of component costs.

---

### 7.30 Dead Stock & Obsolescence Management

**Purpose:** Identify, manage, and dispose of non-moving or obsolete inventory.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Dead stock identification     | P1       | Flag products with zero movement for a configurable period   |
| Obsolescence report           | P1       | Report showing dead stock value per warehouse                |
| Write-off workflow            | P1       | Write off dead stock with reason code and approval           |
| Disposal journal entries      | P1       | Auto-post DR: Inventory Write-Off Expense, CR: Inventory    |

**Acceptance Criteria:**
- Dead stock threshold is configurable per category (e.g., 90, 180, 365 days).
- Write-offs require manager approval and create audit log entries.
- Disposed stock is removed from inventory valuation reports.

---

### 7.31 AR/AP Aging Analysis

**Purpose:** Provide aged receivable and payable reports for cash flow management and collections.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| AR aging report               | P0       | Aging buckets (Current, 1-30, 31-60, 61-90, 90+ days) per customer |
| AP aging report               | P0       | Aging buckets per supplier                                   |
| Aging summary dashboard       | P1       | Visual summary of total aged AR/AP amounts                   |
| Customer statements           | P1       | Generate printable customer statement of account             |
| Collection follow-up tracking | P2       | Track follow-up actions and notes on overdue invoices        |

**Acceptance Criteria:**
- Aging is calculated from invoice due date, not invoice date.
- Report totals reconcile with AR/AP GL account balances.
- Customer statements can be exported as PDF.

---

### 7.32 API Rate Limiting & Throttling

**Purpose:** Protect shared infrastructure and ensure fair usage across multi-tenant deployments.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Per-tenant rate limiting      | P1       | Configurable request limits per tenant per time window       |
| Per-endpoint throttling       | P1       | Different limits for read vs. write operations               |
| Rate limit headers            | P1       | Return X-RateLimit-Limit, X-RateLimit-Remaining, Retry-After headers |
| Rate limit exceeded response  | P1       | Return 429 Too Many Requests with Retry-After guidance      |
| Rate limit dashboard          | P2       | Admin view of tenant API usage and throttle events           |

**Acceptance Criteria:**
- Rate limits are enforced at the API gateway / middleware level.
- Exceeded limits return proper 429 responses without processing the request.
- Rate limit configuration is adjustable per tenant tier.

---

### 7.33 Data Archival & Retention

**Purpose:** Maintain system performance by archiving old data and enforcing configurable retention policies.

| Feature                       | Priority | Description                                                  |
| ----------------------------- | -------- | ------------------------------------------------------------ |
| Archival policy configuration | P1       | Define retention periods per entity type (orders, logs, movements) |
| Automatic archival job        | P1       | Background job to move aged records to archive tables        |
| Archive query access          | P2       | Allow read-only access to archived records from the UI       |
| Audit log retention           | P1       | Retain audit logs per regulatory requirements (default 7 years) |
| Purge after retention         | P2       | Permanently delete archived records after retention period expires |

**Acceptance Criteria:**
- Archived records are removed from active queries to improve performance.
- Archival does not affect referenced records still in active use.
- Purge operations require admin confirmation and create audit entries.

---

## 8. Domain Model & Database Design

### 8.1 Base Entity

All entities inherit multi-tenancy and audit fields:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}
```

### 8.2 Core Entities

| Entity              | Key Fields                                                  | Relationships                           |
| ------------------- | ----------------------------------------------------------- | --------------------------------------- |
| Product             | Name, SKU, Barcode, Price, ReorderLevel, CategoryId         | → Category, → Inventories               |
| Category            | Name, Description, ParentCategoryId                         | → Products, → self (hierarchy)           |
| Warehouse           | Name, Location, Code                                        | → Inventories                           |
| Inventory           | ProductId, WarehouseId, Quantity, LastStockDate              | → Product, → Warehouse                  |
| Customer            | Name, Email, Phone, Address, CreditLimit                    | → SalesOrders                           |
| Supplier            | Name, Email, Phone, PaymentTerms                            | → PurchaseOrders                        |
| SalesOrder          | CustomerId, OrderDate, Status, TotalAmount                  | → Customer, → SalesOrderItems            |
| SalesOrderItem      | SalesOrderId, ProductId, Quantity, UnitPrice, LineTotal      | → SalesOrder, → Product                 |
| PurchaseOrder       | SupplierId, OrderDate, Status, TotalAmount                  | → Supplier, → PurchaseOrderItems         |
| PurchaseOrderItem   | PurchaseOrderId, ProductId, Quantity, UnitPrice              | → PurchaseOrder, → Product               |
| StockMovement       | ProductId, WarehouseId, Type, Quantity, Reference, Date      | → Product, → Warehouse                  |
| Account             | Name, Code, AccountType, ParentAccountId, IsActive           | → JournalEntryLines, → self (hierarchy) |
| JournalEntry        | Date, Reference, Description, IsAutoPosted                   | → JournalEntryLines                     |
| JournalEntryLine    | JournalEntryId, AccountId, Debit, Credit, Description        | → JournalEntry, → Account               |
| Invoice             | OrderId, InvoiceNumber, Date, DueDate, TotalAmount, Status   | → SalesOrder / PurchaseOrder             |
| Payment             | InvoiceId, Amount, PaymentDate, Method, Reference             | → Invoice                               |
| AuditLog            | TableName, RecordId, Action, OldValues, NewValues, UserId    | → ApplicationUser                       |
| ApplicationUser     | FullName, Department (extends IdentityUser)                   | → AuditLogs                             |
| TaxRate             | Name, Rate, TaxType, JurisdictionId, EffectiveFrom, EffectiveTo, IsActive | → TaxJurisdiction                  |
| TaxJurisdiction     | Name, Code, Country, State, Description                       | → TaxRates                              |
| TaxExemption        | CustomerId/ProductId, ExemptionCode, Reason, ValidUntil       | → Customer / Product                    |
| PriceList           | Name, CurrencyId, IsDefault, EffectiveFrom, EffectiveTo       | → PriceListItems, → Customers           |
| PriceListItem       | PriceListId, ProductId, UnitPrice, MinQuantity                | → PriceList, → Product                  |
| Discount            | Name, Type (Percentage/Fixed), Value, MinQty, MaxQty, StartDate, EndDate | → Products / Categories          |
| SalesQuote          | CustomerId, QuoteNumber, Status, ValidUntil, TotalAmount, Version | → Customer, → SalesQuoteItems       |
| SalesQuoteItem      | SalesQuoteId, ProductId, Quantity, UnitPrice, Discount, LineTotal | → SalesQuote, → Product              |
| Currency            | Code (ISO 4217), Name, Symbol, DecimalPlaces, IsBaseCurrency  | → ExchangeRates, → Transactions         |
| ExchangeRate        | FromCurrencyId, ToCurrencyId, Rate, EffectiveDate              | → Currency (x2)                         |
| CreditNote          | InvoiceId, CreditNoteNumber, Date, Amount, Reason, Status      | → Invoice                               |
| DebitNote           | InvoiceId, DebitNoteNumber, Date, Amount, Reason, Status        | → Invoice                               |
| DeliveryNote        | SalesOrderId, DeliveryNoteNumber, Date, ShippedBy, TrackingNo  | → SalesOrder                            |
| Shipment            | SalesOrderId, CarrierName, TrackingNumber, ShipDate, EstDelivery, Status | → SalesOrder                   |
| Stocktake           | WarehouseId, StartDate, EndDate, Status, CreatedBy              | → Warehouse, → StocktakeItems           |
| StocktakeItem       | StocktakeId, ProductId, SystemQty, CountedQty, Variance         | → Stocktake, → Product                  |
| UnitOfMeasure       | Name, Abbreviation, IsBaseUnit                                  | → Products, → UoMConversions            |
| UoMConversion       | FromUoMId, ToUoMId, ConversionFactor                            | → UnitOfMeasure (x2)                    |
| DocumentTemplate    | Name, Type, HtmlContent, IsDefault, TenantId                    | → (used by doc generation engine)       |
| NotificationTemplate| EventType, Channel (Email/SMS/InApp), Subject, BodyTemplate     | → (used by notification engine)         |
| NotificationLog     | UserId, EventType, Channel, Status, SentAt, ErrorMessage         | → ApplicationUser                       |
| WebhookSubscription | TenantId, EventType, Url, Secret, IsActive, CreatedAt           | → (outbound webhook)                    |
| WebhookDeliveryLog  | SubscriptionId, EventType, Payload, StatusCode, Attempt, SentAt  | → WebhookSubscription                   |
| CustomFieldDefinition| EntityType, FieldName, FieldType, IsRequired, Options, SortOrder | → (EAV metadata)                       |
| CustomFieldValue    | DefinitionId, EntityId, Value                                    | → CustomFieldDefinition                 |
| PurchaseReturn      | PurchaseOrderId, ReturnNumber, Status, TotalAmount, Reason       | → PurchaseOrder, → PurchaseReturnItems  |
| PurchaseReturnItem  | PurchaseReturnId, ProductId, Quantity, UnitCost, LineTotal       | → PurchaseReturn, → Product             |
| RfqRequest          | RfqNumber, Status, RequiredDate, Notes                           | → RfqItems, → RfqSupplierResponses      |
| RfqItem             | RfqRequestId, ProductId, Quantity, TargetUnitPrice               | → RfqRequest, → Product                 |
| RfqSupplierResponse | RfqRequestId, SupplierId, QuotedPrice, LeadTimeDays, ValidUntil  | → RfqRequest, → Supplier                |
| LandedCostComponent | Name, Type (Freight/Duty/Insurance/Handling), DefaultRate         | → LandedCostAllocations                 |
| LandedCostAllocation| GRNId, ComponentId, Amount, AllocationMethod                      | → LandedCostComponent                   |
| Kit                 | ProductId (kit SKU), IsActive                                    | → Product, → KitComponents              |
| KitComponent        | KitId, ComponentProductId, Quantity                               | → Kit, → Product                        |
| ApprovalWorkflowRule| DocumentType, Condition, ApproverRoleOrUserId, Level, IsActive   | → (workflow engine)                     |
| ApprovalRequest     | DocumentType, DocumentId, Status, CurrentLevel                    | → ApprovalRequestSteps                  |
| ApprovalRequestStep | ApprovalRequestId, ApproverId, Level, Decision, DecidedAt, Notes  | → ApprovalRequest, → ApplicationUser    |
| ArchivalPolicy      | EntityType, RetentionDays, IsActive, LastRunAt                    | → (background archival job)             |
| DataSubjectRequest  | RequestType (Access/Erasure), SubjectEmail, Status, CompletedAt   | → (GDPR compliance)                     |
| ScheduledReport     | ReportType, CronExpression, Recipients, Format, IsActive, LastRunAt | → (report scheduler)                  |
| BankStatement       | BankAccountName, StatementDate, FileName, ImportedAt              | → BankStatementLines                    |
| BankStatementLine   | StatementId, Date, Description, Amount, Reference, MatchedPaymentId | → BankStatement, → Payment            |

### 8.3 Enums

```csharp
public enum OrderStatus
{
    Pending, Approved, Packed, Shipped, PartiallyShipped, Delivered, Cancelled
}

public enum PurchaseOrderStatus
{
    Draft, Submitted, Approved, PartiallyReceived, Received, Closed, Cancelled
}

public enum AccountType
{
    Asset, Liability, Equity, Revenue, Expense
}

public enum StockMovementType
{
    In, Out, Transfer, Adjustment, Return, WriteOff, KitAssembly, KitDisassembly
}

public enum PaymentMethod
{
    Cash, BankTransfer, Check, CreditCard, Other
}

public enum QuoteStatus
{
    Draft, Sent, Accepted, Rejected, Expired, Converted
}

public enum TaxType
{
    VAT, GST, SalesTax, ServiceTax, Exempt
}

public enum DiscountType
{
    Percentage, FixedAmount
}

public enum CreditNoteStatus
{
    Draft, Approved, Applied, Cancelled
}

public enum ShipmentStatus
{
    Pending, InTransit, Delivered, Failed, Returned
}

public enum StocktakeStatus
{
    Draft, InProgress, PendingApproval, Approved, Cancelled
}

public enum PurchaseReturnStatus
{
    Draft, Approved, Shipped, Completed, Cancelled
}

public enum RfqStatus
{
    Draft, Sent, Received, Awarded, Closed, Cancelled
}

public enum ApprovalDecision
{
    Pending, Approved, Rejected, Escalated
}

public enum NotificationChannel
{
    InApp, Email, SMS
}

public enum CustomFieldType
{
    Text, Number, Date, Dropdown, Checkbox, MultiSelect
}

public enum DataSubjectRequestType
{
    Access, Erasure
}

public enum LandedCostAllocMethod
{
    ByValue, ByWeight, ByVolume, ByQuantity
}
```

### 8.4 EF Core DbContext

```csharp
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // Core
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }
    public DbSet<UoMConversion> UoMConversions { get; set; }

    // Orders
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    // Quotations
    public DbSet<SalesQuote> SalesQuotes { get; set; }
    public DbSet<SalesQuoteItem> SalesQuoteItems { get; set; }
    public DbSet<RfqRequest> RfqRequests { get; set; }
    public DbSet<RfqItem> RfqItems { get; set; }
    public DbSet<RfqSupplierResponse> RfqSupplierResponses { get; set; }

    // Pricing & Tax
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListItem> PriceListItems { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<TaxRate> TaxRates { get; set; }
    public DbSet<TaxJurisdiction> TaxJurisdictions { get; set; }
    public DbSet<TaxExemption> TaxExemptions { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    // Accounting
    public DbSet<Account> Accounts { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<CreditNote> CreditNotes { get; set; }
    public DbSet<DebitNote> DebitNotes { get; set; }
    public DbSet<BankStatement> BankStatements { get; set; }
    public DbSet<BankStatementLine> BankStatementLines { get; set; }

    // Shipping & Logistics
    public DbSet<DeliveryNote> DeliveryNotes { get; set; }
    public DbSet<Shipment> Shipments { get; set; }

    // Stocktake
    public DbSet<Stocktake> Stocktakes { get; set; }
    public DbSet<StocktakeItem> StocktakeItems { get; set; }

    // Returns
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
    public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; }

    // Kitting
    public DbSet<Kit> Kits { get; set; }
    public DbSet<KitComponent> KitComponents { get; set; }

    // Landed Cost
    public DbSet<LandedCostComponent> LandedCostComponents { get; set; }
    public DbSet<LandedCostAllocation> LandedCostAllocations { get; set; }

    // Workflow & Approvals
    public DbSet<ApprovalWorkflowRule> ApprovalWorkflowRules { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<ApprovalRequestStep> ApprovalRequestSteps { get; set; }

    // Documents & Notifications
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; set; }

    // Custom Fields
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; set; }
    public DbSet<CustomFieldValue> CustomFieldValues { get; set; }

    // GDPR & Archival
    public DbSet<DataSubjectRequest> DataSubjectRequests { get; set; }
    public DbSet<ArchivalPolicy> ArchivalPolicies { get; set; }

    // Scheduled Reports
    public DbSet<ScheduledReport> ScheduledReports { get; set; }

    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; }
}
```

**DbContext Behaviors:**
- Global query filters for multi-tenant isolation (`entity.TenantId == currentTenantId`)
- Automatic `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` population on `SaveChangesAsync`
- Automatic audit log capture for all tracked entity changes

---

## 9. Application Layer Design

### 9.1 CQRS with MediatR

All business operations are modeled as commands (writes) or queries (reads), dispatched through MediatR.

#### Commands

| Command                        | Description                                                  |
| ------------------------------ | ------------------------------------------------------------ |
| CreateProductCommand           | Create a new product with SKU, price, category               |
| UpdateProductCommand           | Update product details                                       |
| DeleteProductCommand           | Soft-delete a product                                        |
| AdjustStockCommand             | Manually adjust stock quantity for a product in a warehouse  |
| TransferStockCommand           | Move stock between two warehouses                            |
| CreateSalesOrderCommand        | Create a sales order, validate stock, reserve items          |
| ApproveSalesOrderCommand       | Approve order, deduct stock, post journal entry              |
| CancelSalesOrderCommand        | Cancel order, restore stock, reverse journal entry           |
| CreatePurchaseOrderCommand     | Create a PO linked to a supplier                             |
| ReceiveGoodsCommand            | Record GRN, increase stock, post journal entry               |
| CreateJournalEntryCommand      | Create a manual double-entry journal entry                   |
| CreateCustomerCommand          | Register a new customer                                      |
| CreateSupplierCommand          | Register a new supplier                                      |
| CreateSalesQuoteCommand        | Create a sales quote with items and pricing                  |
| ConvertQuoteToOrderCommand     | Convert an accepted quote into a sales order                 |
| UpdateQuoteStatusCommand       | Progress quote through status workflow                       |
| CreatePurchaseReturnCommand    | Create a return against a purchase order/GRN                 |
| ApprovePurchaseReturnCommand   | Approve purchase return and deduct stock                     |
| CreateRfqRequestCommand        | Create an RFQ and send to suppliers                          |
| RecordRfqResponseCommand       | Record a supplier's quotation response                       |
| ConvertRfqToPurchaseOrderCommand| Convert selected RFQ response into a PO                     |
| CreateCreditNoteCommand        | Issue a credit note against a sales invoice                  |
| CreateDebitNoteCommand         | Issue a debit note against a purchase invoice                |
| ConfigureTaxRateCommand        | Create or update a tax rate configuration                    |
| ConfigureTaxJurisdictionCommand| Create or update a tax jurisdiction                          |
| CreatePriceListCommand         | Create a new price list with items                           |
| UpdatePriceListCommand         | Update price list items and validity dates                   |
| ApplyDiscountCommand           | Apply a discount to an order or line item                    |
| CreateStocktakeCommand         | Initiate a stocktake session for a warehouse                 |
| RecordStocktakeCountCommand    | Record counted quantities for stocktake items                |
| ApproveStocktakeCommand        | Approve stocktake variances and adjust stock                 |
| AssembleKitCommand             | Assemble a kit from component products                       |
| DisassembleKitCommand          | Disassemble a kit back into components                       |
| CreateDeliveryNoteCommand      | Generate a delivery note for a shipped order                 |
| RecordShipmentCommand          | Record shipment details (carrier, tracking, date)            |
| ImportBankStatementCommand     | Import a bank statement file for reconciliation              |
| MatchBankStatementLineCommand  | Match a bank statement line to a payment                     |
| ApproveReconciliationCommand   | Approve bank reconciliation and lock the period              |
| ConfigureCurrencyCommand       | Add or update a currency definition                          |
| UpdateExchangeRateCommand      | Update exchange rates for a currency pair                    |
| CreateDocumentTemplateCommand  | Create or update a document template                         |
| GenerateDocumentCommand        | Generate a PDF document from a template and data             |
| SendNotificationCommand        | Send an email/SMS/in-app notification                        |
| ConfigureNotificationPrefsCmd  | Update a user's notification preferences                     |
| RegisterWebhookCommand         | Register a new webhook subscription                          |
| DefineCustomFieldCommand       | Define a custom field for an entity type                     |
| SetCustomFieldValueCommand     | Set a custom field value on an entity                        |
| BulkImportProductsCommand      | Import products from CSV/Excel file                          |
| BulkImportCustomersCommand     | Import customers from CSV/Excel file                         |
| ImportOpeningStockCommand      | Import opening inventory balances                            |
| ImportOpeningBalancesCommand   | Import chart of accounts opening balances                    |
| ConfigureApprovalRuleCommand   | Define an approval workflow rule                             |
| SubmitForApprovalCommand       | Submit a document for workflow approval                      |
| ProcessApprovalDecisionCommand | Record an approval/rejection decision                        |
| AllocateLandedCostCommand      | Allocate landed costs to a GRN                               |
| WriteOffDeadStockCommand       | Write off dead/obsolete stock with reason                    |
| CreateDataSubjectRequestCmd    | Record a GDPR data subject access/erasure request            |
| ProcessDataSubjectRequestCmd   | Process (export/anonymize) a data subject request            |
| ConfigureArchivalPolicyCommand | Define a data archival/retention policy                      |
| ScheduleReportCommand          | Configure a scheduled report with recipients                 |

#### Queries

| Query                        | Description                                          |
| ---------------------------- | ---------------------------------------------------- |
| GetProductsQuery             | List products with filtering, sorting, pagination    |
| GetProductByIdQuery          | Single product with category and stock info          |
| GetInventoryByWarehouseQuery | Stock levels for all products in a warehouse         |
| GetSalesOrdersQuery          | List orders with status filtering and pagination     |
| GetSalesOrderByIdQuery       | Single order with items and customer info            |
| GetPurchaseOrdersQuery       | List POs with status and supplier info               |
| GetChartOfAccountsQuery      | Hierarchical account tree                            |
| GetTrialBalanceQuery         | Aggregated debit/credit totals per account           |
| GetProfitAndLossQuery        | Revenue and expenses for a date range                |
| GetBalanceSheetQuery         | Assets, liabilities, equity as of a date             |
| GetDashboardKPIsQuery        | Aggregated KPIs for the dashboard                    |
| GetAuditLogsQuery            | Filterable audit trail                               |
| GetSalesQuotesQuery          | List quotes with status filtering and pagination     |
| GetSalesQuoteByIdQuery       | Single quote with items and customer info            |
| GetPurchaseReturnsQuery      | List purchase returns with status filters            |
| GetRfqRequestsQuery          | List RFQs with status and supplier responses         |
| GetRfqComparisonQuery        | Side-by-side comparison of supplier RFQ responses    |
| GetCreditNotesQuery          | List credit notes with filters                       |
| GetDebitNotesQuery           | List debit notes with filters                        |
| GetTaxRatesQuery             | List all tax rates and jurisdictions                 |
| GetPriceListsQuery           | List price lists with items                          |
| GetResolvedPriceQuery        | Resolve effective price for a product + customer     |
| GetStocktakesQuery           | List stocktake sessions with status                  |
| GetStocktakeVarianceQuery    | Variance report for a stocktake session              |
| GetArAgingReportQuery        | Accounts receivable aging by customer                |
| GetApAgingReportQuery        | Accounts payable aging by supplier                   |
| GetCustomerStatementQuery    | Statement of account for a customer over a period    |
| GetBankReconciliationQuery   | Reconciliation view with matched/unmatched items     |
| GetCurrenciesQuery           | List all configured currencies and exchange rates    |
| GetDeliveryNotesQuery        | List delivery notes with order references            |
| GetShipmentsQuery            | List shipments with tracking info                    |
| GetKitsQuery                 | List all kit products with components                |
| GetLandedCostReportQuery     | Landed cost breakdown per PO/GRN                     |
| GetDeadStockReportQuery      | Products with zero movement beyond threshold         |
| GetDocumentTemplatesQuery    | List document templates by type                      |
| GetNotificationLogsQuery     | Notification delivery history                        |
| GetWebhookSubscriptionsQuery | List webhook subscriptions with delivery stats       |
| GetWebhookDeliveryLogsQuery  | Delivery logs for a webhook subscription             |
| GetCustomFieldsQuery         | List custom field definitions for an entity type     |
| GetCustomFieldValuesQuery    | Get custom field values for a specific entity        |
| GetApprovalPendingQuery      | List documents pending the current user's approval   |
| GetApprovalHistoryQuery      | Approval history for a document                      |
| GetDataSubjectRequestsQuery  | List GDPR data subject requests with status          |
| GetScheduledReportsQuery     | List configured scheduled reports                    |
| GetArchivalPoliciesQuery     | List data archival policies and last run status      |
| GlobalSearchQuery            | Unified search across products, orders, customers, suppliers |
| GetInventoryCostingQuery     | Average cost, last cost, standard cost per product   |
| GetAbcAnalysisQuery          | ABC analysis of inventory by revenue contribution    |

### 9.2 Validation Pipeline

FluentValidation runs automatically before every command handler via MediatR `IPipelineBehavior<TRequest, TResponse>`.

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### 9.3 Dependency Injection Configuration

```csharp
// Infrastructure
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// CQRS
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Mapping
builder.Services.AddAutoMapper(typeof(Program));

// Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// SignalR
builder.Services.AddSignalR();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
```

---

## 10. Infrastructure & Cross-Cutting Concerns

### 10.1 Repository Pattern

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
}
```

### 10.2 Multi-Tenancy

- `ITenantProvider` resolves the current tenant ID from authenticated user claims.
- All entities inherit `TenantId` from `BaseEntity`.
- EF Core global query filters enforce tenant isolation at the data layer.
- Tenant ID is set automatically on entity creation — never passed from the client.

### 10.3 Audit Logging

The `SaveChangesAsync` override in `AppDbContext` captures:
- Entity type and record ID
- Action type (Created, Updated, Deleted)
- Old and new values (serialized JSON)
- Authenticated user ID and timestamp

### 10.4 Real-Time Updates (SignalR)

`InventoryHub` broadcasts stock change events to all connected Blazor clients:
- Stock level changes (order, receipt, adjustment, transfer)
- Low stock alert triggers
- Order status transitions

---

## 11. Authentication & Authorization

| Aspect         | Implementation                                            |
| -------------- | --------------------------------------------------------- |
| Provider       | ASP.NET Core Identity                                     |
| Auth scheme    | Cookie-based (server-side Blazor)                         |
| Password policy| Minimum 8 chars, uppercase, lowercase, digit, special     |
| Roles          | Admin, Manager, Sales, Purchasing, Warehouse Staff, Viewer |
| Lockout        | 5 failed attempts → 15-minute lockout                     |

### Role Permissions Matrix

| Permission                       | Admin | Manager | Sales | Purchasing | Warehouse Staff | Viewer |
| -------------------------------- | :---: | :-----: | :---: | :--------: | :-------------: | :----: |
| Manage users & roles             |  Yes  |         |       |            |                 |        |
| View dashboard                   |  Yes  |   Yes   |  Yes  |     Yes    |       Yes       |   Yes  |
| Create/edit products             |  Yes  |   Yes   |       |            |                 |        |
| View inventory                   |  Yes  |   Yes   |  Yes  |     Yes    |       Yes       |   Yes  |
| Adjust stock                     |  Yes  |   Yes   |       |            |       Yes       |        |
| Transfer stock                   |  Yes  |   Yes   |       |            |       Yes       |        |
| Create sales quotes              |  Yes  |   Yes   |  Yes  |            |                 |        |
| Create sales orders              |  Yes  |   Yes   |  Yes  |            |                 |        |
| Approve sales orders             |  Yes  |   Yes   |       |            |                 |        |
| Cancel orders                    |  Yes  |   Yes   |       |            |                 |        |
| Pack/ship orders                 |  Yes  |   Yes   |       |            |       Yes       |        |
| Create purchase orders           |  Yes  |   Yes   |       |     Yes    |                 |        |
| Approve purchase orders          |  Yes  |   Yes   |       |            |                 |        |
| Receive goods (GRN)              |  Yes  |   Yes   |       |     Yes    |       Yes       |        |
| Create purchase returns          |  Yes  |   Yes   |       |     Yes    |                 |        |
| Create/manage RFQs               |  Yes  |   Yes   |       |     Yes    |                 |        |
| Manage customers                 |  Yes  |   Yes   |  Yes  |            |                 |        |
| Manage suppliers                 |  Yes  |   Yes   |       |     Yes    |                 |        |
| Manage chart of accounts         |  Yes  |         |       |            |                 |        |
| Create journal entries           |  Yes  |   Yes   |       |            |                 |        |
| Create credit/debit notes        |  Yes  |   Yes   |       |            |                 |        |
| View financial reports           |  Yes  |   Yes   |       |            |                 |        |
| Manage tax configuration         |  Yes  |         |       |            |                 |        |
| Manage price lists & discounts   |  Yes  |   Yes   |       |            |                 |        |
| Manage currencies & exchange rates|  Yes |         |       |            |                 |        |
| Bank reconciliation              |  Yes  |   Yes   |       |            |                 |        |
| Initiate & record stocktakes     |  Yes  |   Yes   |       |            |       Yes       |        |
| Approve stocktake variances      |  Yes  |   Yes   |       |            |                 |        |
| Manage kits (assembly/disassembly)|  Yes |   Yes   |       |            |       Yes       |        |
| View sales reports               |  Yes  |   Yes   |  Yes  |            |                 |   Yes  |
| View inventory reports           |  Yes  |   Yes   |       |     Yes    |       Yes       |   Yes  |
| Export reports (PDF/Excel)        |  Yes  |   Yes   |       |            |                 |        |
| View audit logs                  |  Yes  |   Yes   |       |            |                 |        |
| Manage document templates        |  Yes  |         |       |            |                 |        |
| Configure approval workflows     |  Yes  |         |       |            |                 |        |
| Process pending approvals        |  Yes  |   Yes   |       |            |                 |        |
| Manage webhooks                  |  Yes  |         |       |            |                 |        |
| Define custom fields             |  Yes  |         |       |            |                 |        |
| Bulk import/export               |  Yes  |   Yes   |       |            |                 |        |
| Configure scheduled reports      |  Yes  |   Yes   |       |            |                 |        |
| Manage GDPR data requests        |  Yes  |         |       |            |                 |        |
| Configure data archival policies |  Yes  |         |       |            |                 |        |
| Configure system settings        |  Yes  |         |       |            |                 |        |
| Manage notification preferences  |  Yes  |   Yes   |  Yes  |     Yes    |       Yes       |   Yes  |

---

## 12. UI Screens & User Flows

### Screen Inventory

| Screen                 | Route                    | Description                                    |
| ---------------------- | ------------------------ | ---------------------------------------------- |
| Login                  | `/login`                 | Email/password authentication                  |
| Dashboard              | `/`                      | KPIs, charts, alerts, quick actions            |
| Products               | `/products`              | Product data table with CRUD actions            |
| Product Detail         | `/products/{id}`         | View/edit single product with stock info        |
| Categories             | `/categories`            | Category tree management                       |
| Inventory              | `/inventory`             | Stock levels by warehouse, adjust stock         |
| Stock Transfers        | `/inventory/transfers`   | Create and track inter-warehouse transfers      |
| Sales Orders           | `/orders/sales`          | Sales order list with status filters            |
| Sales Order Detail     | `/orders/sales/{id}`     | View/edit order, manage status transitions      |
| Purchase Orders        | `/orders/purchase`       | PO list with status filters                     |
| Purchase Order Detail  | `/orders/purchase/{id}`  | View/edit PO, record goods receipt              |
| Customers              | `/customers`             | Customer list with CRUD                         |
| Suppliers              | `/suppliers`             | Supplier list with CRUD                         |
| Warehouses             | `/warehouses`                | Warehouse setup and configuration               |
| Chart of Accounts      | `/accounting/accounts`       | Account hierarchy management                    |
| Journal Entries        | `/accounting/journals`       | Create and view journal entries                  |
| Trial Balance          | `/reports/trial-balance`     | Debit/credit balance report                     |
| Profit & Loss          | `/reports/profit-loss`       | Income statement for a period                   |
| Balance Sheet          | `/reports/balance-sheet`     | Financial position at a date                    |
| Sales Reports          | `/reports/sales`             | Sales performance analytics                     |
| Inventory Reports      | `/reports/inventory`         | Valuation, movement, aging reports              |
| Audit Logs             | `/admin/audit-logs`          | Searchable audit trail                          |
| User Management        | `/admin/users`               | Create users, assign roles                      |
| Sales Quotes           | `/quotes/sales`              | Quote list with status filters                  |
| Sales Quote Detail     | `/quotes/sales/{id}`         | View/edit quote, convert to order               |
| Supplier RFQs          | `/quotes/rfq`                | RFQ list with status filters                    |
| RFQ Detail             | `/quotes/rfq/{id}`           | View responses, compare, convert to PO          |
| Tax Configuration      | `/settings/tax`              | Tax rates and jurisdiction management           |
| Price Lists            | `/settings/pricing`          | Price list management and item assignment        |
| Discounts              | `/settings/discounts`        | Configure volume, promotional, and custom discounts |
| Currency Management    | `/settings/currencies`       | Currency definitions and exchange rates          |
| Delivery Notes         | `/shipping/delivery-notes`   | List and view delivery notes                     |
| Shipments              | `/shipping/shipments`        | Shipment tracking and status management          |
| Stocktakes             | `/inventory/stocktakes`      | Cycle count sessions and variance reports        |
| Stocktake Detail       | `/inventory/stocktakes/{id}` | Record counts, view variances, approve           |
| Purchase Returns       | `/orders/purchase-returns`   | Purchase return list with status filters         |
| Credit Notes           | `/accounting/credit-notes`   | Credit note list and creation                    |
| Debit Notes            | `/accounting/debit-notes`    | Debit note list and creation                     |
| AR Aging Report        | `/reports/ar-aging`          | Accounts receivable aging analysis               |
| AP Aging Report        | `/reports/ap-aging`          | Accounts payable aging analysis                  |
| Customer Statements    | `/reports/customer-statements`| Customer account statements                     |
| Bank Reconciliation    | `/accounting/reconciliation` | Import statements, match payments, approve       |
| Kits                   | `/products/kits`             | Kit definition and assembly/disassembly          |
| Dead Stock Report      | `/reports/dead-stock`        | Non-moving inventory identification              |
| Landed Cost            | `/purchasing/landed-cost`    | Landed cost allocation per GRN                   |
| Document Templates     | `/settings/templates`        | Manage invoice, PO, quote, delivery templates    |
| Notification Settings  | `/settings/notifications`    | User notification preferences                    |
| Approval Workflows     | `/settings/workflows`        | Configure approval rules and chains              |
| My Approvals           | `/approvals`                 | Pending approval items for current user          |
| Webhook Management     | `/settings/webhooks`         | Register and monitor webhook subscriptions       |
| Custom Fields          | `/settings/custom-fields`    | Define custom fields per entity type             |
| Bulk Import            | `/admin/import`              | CSV/Excel import with validation preview         |
| Bulk Export             | `/admin/export`              | Export data tables to CSV/Excel                  |
| Scheduled Reports      | `/settings/scheduled-reports`| Configure automated report generation            |
| Data Privacy           | `/admin/data-privacy`        | GDPR data subject requests management            |
| Data Archival          | `/admin/archival`            | Archival policy configuration and status         |
| Global Search Results  | `/search`                    | Unified search results across all entities       |
| Tax Reports            | `/reports/tax`               | Tax summary reports by period/jurisdiction       |
| ABC Analysis           | `/reports/abc-analysis`      | Inventory ABC analysis by revenue contribution   |
| Inventory Costing      | `/reports/inventory-costing` | Cost breakdown per product (avg, last, standard) |

---

## 13. Key Business Workflows

### 13.1 Sales Order Flow

```
Customer places order
    │
    ▼
Create Sales Order (Status: Pending)
    │
    ├── Validate: Customer credit limit not exceeded
    ├── Validate: Requested quantities available in stock
    │
    ▼
Manager approves order (Status: Approved)
    │
    ├── Deduct stock from warehouse inventory
    ├── Create StockMovement records (Type: Out)
    ├── Auto-post journal entry:
    │     DR: Accounts Receivable (1100)
    │     CR: Sales Revenue (4000)
    │
    ▼
Warehouse packs items (Status: Packed)
    │
    ▼
Shipment dispatched (Status: Shipped)
    │
    ▼
Customer confirms delivery (Status: Delivered)
    │
    ▼
Payment received
    ├── Auto-post journal entry:
    │     DR: Cash (1000)
    │     CR: Accounts Receivable (1100)
```

### 13.2 Purchase Order Flow

```
Purchase requisition raised
    │
    ▼
Create Purchase Order (Status: Draft)
    │
    ▼
Approve PO (Status: Approved)
    │
    ▼
Receive goods at warehouse (GRN)
    │
    ├── Increase warehouse inventory
    ├── Create StockMovement records (Type: In)
    ├── Auto-post journal entry:
    │     DR: Inventory (1200)
    │     CR: Accounts Payable (2000)
    │
    ▼
Supplier invoice matched to PO
    │
    ▼
Payment to supplier
    ├── Auto-post journal entry:
    │     DR: Accounts Payable (2000)
    │     CR: Cash (1000)
```

### 13.3 Stock Transfer Flow

```
Initiate transfer (Source Warehouse → Target Warehouse)
    │
    ├── Validate: Source warehouse has sufficient stock
    │
    ▼
Deduct from source warehouse
    ├── StockMovement (Type: Transfer, Qty: -N, Warehouse: Source)
    │
    ▼
Add to target warehouse
    ├── StockMovement (Type: Transfer, Qty: +N, Warehouse: Target)
    │
    ▼
SignalR broadcasts updated stock levels to all clients
```

### 13.4 Return (RMA) Flow

```
Customer initiates return
    │
    ▼
Create RMA request
    │
    ├── Validate: Original order exists and is delivered
    │
    ▼
Approve return
    │
    ├── Increase stock in warehouse
    ├── Create StockMovement (Type: Return)
    ├── Auto-post reversal journal entry:
    │     DR: Sales Revenue (4000)
    │     CR: Accounts Receivable (1100)
    │
    ▼
Process refund or credit note
```

### 13.5 Sales Quote Flow

```
Sales creates quote
    │
    ▼
Create Sales Quote (Status: Draft)
    │
    ├── Add line items with pricing from price list / custom pricing
    ├── Apply discounts (volume, promotional, manual)
    ├── Calculate tax per line item
    │
    ▼
Send quote to customer (Status: Sent)
    │
    ├── Quote validity countdown begins
    │
    ▼
Customer response
    │
    ├── Accepted → Convert to Sales Order (one-click)
    │     ├── All line items, pricing, and discounts carried over
    │     ├── Quote status → Converted
    │
    ├── Rejected → Quote status → Rejected
    │
    └── No response → Quote auto-expires after validity period
```

### 13.6 Supplier RFQ Flow

```
Purchasing identifies need
    │
    ▼
Create RFQ (Status: Draft)
    │
    ├── Add required items with target quantities
    │
    ▼
Send RFQ to selected suppliers (Status: Sent)
    │
    ▼
Receive supplier quotations (Status: Received)
    │
    ├── Record each supplier's quoted prices, lead times, terms
    │
    ▼
Compare responses side-by-side
    │
    ├── Evaluate: price, lead time, payment terms, reliability
    │
    ▼
Award to selected supplier (Status: Awarded)
    │
    ├── Convert to Purchase Order (pre-filled with supplier, items, quoted prices)
    │
    ▼
RFQ closed
```

### 13.7 Purchase Return (Return-to-Supplier) Flow

```
Defective/incorrect goods identified
    │
    ▼
Create Purchase Return (Status: Draft)
    │
    ├── Link to original PO / GRN
    ├── Specify items, quantities, and return reason
    │
    ▼
Approve return (Status: Approved)
    │
    ├── Deduct stock from warehouse
    ├── Create StockMovement (Type: Return)
    ├── Auto-generate Debit Note
    ├── Auto-post journal entry:
    │     DR: Accounts Payable (2000)
    │     CR: Inventory (1200)
    │
    ▼
Ship goods back to supplier (Status: Shipped)
    │
    ▼
Supplier confirms receipt (Status: Completed)
```

### 13.8 Stocktake / Cycle Count Flow

```
Schedule or initiate stocktake
    │
    ▼
Create Stocktake session (Status: Draft)
    │
    ├── Select warehouse / zone / product subset
    ├── Generate count sheets with expected quantities
    │
    ▼
Begin counting (Status: InProgress)
    │
    ├── Staff record actual counted quantities
    ├── (Optional: blind mode hides expected quantities)
    │
    ▼
Generate variance report (Status: PendingApproval)
    │
    ├── system_qty vs counted_qty per product
    ├── Highlight significant variances
    │
    ▼
Manager approves variances (Status: Approved)
    │
    ├── Auto-adjust stock quantities
    ├── Create StockMovement records (Type: Adjustment)
    ├── Auto-post journal entry:
    │     DR: Inventory Adjustment Expense (surplus → CR)
    │     CR: Inventory (shortage → DR)
```

### 13.9 Credit Note Flow

```
Return approved / billing error identified
    │
    ▼
Create Credit Note against original invoice
    │
    ├── Specify amount (full or partial)
    ├── Link reason (RMA, pricing error, goodwill)
    │
    ▼
Approve credit note
    │
    ├── Reduce AR balance for customer
    ├── Auto-post journal entry:
    │     DR: Sales Revenue (4000) / Sales Returns
    │     CR: Accounts Receivable (1100)
    │
    ▼
Apply credit to future invoices or process refund
```

### 13.10 Bank Reconciliation Flow

```
Import bank statement (CSV/OFX)
    │
    ▼
Parse statement lines
    │
    ├── Date, description, amount, reference
    │
    ▼
Auto-match against recorded payments
    │
    ├── Match by amount + reference number
    ├── Flag unmatched statement lines
    ├── Flag unmatched system payments
    │
    ▼
Manual matching of remaining items
    │
    ├── User pairs statement lines with payments
    │
    ▼
Review reconciliation summary
    │
    ├── Matched, unmatched, discrepancies
    │
    ▼
Approve reconciliation → Lock period
```

### 13.11 Kit Assembly Flow

```
Kit order received or forecast demand
    │
    ▼
Initiate kit assembly
    │
    ├── Validate: All component products have sufficient stock
    │
    ▼
Assemble kits
    │
    ├── Deduct component stock (StockMovement Type: KitAssembly, Qty: -N per component)
    ├── Add kit stock (StockMovement Type: KitAssembly, Qty: +N for kit SKU)
    ├── Kit cost = sum of component costs
    │
    ▼
Kit available for sale
```

### 13.12 Multi-Currency Order Flow

```
Create order in customer/supplier currency
    │
    ├── Line items priced in transaction currency
    ├── Tax calculated in transaction currency
    │
    ▼
Convert to base currency at current exchange rate
    │
    ├── Journal entries posted in base currency
    ├── Original currency amount preserved on transaction
    │
    ▼
Payment received/made (potentially at different rate)
    │
    ├── Calculate realized exchange gain/loss
    ├── Auto-post gain/loss journal entry:
    │     Gain: DR: Cash, CR: AR + Exchange Gain
    │     Loss: DR: Cash + Exchange Loss, CR: AR
```

### 13.13 Approval Workflow

```
User submits document for approval
    │
    ▼
System evaluates approval rules
    │
    ├── Match rules by: document type, amount threshold, department
    ├── Determine approval chain (single-level or multi-level)
    │
    ▼
Notify first-level approver (in-app + email)
    │
    ▼
Approver reviews and decides
    │
    ├── Approved → move to next level (if multi-level)
    ├── Rejected → return to submitter with comments
    ├── No response within SLA → escalate to next approver
    │
    ▼
All levels approved → document proceeds
    │
    ├── Full audit trail of all decisions recorded
```

---

## 14. API & Service Contracts

### IInventoryService

```csharp
public interface IInventoryService
{
    Task<int> GetStockLevel(Guid productId, Guid warehouseId);
    Task AdjustStock(Guid productId, Guid warehouseId, int quantityChange, string reason);
    Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity);
    Task<List<LowStockAlertDto>> GetLowStockAlerts();
}
```

### IOrderService

```csharp
public interface IOrderService
{
    Task<Guid> CreateSalesOrder(CreateSalesOrderDto dto);
    Task ApproveOrder(Guid orderId);
    Task CancelOrder(Guid orderId, string reason);
    Task UpdateOrderStatus(Guid orderId, OrderStatus newStatus);
    Task<bool> ValidateStockAvailability(List<OrderItemDto> items, Guid warehouseId);
}
```

### IPurchaseService

```csharp
public interface IPurchaseService
{
    Task<Guid> CreatePurchaseOrder(CreatePurchaseOrderDto dto);
    Task ApprovePurchaseOrder(Guid poId);
    Task ReceiveGoods(Guid poId, List<GoodsReceivedLineDto> lines);
}
```

### IAccountingService

```csharp
public interface IAccountingService
{
    Task<Guid> CreateJournalEntry(CreateJournalEntryDto dto);
    Task AutoPostSalesEntry(Guid salesOrderId, decimal amount);
    Task AutoPostPurchaseEntry(Guid purchaseOrderId, decimal amount);
    Task<TrialBalanceDto> GetTrialBalance(DateTime asOfDate);
    Task<ProfitAndLossDto> GetProfitAndLoss(DateTime from, DateTime to);
    Task<BalanceSheetDto> GetBalanceSheet(DateTime asOfDate);
}
```

### ITaxService

```csharp
public interface ITaxService
{
    Task<TaxCalculationResult> CalculateTax(Guid productId, Guid customerId, Guid jurisdictionId, decimal amount);
    Task<List<TaxRateDto>> GetTaxRates(Guid? jurisdictionId = null);
    Task<TaxReportDto> GetTaxReport(DateTime from, DateTime to, Guid? jurisdictionId = null);
    Task<bool> IsExempt(Guid? customerId, Guid? productId);
}
```

### IPricingService

```csharp
public interface IPricingService
{
    Task<ResolvedPriceDto> ResolvePrice(Guid productId, Guid customerId, int quantity);
    Task<List<PriceListDto>> GetPriceLists(bool activeOnly = true);
    Task<DiscountResult> CalculateDiscount(Guid orderId, List<OrderItemDto> items);
    Task<bool> ValidateMinimumOrderQuantity(Guid productId, int quantity);
}
```

### IQuotationService

```csharp
public interface IQuotationService
{
    Task<Guid> CreateSalesQuote(CreateSalesQuoteDto dto);
    Task UpdateQuoteStatus(Guid quoteId, QuoteStatus status);
    Task<Guid> ConvertQuoteToOrder(Guid quoteId);
    Task<bool> IsQuoteExpired(Guid quoteId);
}
```

### IShippingService

```csharp
public interface IShippingService
{
    Task<Guid> CreateDeliveryNote(Guid salesOrderId);
    Task<Guid> RecordShipment(CreateShipmentDto dto);
    Task UpdateShipmentStatus(Guid shipmentId, ShipmentStatus status);
    Task<List<ShipmentDto>> GetShipmentsByOrder(Guid salesOrderId);
}
```

### IStocktakeService

```csharp
public interface IStocktakeService
{
    Task<Guid> CreateStocktake(Guid warehouseId, List<Guid> productIds);
    Task RecordCount(Guid stocktakeId, Guid productId, int countedQty);
    Task<StocktakeVarianceDto> GetVarianceReport(Guid stocktakeId);
    Task ApproveAndPostStocktake(Guid stocktakeId);
}
```

### ICurrencyService

```csharp
public interface ICurrencyService
{
    Task<decimal> Convert(decimal amount, Guid fromCurrencyId, Guid toCurrencyId, DateTime? asOfDate = null);
    Task<ExchangeRateDto> GetExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, DateTime? asOfDate = null);
    Task UpdateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate);
    Task<ExchangeGainLossDto> CalculateGainLoss(Guid invoiceId, Guid paymentId);
}
```

### IDocumentService

```csharp
public interface IDocumentService
{
    Task<byte[]> GeneratePdf(string templateType, Guid documentId);
    Task<List<DocumentTemplateDto>> GetTemplates(string documentType);
    Task<Guid> SaveTemplate(CreateDocumentTemplateDto dto);
}
```

### INotificationService

```csharp
public interface INotificationService
{
    Task SendEmail(string to, string subject, string body, List<AttachmentDto> attachments = null);
    Task SendSms(string phoneNumber, string message);
    Task SendInApp(Guid userId, string title, string message);
    Task<NotificationPreferencesDto> GetUserPreferences(Guid userId);
    Task UpdateUserPreferences(Guid userId, NotificationPreferencesDto prefs);
}
```

### IWebhookService

```csharp
public interface IWebhookService
{
    Task<Guid> RegisterWebhook(CreateWebhookDto dto);
    Task DeliverEvent(string eventType, object payload);
    Task RetryFailedDeliveries(Guid subscriptionId);
    Task<List<WebhookDeliveryLogDto>> GetDeliveryLogs(Guid subscriptionId);
}
```

### IBulkImportService

```csharp
public interface IBulkImportService
{
    Task<ImportValidationResult> ValidateImport(string entityType, Stream fileStream);
    Task<ImportResult> ExecuteImport(string entityType, Stream fileStream);
    Task RollbackImport(Guid importBatchId);
}
```

### IApprovalWorkflowService

```csharp
public interface IApprovalWorkflowService
{
    Task<Guid> SubmitForApproval(string documentType, Guid documentId);
    Task ProcessDecision(Guid approvalRequestId, Guid approverId, ApprovalDecision decision, string notes);
    Task<List<ApprovalPendingDto>> GetPendingApprovals(Guid userId);
    Task<bool> IsFullyApproved(Guid approvalRequestId);
}
```

### ICustomFieldService

```csharp
public interface ICustomFieldService
{
    Task<Guid> DefineField(CreateCustomFieldDto dto);
    Task SetValue(Guid definitionId, Guid entityId, string value);
    Task<List<CustomFieldWithValueDto>> GetFieldsForEntity(string entityType, Guid entityId);
}
```

### IBankReconciliationService

```csharp
public interface IBankReconciliationService
{
    Task<Guid> ImportStatement(Stream fileStream, string bankAccountName);
    Task<List<UnmatchedLineDto>> AutoMatch(Guid statementId);
    Task ManualMatch(Guid statementLineId, Guid paymentId);
    Task ApproveReconciliation(Guid statementId);
}
```

### IPurchaseReturnService

```csharp
public interface IPurchaseReturnService
{
    Task<Guid> CreateReturn(CreatePurchaseReturnDto dto);
    Task ApproveReturn(Guid returnId);
    Task<List<PurchaseReturnDto>> GetReturnsByPurchaseOrder(Guid purchaseOrderId);
}
```

### IRfqService

```csharp
public interface IRfqService
{
    Task<Guid> CreateRfq(CreateRfqDto dto);
    Task RecordSupplierResponse(CreateRfqResponseDto dto);
    Task<RfqComparisonDto> CompareResponses(Guid rfqId);
    Task<Guid> ConvertToPurchaseOrder(Guid rfqId, Guid selectedSupplierId);
}
```

### ILandedCostService

```csharp
public interface ILandedCostService
{
    Task AllocateCosts(Guid grnId, List<LandedCostLineDto> costLines, LandedCostAllocMethod method);
    Task<LandedCostReportDto> GetLandedCostReport(Guid purchaseOrderId);
}
```

### IKitService

```csharp
public interface IKitService
{
    Task<Guid> DefineKit(Guid kitProductId, List<KitComponentDto> components);
    Task AssembleKit(Guid kitId, Guid warehouseId, int quantity);
    Task DisassembleKit(Guid kitId, Guid warehouseId, int quantity);
}
```

### IGdprService

```csharp
public interface IGdprService
{
    Task<Guid> CreateDataSubjectRequest(DataSubjectRequestType type, string subjectEmail);
    Task<byte[]> ExportSubjectData(Guid requestId);
    Task AnonymizeSubjectData(Guid requestId);
}
```

### ISearchService

```csharp
public interface ISearchService
{
    Task<GlobalSearchResultDto> Search(string query, int maxResults = 20);
    Task RebuildIndex(string entityType = null);
}
```

---

## 15. Non-Functional Requirements

| Category          | Requirement                                                      | Target                   |
| ----------------- | ---------------------------------------------------------------- | ------------------------ |
| Performance       | Page load time                                                   | < 2 seconds              |
| Performance       | Dashboard KPI refresh                                            | < 1 second               |
| Performance       | API response time (95th percentile)                              | < 500ms                  |
| Performance       | Full-text search response                                        | < 200ms                  |
| Performance       | Concurrent users                                                 | 500+ simultaneous        |
| Performance       | PDF document generation                                          | < 3 seconds              |
| Scalability       | Product catalog size                                             | 100,000+ SKUs            |
| Scalability       | Order volume                                                     | 10,000+ orders/day       |
| Scalability       | Multi-tenant capacity                                            | 50+ isolated tenants     |
| Scalability       | Webhook delivery throughput                                      | 1,000 events/minute      |
| Availability      | System uptime                                                    | 99.9%                    |
| Availability      | Zero-downtime deployments                                        | Required                 |
| Availability      | RPO (Recovery Point Objective)                                   | < 1 hour                 |
| Availability      | RTO (Recovery Time Objective)                                    | < 4 hours                |
| Security          | OWASP Top 10 compliance                                          | All categories addressed |
| Security          | Data encryption at rest                                          | AES-256                  |
| Security          | Data encryption in transit                                       | TLS 1.2+                |
| Security          | API rate limiting                                                | Enforced per tenant      |
| Security          | Webhook payload signing                                          | HMAC-SHA256              |
| Security          | GDPR compliance                                                  | Full compliance          |
| Security          | Password policy                                                  | Min 8 chars, upper, lower, digit, special |
| Security          | Account lockout                                                  | 5 failed attempts → 15 min lockout |
| Data Integrity    | Double-entry balance enforcement                                 | Zero tolerance           |
| Data Integrity    | Multi-tenant isolation                                           | No data leakage          |
| Data Integrity    | Optimistic concurrency on stock mutations                        | EF Core RowVersion       |
| Data Integrity    | Tax calculation accuracy                                         | Rounding to 2 decimal places per jurisdiction rules |
| Data Integrity    | Multi-currency conversion accuracy                               | 6 decimal places on exchange rates |
| Audit             | Full audit trail for all data mutations                          | Retained 7 years         |
| Audit             | Privacy audit log for data access events                         | Retained per GDPR        |
| Backup            | Database backup frequency                                        | Daily full, hourly diff  |
| Backup            | Backup retention                                                 | 30 days                  |
| Accessibility     | WCAG 2.1 Level AA                                                | All web interfaces       |
| Localization      | UTF-8 encoding throughout                                        | Required                 |
| Localization      | Configurable currency and date formats per tenant                | Required                 |
| Browser Support   | Latest two versions of Chrome, Firefox, Edge, Safari             | Required                 |
| Observability     | Structured logging with correlation IDs                          | All services             |
| Observability     | Health check endpoints                                           | `/health` for all dependencies |
| Email             | Transactional email delivery                                     | < 30 seconds             |
| Email             | Failed email retry                                               | Up to 3 retries          |

---

## 16. Development Phases & Roadmap

### Phase 1 — Foundation (Core Infrastructure)

**Objective:** Establish project structure, authentication, and basic CRUD operations.

| Deliverable                                      | Status      |
| ------------------------------------------------ | ----------- |
| Clean Architecture solution setup                | Not Started |
| EF Core DbContext + initial migrations           | Not Started |
| ASP.NET Core Identity + login/register           | Not Started |
| Role-based authorization (Admin, Manager, Staff, Sales, Purchasing, Warehouse Staff, Viewer) | Not Started |
| Product & Category CRUD                          | Not Started |
| Warehouse setup CRUD                             | Not Started |
| Basic inventory tracking (stock levels)          | Not Started |
| Unit of measure (UoM) definition and conversion  | Not Started |
| Dashboard with placeholder KPIs                  | Not Started |
| MudBlazor layout and navigation                  | Not Started |
| Global search bar (basic)                        | Not Started |

**Exit Criteria:** Users can log in, manage products, view stock levels, and see a basic dashboard.

---

### Phase 2 — Order Management & Pricing

**Objective:** Implement full sales and purchase order lifecycles with pricing and tax support.

| Deliverable                                 | Status      |
| ------------------------------------------- | ----------- |
| Customer CRUD                               | Not Started |
| Supplier CRUD                               | Not Started |
| Tax rate & jurisdiction configuration       | Not Started |
| Price list management                       | Not Started |
| Discount engine (volume, promotional, line/order level) | Not Started |
| Sales quote creation & workflow             | Not Started |
| Quote-to-order conversion                   | Not Started |
| Sales order creation + items + tax          | Not Started |
| Order status workflow engine                | Not Started |
| Stock deduction on order approval           | Not Started |
| Order cancellation with stock restore       | Not Started |
| Purchase order creation                     | Not Started |
| Supplier RFQ process                        | Not Started |
| RFQ-to-PO conversion                       | Not Started |
| Goods receiving note (GRN)                  | Not Started |
| Stock movement logging                      | Not Started |
| Invoice generation from orders              | Not Started |
| Minimum order quantity enforcement          | Not Started |

**Exit Criteria:** Complete sales and purchase order flows with pricing, tax, discounts, and automatic stock adjustments.

---

### Phase 3 — Accounting & Financial Engine

**Objective:** Implement double-entry accounting, multi-currency, and financial reporting.

| Deliverable                                 | Status      |
| ------------------------------------------- | ----------- |
| Chart of Accounts management                | Not Started |
| Journal entry creation (manual)             | Not Started |
| Double-entry balance enforcement            | Not Started |
| Auto-posting from sales orders              | Not Started |
| Auto-posting from purchase orders           | Not Started |
| General Ledger view                         | Not Started |
| Accounts Receivable (AR) management         | Not Started |
| Accounts Payable (AP) management            | Not Started |
| AR aging report (30/60/90/120 day buckets)  | Not Started |
| AP aging report                             | Not Started |
| Customer statements generation              | Not Started |
| Credit note workflow                        | Not Started |
| Debit note workflow                         | Not Started |
| Trial Balance report                        | Not Started |
| Profit & Loss statement                     | Not Started |
| Balance Sheet report                        | Not Started |
| Multi-currency definitions & exchange rates | Not Started |
| Transaction currency on orders              | Not Started |
| Realized exchange gain/loss calculation     | Not Started |
| Bank reconciliation (import, match, approve)| Not Started |
| Payment recording and invoice matching      | Not Started |

**Exit Criteria:** All financial reports balance correctly; multi-currency supported; bank reconciliation functional.

---

### Phase 4 — Advanced Operations & Warehouse

**Objective:** Add enterprise-grade operational features for warehouse, shipping, and inventory.

| Deliverable                                  | Status      |
| -------------------------------------------- | ----------- |
| MediatR CQRS full integration                | Not Started |
| FluentValidation pipeline                    | Not Started |
| Audit logging (SaveChanges override)         | Not Started |
| SignalR real-time stock updates               | Not Started |
| Low stock alerts & notifications              | Not Started |
| Stock transfer between warehouses             | Not Started |
| Bin/location tracking                         | Not Started |
| Customer returns / RMA workflow               | Not Started |
| Purchase returns (return-to-supplier)         | Not Started |
| Partial fulfillment                           | Not Started |
| Stocktake / cycle count workflow              | Not Started |
| Delivery note & packing slip generation       | Not Started |
| Shipment tracking (carrier, tracking no.)     | Not Started |
| Delivery scheduling                           | Not Started |
| Freight cost allocation on orders             | Not Started |
| Kitting / simple assembly                     | Not Started |
| Dead stock / obsolescence management          | Not Started |
| Landed cost calculation & allocation          | Not Started |
| Batch/serial number tracking                  | Not Started |
| Expiry management (FIFO/FEFO)                 | Not Started |
| Picking & packing lists                       | Not Started |

**Exit Criteria:** Real-time updates working; full audit trail captured; all advanced warehouse and logistics workflows functional.

---

### Phase 5 — Documents, Notifications & Workflows

**Objective:** Implement document generation, communication, approval workflows, and extensibility.

| Deliverable                                          | Status      |
| ---------------------------------------------------- | ----------- |
| Document template engine (invoice, PO, quote, DN)    | Not Started |
| PDF generation for all document types                 | Not Started |
| Email notification system (transactional)             | Not Started |
| SMS notification support (optional)                   | Not Started |
| Notification preference management per user           | Not Started |
| Configurable approval workflow engine                 | Not Started |
| Multi-level approval chains                           | Not Started |
| Approval delegation & escalation                      | Not Started |
| Custom fields / user-defined fields per entity        | Not Started |
| Product image & document attachment support           | Not Started |
| Bulk import/export (CSV/Excel)                        | Not Started |
| Opening balance & opening stock import                | Not Started |
| Scheduled/automated report generation                 | Not Started |
| Report email delivery to recipients                   | Not Started |

**Exit Criteria:** Documents auto-generate; notifications delivered reliably; approval workflows block/allow progression correctly; bulk import operational.

---

### Phase 6 — Integration & Events

**Objective:** Enable third-party integration, full-text search, and event-driven communication.

| Deliverable                                    | Status      |
| ---------------------------------------------- | ----------- |
| Webhook registration & event system            | Not Started |
| Webhook HMAC signing & delivery retry          | Not Started |
| Webhook delivery logging & monitoring          | Not Started |
| Full-text search (Elasticsearch / SQL FTS)     | Not Started |
| Global search across all entity types          | Not Started |
| Search suggestions / autocomplete              | Not Started |
| API rate limiting & throttling (per tenant)    | Not Started |
| REST API versioning strategy                   | Not Started |
| Swagger / OpenAPI documentation                | Not Started |

**Exit Criteria:** Webhooks deliver reliably; global search returns results in < 200ms; API rate limits enforced.

---

### Phase 7 — Compliance, Privacy & Data Management

**Objective:** Implement GDPR compliance, data archival, and governance features.

| Deliverable                                    | Status      |
| ---------------------------------------------- | ----------- |
| GDPR data subject access request handling      | Not Started |
| Right to erasure (anonymization)               | Not Started |
| Consent management for communications          | Not Started |
| Data retention policy configuration            | Not Started |
| Automatic data archival background job          | Not Started |
| Privacy audit log for data access events        | Not Started |
| Archive query access (read-only)               | Not Started |
| Tax reporting & tax audit trail                | Not Started |
| Inventory costing reports (avg/last/standard)  | Not Started |
| ABC analysis of inventory                      | Not Started |
| Contract pricing for customers & suppliers     | Not Started |

**Exit Criteria:** GDPR compliance verified; archival running automatically; all compliance reports available.

---

### Phase 8 — SaaS & Scale

**Objective:** Prepare the system for multi-tenant deployment and horizontal scaling.

| Deliverable                                    | Status      |
| ---------------------------------------------- | ----------- |
| Multi-tenant isolation (global query filters)  | Not Started |
| Tenant onboarding workflow                     | Not Started |
| Redis distributed caching                      | Not Started |
| Event-driven architecture (optional)           | Not Started |
| Docker containerization                        | Not Started |
| Kubernetes orchestration                       | Not Started |
| CI/CD pipeline (GitHub Actions)                | Not Started |
| Performance testing & optimization             | Not Started |
| Unrealized exchange gain/loss (period-end revaluation) | Not Started |
| Multi-language / i18n support                  | Not Started |
| Barcode scanning integration (mobile/desktop)  | Not Started |
| Third-party integrations (ERP/POS/eCommerce API) | Not Started |
| AI-assisted demand forecasting                 | Not Started |
| Supplier self-service portal                   | Not Started |

**Exit Criteria:** System supports 50+ tenants with full data isolation; deployed via CI/CD to cloud infrastructure.

---

## 17. Risk Assessment & Mitigation

| ID   | Risk                                      | Probability | Impact   | Mitigation Strategy                                          |
| ---- | ----------------------------------------- | ----------- | -------- | ------------------------------------------------------------ |
| R-01 | Double-entry balance integrity failure     | Low         | Critical | Enforce balance validation in domain layer; DB constraints    |
| R-02 | Multi-tenant data leakage                 | Low         | Critical | Global query filters + integration tests per tenant           |
| R-03 | Performance degradation under load        | Medium      | High     | Indexed queries, pagination, Redis caching, load testing      |
| R-04 | Concurrent stock modification conflicts   | Medium      | High     | Optimistic concurrency (EF Core RowVersion) on Inventory      |
| R-05 | Scope creep from stakeholder requests     | High        | Medium   | Strict phase gating; changes require PO approval              |
| R-06 | Complex accounting rules misimplemented   | Medium      | High     | Dedicated accounting test suite; domain expert review          |
| R-07 | SignalR connection scalability             | Medium      | Medium   | Azure SignalR Service for production; connection management    |
| R-08 | Migration failures on schema changes      | Low         | Medium   | EF Core migration testing in staging; rollback scripts         |
| R-09 | Tax calculation errors across jurisdictions | Medium     | High     | Comprehensive tax test suite; immutable tax records on transactions |
| R-10 | Multi-currency exchange rate discrepancies | Medium      | High     | Lock exchange rate at transaction time; 6-decimal precision    |
| R-11 | Webhook delivery failures causing data gaps| Medium      | Medium   | Exponential backoff retry; dead-letter queue; delivery dashboard |
| R-12 | GDPR non-compliance penalties             | Low         | Critical | Privacy-by-design; automated retention; legal review of flows  |
| R-13 | Bulk import corrupting data               | Medium      | High     | Dry-run validation before commit; import rollback capability   |
| R-14 | Document template rendering failures      | Low         | Medium   | Template preview/testing; fallback to default templates        |
| R-15 | Email delivery failures affecting business| Medium      | Medium   | Retry queue; delivery logging; fallback to in-app notifications |
| R-16 | Approval workflow bottlenecks             | Medium      | Medium   | Auto-escalation rules; delegation support; SLA monitoring      |
| R-17 | Custom fields impacting query performance | Medium      | Medium   | JSON column storage with indexed computed columns; performance testing |
| R-18 | Data archival affecting referenced records| Low         | High     | Referential integrity checks before archival; soft-archive approach |
| R-19 | Landed cost miscalculation affecting COGS | Medium      | High     | Allocation validation; reconciliation reports; accountant review |
| R-20 | Kit assembly creating stock inconsistencies| Low        | High     | Atomic transactions; component validation before assembly       |

---

## 18. Testing Strategy

### Test Pyramid

| Level              | Scope                                                | Tools                         | Coverage Target |
| ------------------ | ---------------------------------------------------- | ----------------------------- | --------------- |
| Unit Tests         | Domain logic, validators, service methods            | xUnit, Moq, FluentAssertions | > 80%           |
| Integration Tests  | EF Core repositories, MediatR handlers, API endpoints| xUnit, TestContainers (SQL)  | > 70%           |
| End-to-End Tests   | Full UI workflows via Blazor test host               | bUnit, Playwright            | Top 15 flows    |
| Load Tests         | API throughput under concurrent load                 | k6                           | 500 concurrent  |
| Security Tests     | OWASP Top 10 vulnerabilities                         | OWASP ZAP (CI pipeline)      | Zero high/crit  |

### Critical Test Scenarios

- **Accounting:** Every journal entry balances; Trial Balance always sums to zero difference.
- **Inventory:** Stock never goes negative; concurrent adjustments resolve correctly.
- **Orders:** Approval deducts exact quantities; cancellation fully restores stock.
- **Security:** Tenant A cannot access Tenant B data; role restrictions enforced on all endpoints.
- **Audit:** Every mutation creates an audit log entry with correct old/new values.
- **Tax:** Tax calculation produces correct amounts per jurisdiction; tax-exempt entities skip tax.
- **Multi-currency:** Exchange gain/loss calculated correctly; base currency GL entries always balance.
- **Pricing:** Price resolution follows hierarchy (contract > customer > promotional > default).
- **Stocktake:** Variance approval adjusts stock and posts journal entries atomically.
- **Webhooks:** Failed deliveries retry with backoff; payloads are HMAC-signed.
- **Bulk import:** Invalid rows are rejected without affecting valid rows; duplicate detection works.
- **Approval workflow:** Documents cannot progress without required approvals; escalation triggers on SLA breach.
- **GDPR:** Data export contains all PII; erasure anonymizes without breaking financial records.
- **Credit/debit notes:** Notes cannot exceed original invoice amount; journal entries reverse correctly.
- **Kitting:** Assembly deducts components and creates kit stock atomically; insufficient component stock blocks assembly.
- **Bank reconciliation:** Auto-match correctly pairs by amount and reference; reconciled periods are locked.

---

## 19. Deployment Strategy

### Local Development

- SQL Server LocalDB or Docker SQL Server container
- Kestrel development server
- EF Core migrations applied via CLI

### Staging / Production

| Component         | Technology                             |
| ----------------- | -------------------------------------- |
| Hosting           | Azure App Service or Docker + K8s      |
| Database          | Azure SQL Database (managed)           |
| Cache             | Azure Cache for Redis                  |
| Real-time         | Azure SignalR Service                  |
| File Storage      | Azure Blob Storage / MinIO             |
| Email Service     | SendGrid / Azure Communication Services|
| Search            | Elasticsearch 8 / Azure Cognitive Search|
| CI/CD             | GitHub Actions or Azure DevOps         |
| Monitoring / APM  | Application Insights / Grafana         |
| Logging           | Serilog → Seq or ELK Stack             |
| Health Checks     | ASP.NET Health Checks middleware       |
| Alerting          | PagerDuty / Grafana Alerts             |
| Backup            | Azure automated backups (daily + PITR) |

### CI/CD Pipeline Stages

```
Code Push → Build → Lint → Unit Tests → Integration Tests → Security Scan (OWASP ZAP)
    → Build Docker Images → Push to Container Registry
    → Deploy to Staging → Smoke Tests → Load Tests (k6)
    → Manual Approval → Deploy to Production
```

---

## 20. Dependencies & Third-Party Packages

| Package                                            | Purpose                              |
| -------------------------------------------------- | ------------------------------------ |
| `Microsoft.EntityFrameworkCore.SqlServer`           | SQL Server ORM provider              |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core                   |
| `MediatR`                                          | CQRS mediator pattern                |
| `FluentValidation`                                 | Input validation                     |
| `FluentValidation.DependencyInjectionExtensions`   | DI integration                       |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | Object mapping                    |
| `Microsoft.AspNetCore.SignalR`                     | Real-time communication              |
| `MudBlazor`                                        | Blazor UI component library          |
| `Serilog.AspNetCore`                               | Structured logging                   |
| `Serilog.Sinks.Seq`                               | Seq log sink                         |
| `QuestPDF` or `IronPDF`                            | PDF generation for documents         |
| `ClosedXML`                                        | Excel export/import                  |
| `CsvHelper`                                        | CSV import/export                    |
| `SendGrid` or `MailKit`                            | Email delivery                       |
| `AspNetCoreRateLimit`                              | API rate limiting middleware         |
| `Hangfire` or `Quartz.NET`                         | Background job scheduling            |
| `NEST` (Elasticsearch client)                      | Full-text search                     |
| `Microsoft.AspNetCore.Authentication.JwtBearer`    | JWT token authentication (if API)    |
| `AspNetCore.HealthChecks.SqlServer`                | SQL Server health check              |
| `AspNetCore.HealthChecks.Redis`                    | Redis health check                   |
| `xUnit`                                            | Unit testing framework               |
| `Moq`                                              | Mocking framework                    |
| `FluentAssertions`                                 | Test assertions                      |
| `bUnit`                                            | Blazor component testing             |
| `Microsoft.AspNetCore.Mvc.Testing`                 | Integration test host                |
| `Testcontainers`                                   | Containerized test dependencies      |
| `k6`                                               | Load testing                         |

---

## 21. Glossary

| Term    | Definition                                                                 |
| ------- | -------------------------------------------------------------------------- |
| SKU     | Stock Keeping Unit — unique identifier for a product variant               |
| GRN     | Goods Receiving Note — document confirming receipt of purchased goods       |
| RMA     | Return Merchandise Authorization — process for handling product returns     |
| PO      | Purchase Order — formal request to a supplier to deliver goods             |
| SO      | Sales Order — document recording a customer's purchase request             |
| AR      | Accounts Receivable — money owed to the company by customers               |
| AP      | Accounts Payable — money the company owes to suppliers                     |
| GL      | General Ledger — complete record of all financial transactions             |
| COGS    | Cost of Goods Sold — direct costs attributable to goods sold               |
| FIFO    | First In, First Out — inventory valuation method                           |
| FEFO    | First Expired, First Out — expiry-based picking strategy                   |
| RBAC    | Role-Based Access Control — permission model based on user roles           |
| CQRS    | Command Query Responsibility Segregation — separate read/write models      |
| DI      | Dependency Injection — design pattern for managing object dependencies     |
| RFQ     | Request for Quotation — formal request to suppliers for pricing            |
| MOQ     | Minimum Order Quantity — smallest quantity that can be ordered              |
| UoM     | Unit of Measure — standard unit for measuring product quantities           |
| EAV     | Entity-Attribute-Value — flexible data model for custom fields             |
| HMAC    | Hash-based Message Authentication Code — cryptographic signing method      |
| GDPR    | General Data Protection Regulation — EU data privacy regulation            |
| CCPA    | California Consumer Privacy Act — California data privacy law              |
| RPO     | Recovery Point Objective — maximum acceptable data loss measured in time    |
| RTO     | Recovery Time Objective — maximum acceptable downtime after failure         |
| ABC     | Analysis method classifying inventory by revenue contribution (A=high, C=low) |
| OFX     | Open Financial Exchange — standard format for bank statement data          |
| SLA     | Service Level Agreement — agreed performance and response time targets     |
| PITR    | Point-In-Time Recovery — database restore to a specific moment             |
| DN      | Delivery Note — document accompanying shipped goods                        |
| VAT     | Value Added Tax — consumption tax on goods and services                    |
| GST     | Goods and Services Tax — broad-based consumption tax                       |
| FTS     | Full-Text Search — search technique indexing word content in documents      |

---

*End of Document*
