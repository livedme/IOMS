namespace IOMS.Shared.Helpers;

public static class NumberGenerator
{
    private static readonly Random _random = new();

    public static string GenerateOrderNumber(string prefix = "SO")
        => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateInvoiceNumber()
        => $"INV-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateQuoteNumber()
        => $"QT-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateRfqNumber()
        => $"RFQ-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateReturnNumber(string prefix = "RET")
        => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateDeliveryNoteNumber()
        => $"DN-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateCreditNoteNumber()
        => $"CN-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateDebitNoteNumber()
        => $"DBN-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";

    public static string GenerateJournalReference()
        => $"JE-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";
}
