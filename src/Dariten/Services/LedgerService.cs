using Dariten.Core;

namespace Dariten.Services;

public sealed class LedgerService(IndexedDbLedgerStore store)
{
    private Task? _loading;

    public LedgerSnapshot? Snapshot { get; private set; }
    public string? Error { get; private set; }
    public bool IsLoading { get; private set; } = true;
    public event Action? Changed;

    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    public async Task<ActionResult> CreateAccountAsync(AccountInput input) =>
        await CommitAsync(() => LedgerMutations.CreateAccount(Snapshot!, input));

    public async Task<ActionResult> UpdateAccountAsync(string id, AccountInput input) =>
        await CommitAsync(() => LedgerMutations.UpdateAccount(Snapshot!, id, input));

    public async Task<ActionResult> DeleteAccountAsync(string id) =>
        await CommitAsync(() => LedgerMutations.DeleteAccount(Snapshot!, id));

    public async Task<ActionResult> CreateCategoryAsync(CategoryInput input) =>
        await CommitAsync(() => LedgerMutations.CreateCategory(Snapshot!, input));

    public async Task<ActionResult> UpdateCategoryAsync(string id, CategoryInput input) =>
        await CommitAsync(() => LedgerMutations.UpdateCategory(Snapshot!, id, input));

    public async Task<ActionResult> DeleteCategoryAsync(string id) =>
        await CommitAsync(() => LedgerMutations.DeleteCategory(Snapshot!, id));

    public async Task<ActionResult> CreateTransactionAsync(TransactionInput input) =>
        await CommitAsync(() => LedgerMutations.CreateTransaction(Snapshot!, input));

    public async Task<ActionResult> UpdateTransactionAsync(string id, TransactionInput input) =>
        await CommitAsync(() => LedgerMutations.UpdateTransaction(Snapshot!, id, input));

    public async Task<ActionResult> DeleteTransactionAsync(string id) =>
        await CommitAsync(() => LedgerMutations.DeleteTransaction(Snapshot!, id));

    public async Task<ActionResult> SaveBudgetsAsync(BudgetBatchInput input) =>
        await CommitAsync(() => LedgerMutations.SaveBudgets(Snapshot!, input));

    public async Task<ActionResult> ResetSampleAsync()
    {
        if (Snapshot is null)
        {
            return ActionResult.Fail("The ledger is still loading.");
        }

        Snapshot = SeedData.Build();
        return await PersistAsync(ActionResult.Success());
    }

    public async Task DownloadCsvAsync(string csv)
    {
        await store.DownloadTextAsync("transactions.csv", csv, "text/csv;charset=utf-8");
    }

    public Task<bool> ConfirmAsync(string message) => store.ConfirmAsync(message);

    private async Task LoadAsync()
    {
        try
        {
            var snapshot = await store.LoadAsync();
            if (snapshot.Accounts.Count == 0)
            {
                snapshot = SeedData.Build();
                await store.SaveAsync(snapshot);
            }

            Snapshot = snapshot;
            Error = null;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
            Changed?.Invoke();
        }
    }

    private async Task<ActionResult> CommitAsync(Func<ActionResult> mutate)
    {
        if (Snapshot is null)
        {
            return ActionResult.Fail("The ledger is still loading.");
        }

        var result = mutate();
        if (!result.Ok)
        {
            return result;
        }

        return await PersistAsync(result);
    }

    private async Task<ActionResult> PersistAsync(ActionResult result)
    {
        try
        {
            await store.SaveAsync(Snapshot!);
            Error = null;
            Changed?.Invoke();
            return result;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return ActionResult.Fail(ex.Message);
        }
    }
}
