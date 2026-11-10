using E2E;
using E2E.NUnit;

namespace Dariten.E2E;

/// <summary>
/// Smoke coverage for the Blazor WebAssembly Quicken Demo.
/// Locator checks run without a model. Agent checks need <c>OPENAI_API_KEY</c>
/// (optional <c>OPENAI_BASE_URL</c> and <c>E2E_MODEL</c>).
/// Set <c>E2E_BASE_URL</c> to point at an already running app instead of starting <c>dotnet run</c>.
/// </summary>
public sealed class SmokeTests : E2ETest
{
    protected override string? BaseUrl => AppHost.BaseUrl;

    protected override IAgentModel? CreateModel()
    {
        var model = Environment.GetEnvironmentVariable("E2E_MODEL");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        if (string.IsNullOrWhiteSpace(model) && string.IsNullOrWhiteSpace(baseUrl))
        {
            return base.CreateModel();
        }

        return new OpenAiCompatibleModel(new OpenAiCompatibleModelOptions
        {
            Model = string.IsNullOrWhiteSpace(model) ? "gpt-4.1-mini" : model,
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1" : baseUrl,
            ApiKeyEnv = "OPENAI_API_KEY",
        });
    }

    protected override TimeSpan StepTimeout => TimeSpan.FromSeconds(120);

    [Test]
    public async Task Dashboard_shows_the_seeded_household()
    {
        await App.OpenAsync("/");
        await Expect.That(Screen.GetByRole("heading", "Dashboard")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByTestId("demo-banner")).ToContainTextAsync("Local demo");
        await Expect.That(Screen.GetByTestId("demo-banner")).ToContainTextAsync("Do not enter real bank data");
        await Expect.That(Screen.GetByText("Net worth")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByRole("heading", "Everyday Checking")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByRole("heading", "Travel Rewards Visa")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByText("Farmers Market")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Accounts_page_lists_the_seed_accounts()
    {
        await App.OpenAsync("/");
        await Screen.GetByRole("link", "Accounts").ClickAsync();
        await Expect.That(Screen.GetByRole("heading", "Accounts")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByRole("button", "Add account")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByText("Rainy Day Savings")).ToBeVisibleAsync();
        await Expect.That(Screen.GetByText("Wallet Cash")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Reset_sample_data_restores_the_seed()
    {
        await App.OpenAsync("/transactions");
        await Expect.That(Screen.GetByRole("heading", "Transactions")).ToBeVisibleAsync();
        await Screen.GetByRole("button", "Add transaction").ClickAsync();
        await Screen.GetByRole("textbox", "Payee").FillAsync("E2E Marker");
        await Screen.GetByRole("textbox", "Amount").FillAsync("12.34");
        await Screen.GetByRole("textbox", "Date").FillAsync("2026-09-20");
        await Screen.GetByRole("button", "Save transaction").ClickAsync();
        await Expect.That(Screen.GetByText("E2E Marker")).ToBeVisibleAsync();

        await Screen.GetByRole("button", "Reset sample data").ClickAsync();
        await Screen.GetByRole("button", "Replace sample data").ClickAsync();
        await Expect.That(Screen.GetByText("E2E Marker")).ToBeHiddenAsync();
        await Expect.That(Screen.GetByText("SpotHero")).ToBeVisibleAsync();
    }

    // Demo only: fails on the first attempt and passes on the NUnit retry, to see how the GitHub report shows a test that needed a retry.
    [Test]
    [Retry(2)]
    public async Task Flaky_demo_passes_only_on_retry()
    {
        await App.OpenAsync("/");
        await Expect.That(Screen.GetByRole("heading", "Dashboard")).ToBeVisibleAsync();
        Assert.That(NUnit.Framework.TestContext.CurrentContext.CurrentRepeatCount, Is.GreaterThan(0), "Simulated flake: first attempt fails.");
    }

    [Test]
    [Category("RealModel")]
    public async Task Agent_adds_a_transaction()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            Assert.Ignore("Set OPENAI_API_KEY to run agent tests. Optional: OPENAI_BASE_URL and E2E_MODEL.");
        }

        await App.OpenAsync("/transactions");
        await Agent.ActAsync(
            "Open Add transaction. Create an expense dated 2026-09-20 for payee E2E Coffee, amount 4.50, account Everyday Checking, category Dining Out. Save it. Do not reset sample data and do not change any other transaction.");
        await Agent.AssertAsync("the transaction register lists a payee named E2E Coffee");
        await Expect.That(Screen.GetByText("E2E Coffee")).ToBeVisibleAsync();
    }
}
