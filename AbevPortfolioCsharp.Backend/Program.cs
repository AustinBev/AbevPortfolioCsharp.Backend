using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;

// your namespaces
using AbevPortfolioCsharp.Backend.Services;
using AbevPortfolioCsharp.Backend.Services.Email;
using AbevPortfolioCsharp.Backend.Services.RateLimiting;

var builder = FunctionsApplication.CreateBuilder(args);

// optional middleware; fine to keep
builder.ConfigureFunctionsWebApplication();

// ---- DI registrations ----
builder.Services.AddHttpClient();

// Email sender (Resend via plain HTTP)
builder.Services.AddSingleton<IEmailSender, ResendEmailSender>();

// Turnstile verifier
builder.Services.AddSingleton<ITurnstileVerifier, TurnstileVerifier>();

// Rate limiter backed by Azure Table Storage
var storageConn =
    builder.Configuration["AzureWebJobsStorage"]
    ?? throw new InvalidOperationException("AzureWebJobsStorage not configured");
builder.Services.AddSingleton(new TableServiceClient(storageConn));
builder.Services.AddSingleton<IRateLimiter, TableRateLimiter>();

// ---- run the host ----
builder.Build().Run();
