using Dariten.Core;

namespace Dariten.Unit;

public sealed class ValidatorTests
{
    [Test]
    public void Parses_a_checking_account()
    {
        var parsed = InputParser.ParseAccount(new AccountInput("Everyday Checking", "CHECKING", "Harbor Credit Union", "3250.00"));

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Name, Is.EqualTo("Everyday Checking"));
            Assert.That(parsed.Type, Is.EqualTo(AccountTypes.Checking));
            Assert.That(parsed.Institution, Is.EqualTo("Harbor Credit Union"));
            Assert.That(parsed.OpeningBalanceCents, Is.EqualTo(325000));
        });
    }

    [Test]
    public void Stores_expenses_as_negative_cents()
    {
        var parsed = InputParser.ParseTransaction(new TransactionInput(
            "2026-09-19",
            "Farmers Market",
            "acct_1",
            "cat_1",
            "34.00",
            "out",
            ""));

        Assert.Multiple(() =>
        {
            Assert.That(parsed.AmountCents, Is.EqualTo(-3400));
            Assert.That(parsed.Date, Is.EqualTo("2026-09-19"));
            Assert.That(parsed.Memo, Is.Null);
        });
    }

    [Test]
    public void Rejects_a_negative_transaction_amount()
    {
        var error = Assert.Throws<FormatException>(() => InputParser.ParseTransaction(new TransactionInput(
            "2026-09-19", "Cafe", "acct", "cat", "-4.50", "out", null)));
        Assert.That(error!.Message, Does.Contain("positive number"));
    }
}
