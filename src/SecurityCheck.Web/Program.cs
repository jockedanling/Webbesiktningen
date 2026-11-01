using SecurityCheck.Web.Components;
using SecurityCheck.Core.Checks;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Decorators;
using SecurityCheck.Core.Scanning;
using SecurityCheck.Core.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();


// Decorators kopplas ihop, så att SecurityScanner bara känner till ISecurityCheck.
ISecurityCheck Decorate(ISecurityCheck check) => 
    new LoggingDecorator(
        new ErrorHandlingDecorator(
            new TimeoutDecorator(check, TimeSpan.FromSeconds(10)),
            loggerFactory.CreateLogger<ErrorHandlingDecorator>()),
            loggerFactory.CreateLogger<LoggingDecorator>());

            ISecurityCheck[] checks =
            [
                Decorate(new TlsCertificateCheck(new CertificateFetcher(), TimeProvider.System)),
                Decorate(new EmailProtectionCheck(new TxtRecordResolver())),
                Decorate(new HttpsRedirectCheck(new RedirectFetcher())),
            ];

            return new SecurityScanner(checks, new DomainValidator(new DnsResolver()));
        });
    


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
