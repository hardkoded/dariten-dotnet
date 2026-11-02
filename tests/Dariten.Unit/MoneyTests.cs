using Dariten.Core;

namespace Dariten.Unit;

public sealed class MoneyTests
{
    [Test]
    public void Converts_dollar_strings_to_cents()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Money.DollarsToCents("42.50"), Is.EqualTo(4250));
            Assert.That(Money.DollarsToCents("$1,200.05"), Is.EqualTo(120005));
            Assert.That(Money.DollarsToCents("-18.40"), Is.EqualTo(-1840));
            Assert.That(Money.DollarsToCents("0"), Is.EqualTo(0));
        });
    }

    [Test]
    public void Rejects_invalid_amounts()
    {
        var error = Assert.Throws<FormatException>(() => Money.DollarsToCents("twelve"));
        Assert.That(error!.Message, Does.Contain("valid dollar amount"));
    }

    [Test]
    public void Formats_cents_with_a_unicode_minus()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Money.FormatCents(185000), Is.EqualTo("$1,850.00"));
            Assert.That(Money.FormatCents(-4820), Is.EqualTo("−$48.20"));
        });
    }

    [Test]
    public void Round_trips_cents_to_an_input_string()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Money.CentsToDollarInput(-112000), Is.EqualTo("-1120.00"));
            Assert.That(Money.CentsToDollarInput(24000), Is.EqualTo("240.00"));
        });
    }

    [Test]
    public void Computes_account_and_net_worth_balances()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Money.AccountBalanceCents(325000, [320000, -185000, -9412]), Is.EqualTo(450588));
            Assert.That(Money.NetWorthCents([428015, 1245000, -184233, 18600]), Is.EqualTo(1507382));
        });
    }
}
