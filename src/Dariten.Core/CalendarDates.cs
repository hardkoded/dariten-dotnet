using System.Globalization;
using System.Text.RegularExpressions;

namespace Dariten.Core;

public static partial class CalendarDates
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static string ToDateOnly(string value) => value.Length >= 10 ? value[..10] : value;

    public static DateOnly ParseDateOnly(string value)
    {
        if (!DatePattern().IsMatch(value))
        {
            throw new FormatException("Enter a date as YYYY-MM-DD");
        }

        var year = int.Parse(value[..4], CultureInfo.InvariantCulture);
        var month = int.Parse(value.Substring(5, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(value.Substring(8, 2), CultureInfo.InvariantCulture);
        try
        {
            return new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new FormatException("Enter a date as YYYY-MM-DD", ex);
        }
    }

    public static string FormatDateOnly(string value) =>
        ParseDateOnly(ToDateOnly(value)).ToString("MMM d, yyyy", EnUs);

    public static string CurrentMonth(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return $"{now.Year:D4}-{now.Month:D2}";
    }

    public static (string Start, string End) MonthRange(string month)
    {
        if (!MonthPattern().IsMatch(month))
        {
            throw new FormatException("Month must be YYYY-MM");
        }

        var year = int.Parse(month[..4], CultureInfo.InvariantCulture);
        var monthNumber = int.Parse(month[5..], CultureInfo.InvariantCulture);
        var start = new DateOnly(year, monthNumber, 1);
        var end = start.AddMonths(1);
        return (start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public static string FormatMonthLabel(string month)
    {
        var (start, _) = MonthRange(month);
        return ParseDateOnly(start).ToString("MMMM yyyy", EnUs);
    }

    public static string ShiftMonth(string month, int offset)
    {
        var year = int.Parse(month[..4], CultureInfo.InvariantCulture);
        var monthNumber = int.Parse(month[5..], CultureInfo.InvariantCulture);
        var shifted = new DateOnly(year, monthNumber, 1).AddMonths(offset);
        return $"{shifted.Year:D4}-{shifted.Month:D2}";
    }

    public static bool IsMonth(string? value) => value is not null && MonthPattern().IsMatch(value);

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^\d{4}-\d{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex MonthPattern();
}
