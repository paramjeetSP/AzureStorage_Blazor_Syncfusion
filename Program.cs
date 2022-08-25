

using FileManagerSample.Extensions;
using FileManagerSample.Models;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("your license key");
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<HelperClass>();
builder.Services.AddScoped<AzureFileProvider>();
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddSyncfusionBlazor(options => { options.IgnoreScriptIsolation = true; });
#pragma warning restore CS0618 // Type or member is obsolete

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


await app.RunAsync();
