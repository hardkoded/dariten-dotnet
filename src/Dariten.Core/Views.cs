namespace Dariten.Core;

public sealed record AccountBalanceView(
    string Id,
    string Name,
    string Type,
    string? Institution,
    int OpeningBalanceCents,
    int TransactionCount,
    int BalanceCents);

public sealed record CategoryView(
    string Id,
    string Name,
    string Group,
    bool IsIncome,
    int SortOrder,
    int TransactionCount);

public sealed record TransactionView(
    string Id,
    string Date,
    int AmountCents,
    string Payee,
    string? Memo,
    string AccountId,
    string CategoryId,
    string AccountName,
    string CategoryName,
    bool IsIncome);

public sealed record RecentTransaction(string Id, string Date, int AmountCents, string Payee, string AccountName, string CategoryName);

public sealed record DashboardData(
    string Month,
    IReadOnlyList<AccountBalanceView> Accounts,
    int NetWorthCents,
    int IncomeCents,
    int SpendingCents,
    int SavingsRatePercent,
    IReadOnlyList<SpendingSlice> Spending,
    IReadOnlyList<RecentTransaction> Recent);

public sealed record BudgetRowView(
    string CategoryId,
    string Name,
    string Group,
    int BudgetCents,
    int ActualCents,
    int RemainingCents);

public sealed record BudgetView(string Month, IReadOnlyList<BudgetRowView> Rows, int BudgetedCents, int ActualCents);

public static class LedgerViews
{
    private static readonly Dictionary<string, int> TypeRank = new()
    {
        [AccountTypes.Checking] = 0,
        [AccountTypes.Savings] = 1,
        [AccountTypes.CreditCard] = 2,
        [AccountTypes.Cash] = 3,
    };

    public static IReadOnlyList<AccountBalanceView> AccountsWithBalances(LedgerSnapshot snapshot)
    {
        var amountsByAccount = snapshot.Transactions
            .GroupBy(transaction => transaction.AccountId)
            .ToDictionary(group => group.Key, group => group.Select(transaction => transaction.AmountCents).ToList());

        return snapshot.Accounts
            .OrderBy(account => TypeRank.GetValueOrDefault(account.Type, 99))
            .ThenBy(account => account.Name, StringComparer.Ordinal)
            .Select(account =>
            {
                var amounts = amountsByAccount.GetValueOrDefault(account.Id) ?? [];
                return new AccountBalanceView(
                    account.Id,
                    account.Name,
                    account.Type,
                    account.Institution,
                    account.OpeningBalanceCents,
                    amounts.Count,
                    Money.AccountBalanceCents(account.OpeningBalanceCents, amounts));
            })
            .ToList();
    }

    public static IReadOnlyList<CategoryView> Categories(LedgerSnapshot snapshot)
    {
        var counts = snapshot.Transactions
            .GroupBy(transaction => transaction.CategoryId)
            .ToDictionary(group => group.Key, group => group.Count());

        return snapshot.Categories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name, StringComparer.Ordinal)
            .Select(category => new CategoryView(
                category.Id,
                category.Name,
                category.Group,
                category.IsIncome,
                category.SortOrder,
                counts.GetValueOrDefault(category.Id)))
            .ToList();
    }

    public static IReadOnlyList<TransactionView> Transactions(LedgerSnapshot snapshot, TransactionFilters? filters = null)
    {
        filters ??= new TransactionFilters();
        var accounts = snapshot.Accounts.ToDictionary(account => account.Id);
        var categories = snapshot.Categories.ToDictionary(category => category.Id);

        return snapshot.Transactions
            .Where(transaction => Finance.MatchesTransactionFilters(transaction.AccountId, transaction.CategoryId, transaction.Date, filters))
            .OrderByDescending(transaction => transaction.Date, StringComparer.Ordinal)
            .ThenByDescending(transaction => transaction.CreatedAt, StringComparer.Ordinal)
            .Select(transaction =>
            {
                if (!accounts.TryGetValue(transaction.AccountId, out var account) ||
                    !categories.TryGetValue(transaction.CategoryId, out var category))
                {
                    return null;
                }

                return new TransactionView(
                    transaction.Id,
                    transaction.Date,
                    transaction.AmountCents,
                    transaction.Payee,
                    transaction.Memo,
                    transaction.AccountId,
                    transaction.CategoryId,
                    account.Name,
                    category.Name,
                    category.IsIncome);
            })
            .Where(row => row is not null)
            .Cast<TransactionView>()
            .ToList();
    }

    public static DashboardData Dashboard(LedgerSnapshot snapshot, string? month = null)
    {
        month ??= CalendarDates.CurrentMonth();
        var (start, end) = CalendarDates.MonthRange(month);
        var accounts = AccountsWithBalances(snapshot);
        var categories = snapshot.Categories.ToDictionary(category => category.Id);
        var monthTransactions = snapshot.Transactions
            .Where(transaction => string.CompareOrdinal(transaction.Date, start) >= 0 && string.CompareOrdinal(transaction.Date, end) < 0)
            .ToList();

        var income = monthTransactions.Where(transaction => transaction.AmountCents > 0).Sum(transaction => transaction.AmountCents);
        var spending = monthTransactions.Where(transaction => transaction.AmountCents < 0).Sum(transaction => Math.Abs(transaction.AmountCents));
        var slices = Finance.SpendingByCategory(monthTransactions.SelectMany(transaction =>
        {
            if (!categories.TryGetValue(transaction.CategoryId, out var category))
            {
                return Array.Empty<(int AmountCents, string CategoryName, bool IsIncome)>();
            }

            return [(transaction.AmountCents, category.Name, category.IsIncome)];
        }));

        var recent = Transactions(snapshot).Take(8).Select(transaction =>
            new RecentTransaction(transaction.Id, transaction.Date, transaction.AmountCents, transaction.Payee, transaction.AccountName, transaction.CategoryName)).ToList();

        return new DashboardData(
            month,
            accounts,
            Money.NetWorthCents(accounts.Select(account => account.BalanceCents)),
            income,
            spending,
            Finance.SavingsRatePercent(income, spending),
            slices,
            recent);
    }

    public static BudgetView Budgets(LedgerSnapshot snapshot, string month)
    {
        var (start, end) = CalendarDates.MonthRange(month);
        var budgetByCategory = snapshot.Budgets
            .Where(budget => budget.Month == month)
            .GroupBy(budget => budget.CategoryId)
            .ToDictionary(group => group.Key, group => group.Last().AmountCents);
        var actualByCategory = new Dictionary<string, int>();
        foreach (var transaction in snapshot.Transactions)
        {
            if (string.CompareOrdinal(transaction.Date, start) < 0 ||
                string.CompareOrdinal(transaction.Date, end) >= 0 ||
                transaction.AmountCents >= 0)
            {
                continue;
            }

            actualByCategory.TryGetValue(transaction.CategoryId, out var current);
            actualByCategory[transaction.CategoryId] = current + Math.Abs(transaction.AmountCents);
        }

        var rows = snapshot.Categories
            .Where(category => !category.IsIncome)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name, StringComparer.Ordinal)
            .Select(category =>
            {
                var budget = budgetByCategory.GetValueOrDefault(category.Id);
                var actual = actualByCategory.GetValueOrDefault(category.Id);
                return new BudgetRowView(category.Id, category.Name, category.Group, budget, actual, budget - actual);
            })
            .ToList();

        return new BudgetView(month, rows, rows.Sum(row => row.BudgetCents), rows.Sum(row => row.ActualCents));
    }
}
