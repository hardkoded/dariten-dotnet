using Dariten.Services;
using Microsoft.AspNetCore.Components;

namespace Dariten.Shared;

public abstract class LedgerPage : ComponentBase, IDisposable
{
    [Inject]
    protected LedgerService Ledger { get; set; } = default!;

    protected override void OnInitialized() => Ledger.Changed += HandleChanged;

    protected virtual void OnLedgerChanged()
    {
    }

    private void HandleChanged() => _ = InvokeAsync(() =>
    {
        OnLedgerChanged();
        StateHasChanged();
    });

    public void Dispose() => Ledger.Changed -= HandleChanged;
}
