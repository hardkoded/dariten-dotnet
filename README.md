# Dariten (.NET)

A **demo-only** personal finance app in the spirit of a simplified Quicken, ported to **Blazor WebAssembly** from [kblok/dariten](https://github.com/kblok/dariten). The in-app name is **Quicken Demo**.

The live Next.js demo is [https://dariten.vercel.app](https://dariten.vercel.app). This repository is the .NET port: the same sample household, the same integer-cent balances, and the same pages (dashboard, accounts, transactions, categories, monthly budgets).

This is **not** production software and **not** an official Quicken product. There is **no login** and **no remote database**. Do not enter real bank credentials or real personal transactions.

## What is in the demo

- **Dashboard** — net worth, this month’s income and spending, savings rate, account balances, a spending snapshot, recent activity
- **Accounts** — checking, savings, credit card, and cash; create, edit, and delete; live balances
- **Transactions** — register, filters (account, category, date range), add/edit/delete, CSV export of the current view
- **Categories** — the seeded chart of accounts, plus add, rename, and delete when unused
- **Budgets** — monthly budget versus actual by expense category

Amounts are **integer cents**. A credit card may have a **negative opening balance** (amount owed). **Net worth is the sum of every account balance**, including that liability.

“This month” on the dashboard follows the calendar month in **UTC**. The sample activity is July–September 2026, so a later month shows balances and recent transactions with little or no new cash flow until you add some.

## Where the data lives

Each browser keeps its own ledger in **IndexedDB**, database name **`dariten`** (version 1). Object stores are `accounts`, `categories`, `transactions`, and `budgets`, matching the Dexie schema in the Next.js app. The Blazor app talks to IndexedDB through `wwwroot/js/ledger.js`.

- The first visit writes the sample household when the accounts store is empty (4 accounts, 17 categories, 59 transactions, and September 2026 budgets). The records are ported from `lib/seed-data.ts` in kblok/dariten and live in `src/Dariten.Core/SeedData.cs`.
- Later edits stay in that browser and survive reloads. Another browser, or a private window, gets its own copy.
- **Reset sample data** in the green banner asks you to confirm, then replaces this browser’s ledger with the original sample. It does not affect anyone else.
- You can also delete the `dariten` database under DevTools → Application → IndexedDB. The next load seeds it again.

The green banner is the warning: this is a local demo, and you should not enter real bank data.

## Layout

| Path | What it is |
| --- | --- |
| `src/Dariten.Core` | Models, seed data, money, filters, budgets, and ledger edits. No UI and no browser APIs. |
| `src/Dariten` | Blazor WebAssembly standalone app |
| `tests/Dariten.Unit` | NUnit tests for money, seed views, and validators. No browser and no API key. |
| `tests/Dariten.E2E` | Browser smoke tests with [E2E](https://www.nuget.org/packages/E2E) and [E2E.NUnit](https://www.nuget.org/packages/E2E.NUnit) |
| `.github/workflows/azure-static-web-apps.yml` | Build, unit test, and optional Azure Static Web Apps deploy |

## Local run

You need the **.NET 10 SDK**.

```bash
dotnet run --project src/Dariten/Dariten.csproj
```

Or, with hot reload:

```bash
dotnet watch --project src/Dariten/Dariten.csproj
```

Open [http://localhost:5173](http://localhost:5173). The dev server uses the `http` launch profile. The dashboard heading appears without a remote database. The sample household loads from IndexedDB on first visit.

```bash
dotnet build
dotnet test tests/Dariten.Unit/Dariten.Unit.csproj
```

`dotnet build` uses `Dariten.slnx`.

## End-to-end tests

The E2E project starts `dotnet run` for the Blazor app on a free loopback port before the tests, then stops it when the run ends. [E2E 0.9.0](https://www.nuget.org/packages/E2E) does not launch an `app.command` from `e2e.config.json`, so `tests/Dariten.E2E/AppCommands.cs` owns that process. Set `E2E_BASE_URL` to skip the launch and use a server that is already running.

```bash
dotnet test tests/Dariten.E2E/Dariten.E2E.csproj
```

`dotnet` must be on `PATH` for the child process that serves the app. The first run downloads Playwright’s Chromium build (network required). On Linux, if Chromium fails to launch because system libraries are missing, install them from the test output:

```bash
pwsh tests/Dariten.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

Or set `E2E_SKIP_BROWSER_INSTALL=1` after you have installed the browser yourself.

### What the suite covers

Locator checks (no model):

- The dashboard opens with the seeded household and the local-demo banner.
- Accounts navigation lists the seed accounts.
- Adding a transaction and choosing **Reset sample data** restores the starter register.

Agent checks (a live model):

- `Agent_adds_a_transaction` uses `Agent.ActAsync` / `Agent.AssertAsync`, then a locator expect for the new payee.

Without `OPENAI_API_KEY`, that agent test is **ignored**. The locator tests still run. A passing agent step is recorded under `tests/Dariten.E2E/.e2e/cache` and the next run replays it without calling the model until the screen no longer matches. That cache is gitignored.

### Model environment variables

The default agent is an OpenAI-compatible chat-completions client (`OpenAiCompatibleModel`), configured in `tests/Dariten.E2E/e2e.config.json` as `gpt-4.1-mini` with provider `openai`.

| Variable | Required | Purpose |
| --- | --- | --- |
| `OPENAI_API_KEY` | For agent tests | Bearer token. Never commit it. |
| `OPENAI_BASE_URL` | No | API root. Default `https://api.openai.com/v1`. Set this for another OpenAI-compatible server. Requests go to `{base}/chat/completions`. |
| `E2E_MODEL` | No | Model id. Default `gpt-4.1-mini`, or the model in `e2e.config.json` when this variable is unset. |
| `E2E_BASE_URL` | No | Skip launching `dotnet run` and open this origin instead, for example `http://127.0.0.1:5173`. |

Example against a local OpenAI-compatible server:

```bash
export OPENAI_API_KEY=sk-local
export OPENAI_BASE_URL=http://127.0.0.1:11434/v1
export E2E_MODEL=llama3.1
dotnet test tests/Dariten.E2E/Dariten.E2E.csproj
```

### Reporters (E2E 0.9.0)

`tests/Dariten.E2E/RunReporting.cs` is the run fixture that calls the reporters listed in `e2e.config.json` when the run ends.

| Reporter | What it does |
| --- | --- |
| `report.json` | Always written to `tests/Dariten.E2E/.e2e/report.json`. Every other reporter reads it. |
| `LiveEventReporter` | Watches `RunEvent`s as they happen and prints a line per test, then an `Events` summary row. |
| `MarkdownFileReporter` | Renders the run with `MarkdownReport.RenderMarkdownReport` to `.e2e/report.md`. |
| `E2E.GitHub.GitHubReporter` | On GitHub Actions, writes the job summary and posts one pull request comment, then edits it on the next run. Needs `GITHUB_TOKEN` and `pull-requests: write`. Does nothing elsewhere. |

The workflow is `.github/workflows/e2e.yml`. Run only what failed last time with `E2E_LAST_FAILED=1 dotnet test tests/Dariten.E2E/Dariten.E2E.csproj`. The GitHub comment then shows the whole suite, not just the rerun.

Docs: [https://www.hardkoded.com/e2e-dotnet/](https://www.hardkoded.com/e2e-dotnet/) and [https://github.com/hardkoded/e2e-dotnet](https://github.com/hardkoded/e2e-dotnet).

## Azure Static Web Apps

The GitHub Actions workflow builds the solution, runs the unit tests, publishes the Blazor app, and uploads `published/wwwroot` when the deployment token exists. It does **not** create Azure resources. `staticwebapp.config.json` in `wwwroot` sends unknown paths back to `index.html` so client-side routes such as `/transactions` work on a refresh, and it sets WASM content types.

### Create the site and connect GitHub

1. Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and sign in: `az login`.
2. Create a resource group and an empty Static Web App (pick your region and resource names):

   ```bash
   az group create --name rg-dariten --location eastus2
   az staticwebapp create \
     --name dariten-dotnet \
     --resource-group rg-dariten \
     --location eastus2 \
     --sku Free
   ```

3. Link the GitHub repository when you are ready for Azure to deploy from this repo. You can do that from the Static Web App in the Azure portal (**Manage deployment token** / GitHub connection), or with the CLI if the GitHub CLI is already authenticated:

   ```bash
   az staticwebapp connect \
     --name dariten-dotnet \
     --resource-group rg-dariten
   ```

   The portal flow is the one that adds the secret for you. If you created the app without a GitHub connection, copy the deployment token and store it yourself (next step).

4. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**. Name it exactly `AZURE_STATIC_WEB_APPS_API_TOKEN` and paste the deployment token from **Manage deployment token**. Do not commit the token.
5. Push to `main`. The workflow deploys the published `wwwroot`. Pull requests get a preview environment when the token is present; closing the pull request runs the `close` action.

Until that secret exists, the workflow still builds and unit-tests, then skips the deploy step.

There is no `azure.yaml` or Bicep in this repo. The CLI commands above are the whole infrastructure step.

## Product notes

- Transfers in the seed are two register lines (out of one account, into another). There is no linked-transfer engine.
- Deleting an account or category that still has transactions is refused. Deleting a category also removes its budgets.
- Credit-card balances render as a negative amount when money is owed.

## License

MIT. See [LICENSE](LICENSE).
