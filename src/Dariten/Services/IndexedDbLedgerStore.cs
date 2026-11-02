using Dariten.Core;
using Microsoft.JSInterop;

namespace Dariten.Services;

public sealed class IndexedDbLedgerStore(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<LedgerSnapshot> LoadAsync()
    {
        var module = await ModuleAsync();
        return await module.InvokeAsync<LedgerSnapshot>("loadSnapshot");
    }

    public async Task SaveAsync(LedgerSnapshot snapshot)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("saveSnapshot", snapshot);
    }

    public async Task DownloadTextAsync(string filename, string text, string mimeType)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("downloadText", filename, text, mimeType);
    }

    public async Task<bool> ConfirmAsync(string message)
    {
        return await js.InvokeAsync<bool>("confirm", message);
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/ledger.js");

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
