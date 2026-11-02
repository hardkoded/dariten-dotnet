using Dariten.Core;

namespace Dariten.Unit;

public sealed class SeedViewTests
{
    private readonly LedgerSnapshot _snapshot = SeedData.Build();

    [Test]
    public void Builds_the_sample_household()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_snapshot.Accounts, Has.Count.EqualTo(4));
            Assert.That(_snapshot.Categories, Has.Count.EqualTo(17));
            Assert.That(_snapshot.Transactions, Has.Count.EqualTo(59));
            Assert.That(_snapshot.Budgets, Has.Count.EqualTo(11));
            Assert.That(_snapshot.Accounts.Single(account => account.Type == AccountTypes.CreditCard).OpeningBalanceCents, Is.LessThan(0));
            Assert.That(_snapshot.Transactions[0].Id, Is.EqualTo("seed-txn-001"));
            Assert.That(_snapshot.Categories.Single(category => category.Name == "Rent / Mortgage").Id, Is.EqualTo("seed-cat-rent-mortgage"));
        });
    }

    [Test]
    public void Orders_accounts_and_keeps_the_card_as_a_liability()
    {
        var accounts = LedgerViews.AccountsWithBalances(_snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(accounts.Select(account => account.Name), Is.EqualTo(new[]
            {
                "Everyday Checking",
                "Rainy Day Savings",
                "Travel Rewards Visa",
                "Wallet Cash",
            }));
            Assert.That(accounts.Single(account => account.Type == AccountTypes.CreditCard).BalanceCents, Is.LessThan(0));
        });
    }

    [Test]
    public void Computes_september_budget_vs_actual_for_groceries()
    {
        var view = LedgerViews.Budgets(_snapshot, "2026-09");
        var groceries = view.Rows.Single(row => row.Name == "Groceries");

        Assert.Multiple(() =>
        {
            Assert.That(groceries.BudgetCents, Is.EqualTo(40000));
            Assert.That(groceries.ActualCents, Is.EqualTo(24877));
            Assert.That(groceries.RemainingCents, Is.EqualTo(15123));
        });
    }

    [Test]
    public void Filters_the_register_and_summarizes_september_cash_flow()
    {
        var checking = _snapshot.Accounts.Single(account => account.Name == "Everyday Checking");
        var rows = LedgerViews.Transactions(_snapshot, new TransactionFilters
        {
            AccountId = checking.Id,
            From = "2026-09-01",
            To = "2026-09-30",
        });
        var dashboard = LedgerViews.Dashboard(_snapshot, "2026-09");

        Assert.Multiple(() =>
        {
            Assert.That(rows.All(row => row.AccountName == "Everyday Checking"), Is.True);
            Assert.That(rows.Any(row => row.Payee == "Oak & Pine Residences"), Is.True);
            Assert.That(rows.Any(row => row.Payee == "Shell"), Is.False);
            Assert.That(dashboard.NetWorthCents, Is.EqualTo(dashboard.Accounts.Sum(account => account.BalanceCents)));
            Assert.That(dashboard.SavingsRatePercent, Is.EqualTo(Finance.SavingsRatePercent(dashboard.IncomeCents, dashboard.SpendingCents)));
            Assert.That(dashboard.IncomeCents, Is.GreaterThan(0));
            Assert.That(dashboard.Recent[0].Payee, Is.EqualTo("Farmers Market"));
            Assert.That(dashboard.Recent[0].Date, Is.EqualTo("2026-09-19"));
        });
    }

    [Test]
    public void Net_worth_is_the_sum_of_balances_including_a_negative_card()
    {
        var dashboard = LedgerViews.Dashboard(_snapshot, "2026-11");
        Assert.Multiple(() =>
        {
            Assert.That(dashboard.IncomeCents, Is.EqualTo(0));
            Assert.That(dashboard.SpendingCents, Is.EqualTo(0));
            Assert.That(dashboard.NetWorthCents, Is.EqualTo(dashboard.Accounts.Sum(account => account.BalanceCents)));
            Assert.That(dashboard.Accounts.Single(account => account.Name == "Travel Rewards Visa").BalanceCents, Is.LessThan(0));
            Assert.That(dashboard.Recent[0].Payee, Is.EqualTo("Farmers Market"));
        });
    }
}
