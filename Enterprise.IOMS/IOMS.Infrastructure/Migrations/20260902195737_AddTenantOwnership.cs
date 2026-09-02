using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Key", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "default", "Default Tenant" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_TenantId",
                table: "WebhookSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_TenantId",
                table: "WebhookDeliveryLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UoMConversions_TenantId",
                table: "UoMConversions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_TenantId",
                table: "UnitsOfMeasure",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_TenantId",
                table: "TaxRates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxJurisdictions_TenantId",
                table: "TaxJurisdictions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxExemptions_TenantId",
                table: "TaxExemptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakes_TenantId",
                table: "Stocktakes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_TenantId",
                table: "StocktakeItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId",
                table: "Shipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_TenantId",
                table: "ScheduledReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteItems_TenantId",
                table: "SalesQuoteItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_TenantId",
                table: "SalesOrderItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqSupplierResponses_TenantId",
                table: "RfqSupplierResponses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqRequests_TenantId",
                table: "RfqRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqItems_TenantId",
                table: "RfqItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId",
                table: "PurchaseReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_TenantId",
                table: "PurchaseReturnItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TenantId",
                table: "PurchaseOrderItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId",
                table: "PriceLists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId",
                table: "PriceListItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TenantId",
                table: "NotificationTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId",
                table: "NotificationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostComponents_TenantId",
                table: "LandedCostComponents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostAllocations_TenantId",
                table: "LandedCostAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Kits_TenantId",
                table: "Kits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KitComponents_TenantId",
                table: "KitComponents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId",
                table: "JournalEntryLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_TenantId",
                table: "ExchangeRates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_TenantId",
                table: "DocumentTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_TenantId",
                table: "Discounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_TenantId",
                table: "DeliveryNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_TenantId",
                table: "DebitNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSubjectRequests_TenantId",
                table: "DataSubjectRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_TenantId",
                table: "CustomFieldValues",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_TenantId",
                table: "CustomFieldDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId",
                table: "CreditNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId",
                table: "Branches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_TenantId",
                table: "BankStatements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_TenantId",
                table: "BankStatementLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivalPolicies_TenantId",
                table: "ArchivalPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowRules_TenantId",
                table: "ApprovalWorkflowRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequestSteps_TenantId",
                table: "ApprovalRequestSteps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_TenantId",
                table: "ApprovalRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Key",
                table: "Tenants",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Tenants_TenantId",
                table: "Accounts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Tenants_TenantId",
                table: "ApprovalRequests",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequestSteps_Tenants_TenantId",
                table: "ApprovalRequestSteps",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalWorkflowRules_Tenants_TenantId",
                table: "ApprovalWorkflowRules",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ArchivalPolicies_Tenants_TenantId",
                table: "ArchivalPolicies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatementLines_Tenants_TenantId",
                table: "BankStatementLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BankStatements_Tenants_TenantId",
                table: "BankStatements",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Tenants_TenantId",
                table: "Branches",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Brands_Tenants_TenantId",
                table: "Brands",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_Tenants_TenantId",
                table: "CreditNotes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Currencies_Tenants_TenantId",
                table: "Currencies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldDefinitions_Tenants_TenantId",
                table: "CustomFieldDefinitions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldValues_Tenants_TenantId",
                table: "CustomFieldValues",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataSubjectRequests_Tenants_TenantId",
                table: "DataSubjectRequests",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DebitNotes_Tenants_TenantId",
                table: "DebitNotes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_Tenants_TenantId",
                table: "DeliveryNotes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Tenants_TenantId",
                table: "Discounts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTemplates_Tenants_TenantId",
                table: "DocumentTemplates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRates_Tenants_TenantId",
                table: "ExchangeRates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Tenants_TenantId",
                table: "Inventories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Tenants_TenantId",
                table: "Invoices",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Tenants_TenantId",
                table: "JournalEntries",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_Tenants_TenantId",
                table: "JournalEntryLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitComponents_Tenants_TenantId",
                table: "KitComponents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kits_Tenants_TenantId",
                table: "Kits",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LandedCostAllocations_Tenants_TenantId",
                table: "LandedCostAllocations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LandedCostComponents_Tenants_TenantId",
                table: "LandedCostComponents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_Tenants_TenantId",
                table: "NotificationLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationTemplates_Tenants_TenantId",
                table: "NotificationTemplates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Tenants_TenantId",
                table: "Payments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListItems_Tenants_TenantId",
                table: "PriceListItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Tenants_TenantId",
                table: "PriceLists",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSerials_Tenants_TenantId",
                table: "ProductSerials",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Tenants_TenantId",
                table: "PurchaseOrderItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturnItems_Tenants_TenantId",
                table: "PurchaseReturnItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturns_Tenants_TenantId",
                table: "PurchaseReturns",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RfqItems_Tenants_TenantId",
                table: "RfqItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RfqRequests_Tenants_TenantId",
                table: "RfqRequests",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RfqSupplierResponses_Tenants_TenantId",
                table: "RfqSupplierResponses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderItems_Tenants_TenantId",
                table: "SalesOrderItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuoteItems_Tenants_TenantId",
                table: "SalesQuoteItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotes_Tenants_TenantId",
                table: "SalesQuotes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledReports_Tenants_TenantId",
                table: "ScheduledReports",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Tenants_TenantId",
                table: "StockMovements",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StocktakeItems_Tenants_TenantId",
                table: "StocktakeItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stocktakes_Tenants_TenantId",
                table: "Stocktakes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Tenants_TenantId",
                table: "Suppliers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxExemptions_Tenants_TenantId",
                table: "TaxExemptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxJurisdictions_Tenants_TenantId",
                table: "TaxJurisdictions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxRates_Tenants_TenantId",
                table: "TaxRates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitsOfMeasure_Tenants_TenantId",
                table: "UnitsOfMeasure",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UoMConversions_Tenants_TenantId",
                table: "UoMConversions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookDeliveryLogs_Tenants_TenantId",
                table: "WebhookDeliveryLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookSubscriptions_Tenants_TenantId",
                table: "WebhookSubscriptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Tenants_TenantId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Tenants_TenantId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequestSteps_Tenants_TenantId",
                table: "ApprovalRequestSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalWorkflowRules_Tenants_TenantId",
                table: "ApprovalWorkflowRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ArchivalPolicies_Tenants_TenantId",
                table: "ArchivalPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_BankStatementLines_Tenants_TenantId",
                table: "BankStatementLines");

            migrationBuilder.DropForeignKey(
                name: "FK_BankStatements_Tenants_TenantId",
                table: "BankStatements");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Tenants_TenantId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Brands_Tenants_TenantId",
                table: "Brands");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditNotes_Tenants_TenantId",
                table: "CreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Currencies_Tenants_TenantId",
                table: "Currencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldDefinitions_Tenants_TenantId",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldValues_Tenants_TenantId",
                table: "CustomFieldValues");

            migrationBuilder.DropForeignKey(
                name: "FK_DataSubjectRequests_Tenants_TenantId",
                table: "DataSubjectRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitNotes_Tenants_TenantId",
                table: "DebitNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_Tenants_TenantId",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Tenants_TenantId",
                table: "Discounts");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTemplates_Tenants_TenantId",
                table: "DocumentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRates_Tenants_TenantId",
                table: "ExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Tenants_TenantId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Tenants_TenantId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Tenants_TenantId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_Tenants_TenantId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_KitComponents_Tenants_TenantId",
                table: "KitComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Kits_Tenants_TenantId",
                table: "Kits");

            migrationBuilder.DropForeignKey(
                name: "FK_LandedCostAllocations_Tenants_TenantId",
                table: "LandedCostAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_LandedCostComponents_Tenants_TenantId",
                table: "LandedCostComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationLogs_Tenants_TenantId",
                table: "NotificationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationTemplates_Tenants_TenantId",
                table: "NotificationTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Tenants_TenantId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceListItems_Tenants_TenantId",
                table: "PriceListItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Tenants_TenantId",
                table: "PriceLists");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSerials_Tenants_TenantId",
                table: "ProductSerials");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Tenants_TenantId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturnItems_Tenants_TenantId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturns_Tenants_TenantId",
                table: "PurchaseReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_RfqItems_Tenants_TenantId",
                table: "RfqItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RfqRequests_Tenants_TenantId",
                table: "RfqRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RfqSupplierResponses_Tenants_TenantId",
                table: "RfqSupplierResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderItems_Tenants_TenantId",
                table: "SalesOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuoteItems_Tenants_TenantId",
                table: "SalesQuoteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotes_Tenants_TenantId",
                table: "SalesQuotes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledReports_Tenants_TenantId",
                table: "ScheduledReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Tenants_TenantId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StocktakeItems_Tenants_TenantId",
                table: "StocktakeItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Stocktakes_Tenants_TenantId",
                table: "Stocktakes");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Tenants_TenantId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxExemptions_Tenants_TenantId",
                table: "TaxExemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxJurisdictions_Tenants_TenantId",
                table: "TaxJurisdictions");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxRates_Tenants_TenantId",
                table: "TaxRates");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitsOfMeasure_Tenants_TenantId",
                table: "UnitsOfMeasure");

            migrationBuilder.DropForeignKey(
                name: "FK_UoMConversions_Tenants_TenantId",
                table: "UoMConversions");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_WebhookDeliveryLogs_Tenants_TenantId",
                table: "WebhookDeliveryLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WebhookSubscriptions_Tenants_TenantId",
                table: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_WebhookSubscriptions_TenantId",
                table: "WebhookSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_WebhookDeliveryLogs_TenantId",
                table: "WebhookDeliveryLogs");

            migrationBuilder.DropIndex(
                name: "IX_UoMConversions_TenantId",
                table: "UoMConversions");

            migrationBuilder.DropIndex(
                name: "IX_UnitsOfMeasure_TenantId",
                table: "UnitsOfMeasure");

            migrationBuilder.DropIndex(
                name: "IX_TaxRates_TenantId",
                table: "TaxRates");

            migrationBuilder.DropIndex(
                name: "IX_TaxJurisdictions_TenantId",
                table: "TaxJurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_TaxExemptions_TenantId",
                table: "TaxExemptions");

            migrationBuilder.DropIndex(
                name: "IX_Stocktakes_TenantId",
                table: "Stocktakes");

            migrationBuilder.DropIndex(
                name: "IX_StocktakeItems_TenantId",
                table: "StocktakeItems");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_TenantId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledReports_TenantId",
                table: "ScheduledReports");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuoteItems_TenantId",
                table: "SalesQuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderItems_TenantId",
                table: "SalesOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_RfqSupplierResponses_TenantId",
                table: "RfqSupplierResponses");

            migrationBuilder.DropIndex(
                name: "IX_RfqRequests_TenantId",
                table: "RfqRequests");

            migrationBuilder.DropIndex(
                name: "IX_RfqItems_TenantId",
                table: "RfqItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_TenantId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturnItems_TenantId",
                table: "PurchaseReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_TenantId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_TenantId",
                table: "PriceLists");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItems_TenantId",
                table: "PriceListItems");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_NotificationTemplates_TenantId",
                table: "NotificationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_TenantId",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_LandedCostComponents_TenantId",
                table: "LandedCostComponents");

            migrationBuilder.DropIndex(
                name: "IX_LandedCostAllocations_TenantId",
                table: "LandedCostAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Kits_TenantId",
                table: "Kits");

            migrationBuilder.DropIndex(
                name: "IX_KitComponents_TenantId",
                table: "KitComponents");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_TenantId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_TenantId",
                table: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTemplates_TenantId",
                table: "DocumentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_TenantId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryNotes_TenantId",
                table: "DeliveryNotes");

            migrationBuilder.DropIndex(
                name: "IX_DebitNotes_TenantId",
                table: "DebitNotes");

            migrationBuilder.DropIndex(
                name: "IX_DataSubjectRequests_TenantId",
                table: "DataSubjectRequests");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldValues_TenantId",
                table: "CustomFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldDefinitions_TenantId",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_TenantId",
                table: "CreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_Branches_TenantId",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_BankStatements_TenantId",
                table: "BankStatements");

            migrationBuilder.DropIndex(
                name: "IX_BankStatementLines_TenantId",
                table: "BankStatementLines");

            migrationBuilder.DropIndex(
                name: "IX_ArchivalPolicies_TenantId",
                table: "ArchivalPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalWorkflowRules_TenantId",
                table: "ApprovalWorkflowRules");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequestSteps_TenantId",
                table: "ApprovalRequestSteps");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_TenantId",
                table: "ApprovalRequests");
        }
    }
}
