namespace TradeFlow.Web.Components.Shared.Dashboard;

/// <summary>
/// One selectable dashboard period. <see cref="From"/> and <see cref="To"/> are inclusive calendar
/// days, and <see cref="Months"/> is non-zero only for the month-based presets, where it drives the
/// span wording instead of the day count.
/// </summary>
public sealed record DashboardRange(string Label, DateTime From, DateTime To, int Months = 0)
{
    /// <summary>
    /// Inclusive day count. The dashboard service reads a trailing "last N days" window and nothing
    /// else, so this is the single number that makes that window land exactly on From/To. That is why
    /// a month preset is measured in real days rather than rounded to 30/90/365 — "This Month" on the
    /// 3rd is three days of data, not a month, and "Last 3 Months" is never a fixed 90.
    /// </summary>
    public int Days => (To.Date - From.Date).Days + 1;

    /// <summary>Span wording shown under the card title, e.g. "7 days to 1 Oct 2026".</summary>
    public string Caption
    {
        get
        {
            if (Months > 0)
            {
                // "This Month" resolves to a single month, where repeating the same name on both ends
                // of a range would read as nonsense.
                if (From.Year == To.Year && From.Month == To.Month) return To.ToString("MMMM yyyy");
                return $"{From:MMM yyyy} – {To:MMM yyyy}";
            }

            return Days == 1
                ? $"Today · {To:d MMM yyyy}"
                : $"{Days} days to {To:d MMM yyyy}";
        }
    }
}

/// <summary>
/// The dashboard's period presets, in the order they appear in every range selector. The header
/// strip and both overview cards read this one list, so a preset can never be offered in one place
/// and missing from another.
/// </summary>
public static class DashboardRanges
{
    public const string Today = "Today";
    public const string Last7Days = "Last 7 Days";
    public const string Last30Days = "Last 30 Days";
    public const string ThisMonth = "This Month";
    public const string Last3Months = "Last 3 Months";
    public const string Last6Months = "Last 6 Months";
    public const string Last12Months = "Last 12 Months";

    /// <summary>Preselected range of every dashboard range selector.</summary>
    public const string DefaultLabel = Last7Days;

    public static readonly IReadOnlyList<string> Labels = new[]
    {
        Today, Last7Days, Last30Days, ThisMonth, Last3Months, Last6Months, Last12Months
    };

    /// <summary>
    /// Turns a selector label into a concrete window relative to <paramref name="today"/>.
    /// An unrecognised label resolves to the default period, which is the period the selector is
    /// actually showing, instead of falling through to some other preset's day count.
    /// </summary>
    public static DashboardRange Resolve(string? label, DateTime today) => label switch
    {
        Today => new DashboardRange(Today, today.Date, today.Date),
        Last7Days => TrailingDays(today, Last7Days, days: 7),
        Last30Days => TrailingDays(today, Last30Days, days: 30),
        ThisMonth => new DashboardRange(ThisMonth, new DateTime(today.Year, today.Month, 1), today.Date, Months: 1),
        Last3Months => TrailingMonths(today, Last3Months, months: 3),
        Last6Months => TrailingMonths(today, Last6Months, months: 6),
        Last12Months => TrailingMonths(today, Last12Months, months: 12),
        _ => TrailingDays(today, DefaultLabel, days: 7)
    };

    /// <summary>
    /// A window of <paramref name="days"/> days ending today, inclusive. Yesterday therefore counts
    /// as day one of "Last 7 Days", not day zero.
    /// </summary>
    private static DashboardRange TrailingDays(DateTime today, string label, int days) =>
        new(label, today.Date.AddDays(-(days - 1)), today.Date);

    /// <summary>
    /// Starts on the first of the month N months back so the window contains whole months, and ends
    /// today, which may be mid-month.
    /// </summary>
    private static DashboardRange TrailingMonths(DateTime today, string label, int months)
    {
        var first = new DateTime(today.Year, today.Month, 1).AddMonths(-months);
        return new DashboardRange(label, first, today.Date, months);
    }
}