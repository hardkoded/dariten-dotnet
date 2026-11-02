namespace Dariten.Core;

public static class AccountTypes
{
    public const string Checking = "CHECKING";
    public const string Savings = "SAVINGS";
    public const string CreditCard = "CREDIT_CARD";
    public const string Cash = "CASH";

    public static readonly IReadOnlyList<string> All = [Checking, Savings, CreditCard, Cash];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Checking] = "Checking",
        [Savings] = "Savings",
        [CreditCard] = "Credit card",
        [Cash] = "Cash",
    };

    public static bool IsKnown(string type) => Labels.ContainsKey(type);

    public static bool IsLiability(string type) => type == CreditCard;

    public static string Label(string type) => Labels.TryGetValue(type, out var label) ? label : type;
}

public static class Finance
{
    public static IReadOnlyList<SpendingSlice> SpendingByCategory(
        IEnumerable<(int AmountCents, string CategoryName, bool IsIncome)> transactions)
    {
        var totals = new Dictionary<string, int>();
        foreach (var transaction in transactions)
        {
            if (transaction.IsIncome || transaction.AmountCents >= 0)
            {
                continue;
            }

            totals.TryGetValue(transaction.CategoryName, out var current);
            totals[transaction.CategoryName] = current + Math.Abs(transaction.AmountCents);
        }

        return totals
            .Select(pair => new SpendingSlice(pair.Key, pair.Value))
            .OrderByDescending(slice => slice.AmountCents)
            .ToList();
    }

    /// <summary>Share of this month's income that was not spent: (income − spending) / income.</summary>
    public static int SavingsRatePercent(int incomeCents, int spendingCents)
    {
        if (incomeCents <= 0)
        {
            return 0;
        }

        return JsRound((incomeCents - spendingCents) / (double)incomeCents * 100);
    }

    public static BudgetProgress BudgetProgress(int budgetCents, int actualCents)
    {
        var remaining = budgetCents - actualCents;
        var percent = budgetCents == 0
            ? actualCents > 0 ? 100 : 0
            : Math.Min(100, JsRound(actualCents / (double)budgetCents * 100));
        return new BudgetProgress(remaining, percent, remaining < 0);
    }

    public static bool MatchesTransactionFilters(
        string accountId,
        string categoryId,
        string date,
        TransactionFilters filters)
    {
        if (!string.IsNullOrEmpty(filters.AccountId) && accountId != filters.AccountId)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filters.CategoryId) && categoryId != filters.CategoryId)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filters.From) && string.CompareOrdinal(date, filters.From) < 0)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(filters.To) && string.CompareOrdinal(date, filters.To) > 0)
        {
            return false;
        }

        return true;
    }

    public static string ToCsvRow(IReadOnlyList<string> values) =>
        string.Join(",", values.Select(value =>
        {
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            }

            return value;
        }));

    public static string TransactionsToCsv(IReadOnlyList<CsvTransaction> rows)
    {
        var header = ToCsvRow(["Date", "Payee", "Category", "Account", "Amount", "Memo"]);
        var body = rows.Select(row => ToCsvRow([
            row.Date,
            row.Payee,
            row.Category,
            row.Account,
            (row.AmountCents / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            row.Memo,
        ]));
        return string.Join("\n", new[] { header }.Concat(body));
    }

    /// <summary>ECMAScript <c>Math.round</c>: half rounds toward +∞.</summary>
    private static int JsRound(double value) => (int)Math.Floor(value + 0.5);
}

public readonly record struct SpendingSlice(string Name, int AmountCents);

public readonly record struct BudgetProgress(int RemainingCents, int Percent, bool Over);

public sealed record TransactionFilters
{
    public string? AccountId { get; init; }
    public string? CategoryId { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
}

public readonly record struct CsvTransaction(
    string Date,
    string Payee,
    string Category,
    string Account,
    int AmountCents,
    string Memo);
