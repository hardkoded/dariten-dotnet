using System.Globalization;
using System.Text.RegularExpressions;

namespace Dariten.Core;

public static partial class Money
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static int DollarsToCents(string input)
    {
        var normalized = StripPattern().Replace(input.Trim(), "");
        if (!DollarPattern().IsMatch(normalized))
        {
            throw new FormatException("Enter a valid dollar amount, like 42.50");
        }

        var negative = normalized.StartsWith('-');
        var unsigned = negative ? normalized[1..] : normalized;
        var parts = unsigned.Split('.');
        var whole = parts[0];
        var fraction = parts.Length > 1 ? parts[1] : "";
        var fractionCents = fraction.Length == 0 ? 0 : int.Parse(fraction.PadRight(2, '0'), CultureInfo.InvariantCulture);
        var cents = int.Parse(whole, CultureInfo.InvariantCulture) * 100 + fractionCents;
        return negative ? -cents : cents;
    }

    public static string CentsToDollarInput(int cents)
    {
        var negative = cents < 0;
        var abs = Math.Abs(cents);
        var value = (abs / 100).ToString(CultureInfo.InvariantCulture) + "." + (abs % 100).ToString("D2", CultureInfo.InvariantCulture);
        return negative ? "-" + value : value;
    }

    public static string FormatCents(int cents)
    {
        var absolute = Math.Abs(cents) / 100m;
        var formatted = absolute.ToString("C2", EnUs);
        return cents < 0 ? "−" + formatted : formatted;
    }

    public static int SumCents(IEnumerable<int> values) => values.Sum();

    public static int AccountBalanceCents(int openingBalanceCents, IEnumerable<int> transactionAmounts) =>
        openingBalanceCents + SumCents(transactionAmounts);

    public static int NetWorthCents(IEnumerable<int> balances) => SumCents(balances);

    [GeneratedRegex(@"[$,\s]", RegexOptions.CultureInvariant)]
    private static partial Regex StripPattern();

    [GeneratedRegex(@"^-?\d+(\.\d{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex DollarPattern();
}
