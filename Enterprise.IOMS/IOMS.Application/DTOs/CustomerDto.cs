using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Application.DTOs
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string ContactPersonName { get; set; } = string.Empty;
        public string? ContactPersonEmail { get; set; }
        public string? ContactPersonPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Zila { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public decimal CreditLimit { get; set; }
        public string? PaymentTerms { get; set; }
        public bool IsActive { get; set; }
        public bool IsTaxExempt { get; set; }
        public Guid TaxJurisdictionId { get; set; }
        public Guid DefaultPriceListId { get; set; }
        public Guid DefaultCurrencyId { get; set; }

        
    }
}
