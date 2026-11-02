using Dariten.Core;

namespace Dariten.Unit;

public sealed class FinanceTests
{
    [Test]
    public void Rolls_expense_categories_into_a_spending_snapshot()
    {
        var snapshot = Finance.SpendingByCategory(
        [
            (-12844, "Groceries", false),
            (-8633, "Groceries", false),
            (320000, "Paycheck", true),
            (-1480, "Dining Out", false),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Has.Count.EqualTo(2));
            Assert.That(snapshot[0], Is.EqualTo(new SpendingSlice("Groceries", 21477)));
        });
    }

    [Test]
    public void Filters_transactions_by_account_category_and_date()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Finance.MatchesTransactionFilters("chk", "groc", "2026-09-08", new TransactionFilters { AccountId = "chk", From = "2026-09-01" }), Is.True);
            Assert.That(Finance.MatchesTransactionFilters("chk", "groc", "2026-09-08", new TransactionFilters { CategoryId = "gas" }), Is.False);
            Assert.That(Finance.MatchesTransactionFilters("chk", "groc", "2026-09-08", new TransactionFilters { To = "2026-09-01" }), Is.False);
        });
    }

    [Test]
    public void Computes_savings_rate_as_leftover_income_over_income()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Finance.SavingsRatePercent(721000, 321310), Is.EqualTo(55));
            Assert.That(Finance.SavingsRatePercent(10000, 10000), Is.EqualTo(0));
            Assert.That(Finance.SavingsRatePercent(10000, 12000), Is.EqualTo(-20));
            Assert.That(Finance.SavingsRatePercent(0, 5000), Is.EqualTo(0));
            Assert.That(Finance.SavingsRatePercent(-100, 50), Is.EqualTo(0));
        });
    }

    [Test]
    public void Computes_budget_remaining_and_percent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Finance.BudgetProgress(40000, 24877), Is.EqualTo(new BudgetProgress(15123, 62, false)));
            Assert.That(Finance.BudgetProgress(5000, 6200).Over, Is.True);
        });
    }

    [Test]
    public void Exports_transactions_as_escaped_csv()
    {
        var csv = Finance.TransactionsToCsv(
        [
            new CsvTransaction("2026-09-03", "Whole Foods, \"Market\"", "Groceries", "Everyday Checking", -12844, "Weekly shop"),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(csv.Split('\n')[0], Is.EqualTo("Date,Payee,Category,Account,Amount,Memo"));
            Assert.That(csv, Does.Contain("-128.44"));
            Assert.That(csv, Does.Contain("\"Whole Foods, \"\"Market\"\"\""));
        });
    }
}
