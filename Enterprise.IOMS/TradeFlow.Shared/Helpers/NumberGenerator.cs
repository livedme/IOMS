namespace TradeFlow.Shared.Helpers;

public static class NumberGenerator
{
    private static readonly Random _random = new();

    public static string GenerateOrderNumber(string prefix = "SO")
        => $"{prefix}-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateInvoiceNumber()
        => $"INV-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateQuoteNumber()
        => $"QT-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateRfqNumber()
        => $"RFQ-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateReturnNumber(string prefix = "RET")
        => $"{prefix}-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateDeliveryNoteNumber()
        => $"DN-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateCreditNoteNumber()
        => $"CN-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateDebitNoteNumber()
        => $"DBN-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";

    public static string GenerateJournalReference()
        => $"JE-{DateTime.UtcNow:yyMMdd}-{_random.Next(10, 999999)}";
}
