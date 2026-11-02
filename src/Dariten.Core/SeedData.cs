using System.Globalization;
using System.Text.RegularExpressions;

namespace Dariten.Core;

/// <summary>
/// Starter household ported from kblok/dariten <c>lib/seed-data.ts</c>.
/// Written into IndexedDB when a browser's ledger is empty.
/// </summary>
public static partial class SeedData
{
    public const string DatabaseName = "dariten";

    private static readonly DateTimeOffset SeedEpoch = DateTimeOffset.Parse(
        "2026-07-01T00:00:00.000Z",
        CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind);

    private readonly record struct SeedCategory(string Name, string Group, bool IsIncome, int SortOrder);
    private readonly record struct SeedAccount(string Key, string Name, string Type, string? Institution, int OpeningBalanceCents);
    private readonly record struct SeedTxn(string Date, string Account, string Category, string Payee, int AmountCents, string? Memo = null);

    private static readonly SeedCategory[] Categories =
    [
        new("Paycheck", "Income", true, 10),
        new("Interest", "Income", true, 20),
        new("Other Income", "Income", true, 30),
        new("Rent / Mortgage", "Housing", false, 40),
        new("Utilities", "Housing", false, 50),
        new("Groceries", "Food", false, 60),
        new("Dining Out", "Food", false, 70),
        new("Gas", "Transport", false, 80),
        new("Transit", "Transport", false, 90),
        new("Shopping", "Lifestyle", false, 100),
        new("Entertainment", "Lifestyle", false, 110),
        new("Subscriptions", "Lifestyle", false, 120),
        new("Medical", "Health", false, 130),
        new("Fitness", "Health", false, 140),
        new("Credit Card Payment", "Transfers", false, 150),
        new("Transfer", "Transfers", false, 160),
        new("Uncategorized", "Other", false, 170),
    ];

    private static readonly SeedAccount[] Accounts =
    [
        new("checking", "Everyday Checking", AccountTypes.Checking, "Harbor Credit Union", 325000),
        new("savings", "Rainy Day Savings", AccountTypes.Savings, "Harbor Credit Union", 1180000),
        new("card", "Travel Rewards Visa", AccountTypes.CreditCard, "Northline Card", -112000),
        new("cash", "Wallet Cash", AccountTypes.Cash, null, 24000),
    ];

    private static readonly SeedTxn[] Transactions =
    [
        new("2026-07-01", "checking", "Paycheck", "Northline Studio", 320000, "Biweekly pay"),
        new("2026-07-01", "checking", "Rent / Mortgage", "Oak & Pine Residences", -185000),
        new("2026-07-05", "checking", "Utilities", "City Light & Water", -8840),
        new("2026-07-06", "checking", "Groceries", "Whole Foods", -11900),
        new("2026-07-08", "checking", "Transfer", "Transfer to Savings", -40000),
        new("2026-07-08", "savings", "Transfer", "Transfer from Checking", 40000),
        new("2026-07-10", "card", "Dining Out", "Pizzeria Locale", -3675),
        new("2026-07-12", "card", "Gas", "Shell", -4780),
        new("2026-07-15", "checking", "Paycheck", "Northline Studio", 320000),
        new("2026-07-15", "checking", "Credit Card Payment", "Northline Card Payment", -30000),
        new("2026-07-15", "card", "Credit Card Payment", "Payment thank you", 30000),
        new("2026-07-18", "card", "Shopping", "REI", -12400),
        new("2026-07-20", "checking", "Medical", "Lakeside Clinic", -6500),
        new("2026-07-22", "checking", "Groceries", "Trader Joe's", -8150),
        new("2026-07-25", "card", "Entertainment", "Alamo Drafthouse", -4500),
        new("2026-07-28", "card", "Dining Out", "Taco Nido", -2210),
        new("2026-07-31", "savings", "Interest", "Harbor Credit Union", 388),

        new("2026-08-01", "checking", "Paycheck", "Northline Studio", 320000),
        new("2026-08-01", "checking", "Rent / Mortgage", "Oak & Pine Residences", -185000),
        new("2026-08-03", "checking", "Utilities", "Harbor Fiber", -7999, "Internet"),
        new("2026-08-04", "checking", "Groceries", "Whole Foods", -14210),
        new("2026-08-05", "checking", "Transfer", "Transfer to Savings", -40000),
        new("2026-08-05", "savings", "Transfer", "Transfer from Checking", 40000),
        new("2026-08-07", "card", "Dining Out", "Sushi Katsu", -4250),
        new("2026-08-09", "card", "Gas", "Shell", -5200),
        new("2026-08-11", "card", "Subscriptions", "Spotify", -1199),
        new("2026-08-12", "checking", "Groceries", "Trader Joe's", -9120),
        new("2026-08-14", "savings", "Interest", "Harbor Credit Union", 412),
        new("2026-08-15", "checking", "Paycheck", "Northline Studio", 320000),
        new("2026-08-15", "checking", "Credit Card Payment", "Northline Card Payment", -40000),
        new("2026-08-15", "card", "Credit Card Payment", "Payment thank you", 40000),
        new("2026-08-16", "card", "Dining Out", "Sweetgreen", -2800),
        new("2026-08-18", "card", "Shopping", "Madewell", -8845),
        new("2026-08-20", "checking", "Fitness", "Movement Gym", -4900),
        new("2026-08-22", "cash", "Dining Out", "Blue Bottle", -550),
        new("2026-08-25", "checking", "Groceries", "Farmers Market", -7618),
        new("2026-08-28", "card", "Entertainment", "Metro Cinema", -3200),
        new("2026-08-30", "card", "Dining Out", "Bar Tonno", -5420),

        new("2026-09-01", "checking", "Paycheck", "Northline Studio", 320000),
        new("2026-09-01", "checking", "Rent / Mortgage", "Oak & Pine Residences", -185000),
        new("2026-09-02", "checking", "Utilities", "City Light & Water", -9412),
        new("2026-09-03", "checking", "Groceries", "Whole Foods", -12844),
        new("2026-09-04", "checking", "Transfer", "Transfer to Savings", -40000),
        new("2026-09-04", "savings", "Transfer", "Transfer from Checking", 40000),
        new("2026-09-05", "card", "Dining Out", "Blue Bottle", -675),
        new("2026-09-06", "card", "Gas", "Shell", -4820),
        new("2026-09-07", "card", "Subscriptions", "Netflix", -1599),
        new("2026-09-08", "checking", "Groceries", "Trader Joe's", -8633),
        new("2026-09-10", "card", "Dining Out", "Chipotle", -1480),
        new("2026-09-11", "card", "Transit", "SpotHero", -1800),
        new("2026-09-12", "card", "Shopping", "Target", -6219),
        new("2026-09-15", "checking", "Paycheck", "Northline Studio", 320000),
        new("2026-09-15", "checking", "Credit Card Payment", "Northline Card Payment", -35000),
        new("2026-09-15", "card", "Credit Card Payment", "Payment thank you", 35000),
        new("2026-09-16", "card", "Dining Out", "Sweetgreen", -1640),
        new("2026-09-17", "checking", "Medical", "CVS Pharmacy", -2788),
        new("2026-09-18", "checking", "Transfer", "ATM Withdrawal", -6000),
        new("2026-09-18", "cash", "Transfer", "ATM Deposit", 6000),
        new("2026-09-19", "cash", "Groceries", "Farmers Market", -3400),
    ];

