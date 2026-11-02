namespace Dariten.Core;

/// <summary>Browser copy of the Dariten ledger. Dates are YYYY-MM-DD; timestamps are ISO-8601 UTC.</summary>
public sealed record AccountRecord
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Institution { get; init; }
    public int OpeningBalanceCents { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public sealed record CategoryRecord
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }
    public bool IsIncome { get; init; }
    public int SortOrder { get; init; }
    public required string CreatedAt { get; init; }
}

public sealed record TransactionRecord
{
    public required string Id { get; init; }
    public required string Date { get; init; }
    public int AmountCents { get; init; }
    public required string Payee { get; init; }
    public string? Memo { get; init; }
    public required string AccountId { get; init; }
    public required string CategoryId { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public sealed record BudgetRecord
{
    public required string Id { get; init; }
    public required string CategoryId { get; init; }
    public required string Month { get; init; }
    public int AmountCents { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public sealed class LedgerSnapshot
{
    public List<AccountRecord> Accounts { get; set; } = [];
    public List<CategoryRecord> Categories { get; set; } = [];
    public List<TransactionRecord> Transactions { get; set; } = [];
    public List<BudgetRecord> Budgets { get; set; } = [];
}

public readonly record struct ActionResult(bool Ok, string? Error)
{
    public static ActionResult Success() => new(true, null);

    public static ActionResult Fail(string error) => new(false, error);
}
