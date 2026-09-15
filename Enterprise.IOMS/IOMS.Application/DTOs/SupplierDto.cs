using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Application.DTOs
{
    public class SupplierDto
    {
        public Guid Id { get; set; }
        public string SupplierName { get; set; }
        public string? SupplierEmail { get; set; }
        public string? SupplierPhone { get; set; }
        public string ContactPersonName { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? ContactPersonPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Zila { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public Guid? DefaultCurrencyId { get; set; }
        public string? PaymentTerms { get; set; }
        public int LeadTimeDays { get; set; } = 1;
        public decimal Rating { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
