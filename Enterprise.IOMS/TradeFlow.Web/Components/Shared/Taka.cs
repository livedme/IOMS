using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace TradeFlow.Web.Utility;

/// <summary>
/// Renders money with a styled, larger ৳ symbol: "<span class="cash-sign">৳</span>1,250.00".
/// H/Ho return MarkupString for direct use in Razor markup; Text/Text0 return plain strings
/// for strings built in code (toasts, labels).
/// </summary>
public static class Taka
{
    public const string Sign = "৳";

    private const string Mark = "<span class=\"cash-sign\">" + Sign + "</span>";

    public static MarkupString H(decimal value, string format = "N2")
        => (MarkupString)(Mark + value.ToString(format));

    public static MarkupString Ho(decimal value)
        => H(value, "N0");

    public static MarkupString H(int value)
        => (MarkupString)(Mark + value.ToString("N0"));

    public static MarkupString H(long value)
        => (MarkupString)(Mark + value.ToString("N0"));

    public static string Text(decimal value, string format = "N2")
        => Sign + value.ToString(format);

    public static string Text0(decimal value)
        => Sign + value.ToString("N0");
}