    private static readonly (string Category, int AmountCents)[] SeptemberBudgets =
    [
        ("Rent / Mortgage", 185000),
        ("Utilities", 12000),
        ("Groceries", 40000),
        ("Dining Out", 20000),
        ("Gas", 12000),
        ("Transit", 6000),
        ("Shopping", 15000),
        ("Entertainment", 8000),
        ("Subscriptions", 5000),
        ("Medical", 8000),
        ("Fitness", 5000),
    ];

    public static LedgerSnapshot Build()
    {
        var createdAt = Stamp(0);
        var accountRecords = Accounts.Select(account => new AccountRecord
        {
            Id = $"seed-acct-{account.Key}",
            Name = account.Name,
            Type = account.Type,
            Institution = account.Institution,
            OpeningBalanceCents = account.OpeningBalanceCents,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        }).ToList();
        var accountIds = Accounts.Select((account, index) => (account.Key, accountRecords[index].Id))
            .ToDictionary(pair => pair.Key, pair => pair.Id);

        var categoryRecords = Categories.Select(category => new CategoryRecord
        {
            Id = CategoryId(category.Name),
            Name = category.Name,
            Group = category.Group,
            IsIncome = category.IsIncome,
            SortOrder = category.SortOrder,
            CreatedAt = createdAt,
        }).ToList();
        var categoryIds = categoryRecords.ToDictionary(category => category.Name, category => category.Id);

        var transactionRecords = Transactions.Select((transaction, index) =>
        {
            if (!accountIds.TryGetValue(transaction.Account, out var accountId) ||
                !categoryIds.TryGetValue(transaction.Category, out var categoryId))
            {
                throw new InvalidOperationException($"Missing account or category for {transaction.Payee}");
            }

            var timestamp = Stamp(index + 1);
            return new TransactionRecord
            {
                Id = $"seed-txn-{(index + 1).ToString("D3", CultureInfo.InvariantCulture)}",
                Date = transaction.Date,
                AmountCents = transaction.AmountCents,
                Payee = transaction.Payee,
                Memo = transaction.Memo,
                AccountId = accountId,
                CategoryId = categoryId,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
            };
        }).ToList();

        var budgetRecords = SeptemberBudgets.Select((budget, index) =>
        {
            if (!categoryIds.TryGetValue(budget.Category, out var categoryId))
            {
                throw new InvalidOperationException($"Missing budget category {budget.Category}");
            }

            var timestamp = Stamp(Transactions.Length + index + 1);
            return new BudgetRecord
            {
                Id = $"seed-budget-{CategoryId(budget.Category).Replace("seed-cat-", "", StringComparison.Ordinal)}",
                CategoryId = categoryId,
                Month = "2026-09",
                AmountCents = budget.AmountCents,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
            };
        }).ToList();

        return new LedgerSnapshot
        {
            Accounts = accountRecords,
            Categories = categoryRecords,
            Transactions = transactionRecords,
            Budgets = budgetRecords,
        };
    }

    private static string CategoryId(string name) =>
        "seed-cat-" + SlugPattern().Replace(name.ToLowerInvariant(), "-").Trim('-');

    private static string Stamp(int index) =>
        SeedEpoch.AddSeconds(index).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
