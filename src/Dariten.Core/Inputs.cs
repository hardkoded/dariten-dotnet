namespace Dariten.Core;

public sealed record AccountInput(string Name, string Type, string? Institution, string OpeningBalance);

public sealed record CategoryInput(string Name, string Group, bool IsIncome);

public sealed record TransactionInput(
    string Date,
    string Payee,
    string AccountId,
    string CategoryId,
    string Amount,
    string Flow,
    string? Memo);

public sealed record BudgetItemInput(string CategoryId, string Amount);

public sealed record BudgetBatchInput(string Month, IReadOnlyList<BudgetItemInput> Items);

public sealed record ParsedAccount(string Name, string Type, string? Institution, int OpeningBalanceCents);

public sealed record ParsedTransaction(string Date, string Payee, string AccountId, string CategoryId, int AmountCents, string? Memo);

public sealed record ParsedBudgetItem(string CategoryId, int AmountCents);

public sealed record ParsedBudgetBatch(string Month, IReadOnlyList<ParsedBudgetItem> Items);

public static class InputParser
{
    public static ParsedAccount ParseAccount(AccountInput input)
    {
        var name = RequireText(input.Name, "Name is required", 80, "Name must be at most 80 characters");
        if (!AccountTypes.IsKnown(input.Type))
        {
            throw new FormatException("Choose a valid account type.");
        }

        var institution = OptionalText(input.Institution, 80, "Institution must be at most 80 characters");
        if (string.IsNullOrWhiteSpace(input.OpeningBalance))
        {
            throw new FormatException("Opening balance is required");
        }

        return new ParsedAccount(name, input.Type, institution, Money.DollarsToCents(input.OpeningBalance));
    }

    public static CategoryInput ParseCategory(CategoryInput input)
    {
        var name = RequireText(input.Name, "Name is required", 80, "Name must be at most 80 characters");
        var group = RequireText(input.Group, "Group is required", 40, "Group must be at most 40 characters");
        return new CategoryInput(name, group, input.IsIncome);
    }

    public static ParsedTransaction ParseTransaction(TransactionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Date))
        {
            throw new FormatException("Date is required");
        }

        var date = CalendarDates.ParseDateOnly(input.Date.Trim());
        var payee = RequireText(input.Payee, "Payee is required", 80, "Payee must be at most 80 characters");
        if (string.IsNullOrWhiteSpace(input.AccountId))
        {
            throw new FormatException("Account is required");
        }

        if (string.IsNullOrWhiteSpace(input.CategoryId))
        {
            throw new FormatException("Category is required");
        }

        if (input.Flow is not ("in" or "out"))
        {
            throw new FormatException("Choose Income or Expense");
        }

        if (string.IsNullOrWhiteSpace(input.Amount))
        {
            throw new FormatException("Amount is required");
        }

        var unsigned = Money.DollarsToCents(input.Amount);
        if (unsigned < 0)
        {
            throw new FormatException("Enter the amount as a positive number and choose Income or Expense");
        }

        var memo = OptionalText(input.Memo, 160, "Memo must be at most 160 characters");
        return new ParsedTransaction(
            date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            payee,
            input.AccountId.Trim(),
            input.CategoryId.Trim(),
            input.Flow == "out" ? -unsigned : unsigned,
            memo);
    }

    public static ParsedBudgetBatch ParseBudgetBatch(BudgetBatchInput input)
    {
        if (!CalendarDates.IsMonth(input.Month))
        {
            throw new FormatException("Month must be YYYY-MM");
        }

        var items = input.Items.Select(item =>
        {
            if (string.IsNullOrWhiteSpace(item.CategoryId))
            {
                throw new FormatException("Category is required");
            }

            if (string.IsNullOrWhiteSpace(item.Amount))
            {
                throw new FormatException("Amount is required");
            }

            return new ParsedBudgetItem(item.CategoryId, Money.DollarsToCents(item.Amount));
        }).ToList();

        return new ParsedBudgetBatch(input.Month, items);
    }

    private static string RequireText(string? value, string required, int max, string tooLong)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            throw new FormatException(required);
        }

        if (text.Length > max)
        {
            throw new FormatException(tooLong);
        }

        return text;
    }

    private static string? OptionalText(string? value, int max, string tooLong)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length > max)
        {
            throw new FormatException(tooLong);
        }

        return text;
    }
}
