namespace IOMS.Shared.Extensions;

public static class StringExtensions
{
    public static bool HasValue(this string? value)
        => !string.IsNullOrWhiteSpace(value);

    public static string ToTitleCase(this string value)
        => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
}

public static class DecimalExtensions
{
    public static string ToCurrency(this decimal value, string symbol = "$")
        => $"{symbol}{value:N2}";

    public static decimal RoundToPlaces(this decimal value, int places = 2)
        => Math.Round(value, places, MidpointRounding.AwayFromZero);
}

public static class DateTimeExtensions
{
    public static string ToShortDisplay(this DateTime date)
        => date.ToString("MMM dd, yyyy");

    public static string ToLongDisplay(this DateTime date)
        => date.ToString("MMMM dd, yyyy hh:mm tt");

    public static int DaysOverdue(this DateTime dueDate)
        => Math.Max(0, (DateTime.UtcNow.Date - dueDate.Date).Days);
}

public static class EnumExtensions
{
    public static string ToDisplayString(this Enum value)
    {
        var name = value.ToString();
        return System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])([A-Z])", " $1");
    }
}
