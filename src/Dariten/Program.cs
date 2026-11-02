using Dariten;
using Dariten.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IndexedDbLedgerStore>();
builder.Services.AddScoped<LedgerService>();

await builder.Build().RunAsync();
