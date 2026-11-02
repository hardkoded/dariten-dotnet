using System.Globalization;

namespace Dariten.Core;

public static class LedgerMutations
{
    public static ActionResult CreateAccount(LedgerSnapshot snapshot, AccountInput input)
    {
        try
        {
            var data = InputParser.ParseAccount(input);
            var now = Timestamp();
            snapshot.Accounts.Add(new AccountRecord
            {
                Id = NewId(),
                Name = data.Name,
                Type = data.Type,
                Institution = data.Institution,
                OpeningBalanceCents = data.OpeningBalanceCents,
                CreatedAt = now,
                UpdatedAt = now,
            });
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult UpdateAccount(LedgerSnapshot snapshot, string id, AccountInput input)
    {
        try
        {
            var data = InputParser.ParseAccount(input);
            var index = snapshot.Accounts.FindIndex(account => account.Id == id);
            if (index < 0)
            {
                return ActionResult.Fail("That account no longer exists.");
            }

            var existing = snapshot.Accounts[index];
            snapshot.Accounts[index] = existing with
            {
                Name = data.Name,
                Type = data.Type,
                Institution = data.Institution,
                OpeningBalanceCents = data.OpeningBalanceCents,
                UpdatedAt = Timestamp(),
            };
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult DeleteAccount(LedgerSnapshot snapshot, string id)
    {
        if (snapshot.Transactions.Any(transaction => transaction.AccountId == id))
        {
            return ActionResult.Fail("Move or delete this account's transactions first.");
        }

        snapshot.Accounts.RemoveAll(account => account.Id == id);
        return ActionResult.Success();
    }

    public static ActionResult CreateCategory(LedgerSnapshot snapshot, CategoryInput input)
    {
        try
        {
            var data = InputParser.ParseCategory(input);
            if (NameTaken(snapshot, data.Name, null))
            {
                return ActionResult.Fail("A category with that name already exists.");
            }

            var last = snapshot.Categories.Count == 0 ? 0 : snapshot.Categories.Max(category => category.SortOrder);
            snapshot.Categories.Add(new CategoryRecord
            {
                Id = NewId(),
                Name = data.Name,
                Group = data.Group,
                IsIncome = data.IsIncome,
                SortOrder = last + 10,
                CreatedAt = Timestamp(),
            });
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult UpdateCategory(LedgerSnapshot snapshot, string id, CategoryInput input)
    {
        try
        {
            var data = InputParser.ParseCategory(input);
            if (NameTaken(snapshot, data.Name, id))
            {
                return ActionResult.Fail("A category with that name already exists.");
            }

            var index = snapshot.Categories.FindIndex(category => category.Id == id);
            if (index < 0)
            {
                return ActionResult.Fail("That category no longer exists.");
            }

            var existing = snapshot.Categories[index];
            snapshot.Categories[index] = existing with
            {
                Name = data.Name,
                Group = data.Group,
                IsIncome = data.IsIncome,
            };
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult DeleteCategory(LedgerSnapshot snapshot, string id)
    {
        if (snapshot.Transactions.Any(transaction => transaction.CategoryId == id))
        {
            return ActionResult.Fail("Recategorize this category's transactions first.");
        }

        snapshot.Budgets.RemoveAll(budget => budget.CategoryId == id);
        snapshot.Categories.RemoveAll(category => category.Id == id);
        return ActionResult.Success();
    }

    public static ActionResult CreateTransaction(LedgerSnapshot snapshot, TransactionInput input)
    {
        try
        {
            var data = InputParser.ParseTransaction(input);
            var missing = RequireAccountAndCategory(snapshot, data.AccountId, data.CategoryId);
            if (missing is not null)
            {
                return missing.Value;
            }

            var now = Timestamp();
            snapshot.Transactions.Add(new TransactionRecord
            {
                Id = NewId(),
                Date = data.Date,
                AmountCents = data.AmountCents,
                Payee = data.Payee,
                Memo = data.Memo,
                AccountId = data.AccountId,
                CategoryId = data.CategoryId,
                CreatedAt = now,
                UpdatedAt = now,
            });
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult UpdateTransaction(LedgerSnapshot snapshot, string id, TransactionInput input)
    {
        try
        {
            var data = InputParser.ParseTransaction(input);
            var missing = RequireAccountAndCategory(snapshot, data.AccountId, data.CategoryId);
            if (missing is not null)
            {
                return missing.Value;
            }

            var index = snapshot.Transactions.FindIndex(transaction => transaction.Id == id);
            if (index < 0)
            {
                return ActionResult.Fail("That transaction no longer exists.");
            }

            var existing = snapshot.Transactions[index];
            snapshot.Transactions[index] = existing with
            {
                Date = data.Date,
                AmountCents = data.AmountCents,
                Payee = data.Payee,
                Memo = data.Memo,
                AccountId = data.AccountId,
                CategoryId = data.CategoryId,
                UpdatedAt = Timestamp(),
            };
            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    public static ActionResult DeleteTransaction(LedgerSnapshot snapshot, string id)
    {
        snapshot.Transactions.RemoveAll(transaction => transaction.Id == id);
        return ActionResult.Success();
    }

    public static ActionResult SaveBudgets(LedgerSnapshot snapshot, BudgetBatchInput input)
    {
        try
        {
            var data = InputParser.ParseBudgetBatch(input);
            var now = Timestamp();
            foreach (var item in data.Items)
            {
                var index = snapshot.Budgets.FindIndex(budget => budget.CategoryId == item.CategoryId && budget.Month == data.Month);
                if (index >= 0)
                {
                    snapshot.Budgets[index] = snapshot.Budgets[index] with
                    {
                        AmountCents = item.AmountCents,
                        UpdatedAt = now,
                    };
                }
                else
                {
                    snapshot.Budgets.Add(new BudgetRecord
                    {
                        Id = NewId(),
                        CategoryId = item.CategoryId,
                        Month = data.Month,
                        AmountCents = item.AmountCents,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
            }

            return ActionResult.Success();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return ActionResult.Fail(ex.Message);
        }
    }

    private static bool NameTaken(LedgerSnapshot snapshot, string name, string? exceptId) =>
        snapshot.Categories.Any(category => category.Name == name && category.Id != exceptId);

    private static ActionResult? RequireAccountAndCategory(LedgerSnapshot snapshot, string accountId, string categoryId)
    {
        var account = snapshot.Accounts.Any(item => item.Id == accountId);
        var category = snapshot.Categories.Any(item => item.Id == categoryId);
        return account && category ? null : ActionResult.Fail("Choose an account and a category that still exist.");
    }

    private static string NewId() => Guid.NewGuid().ToString("D");

    private static string Timestamp() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
