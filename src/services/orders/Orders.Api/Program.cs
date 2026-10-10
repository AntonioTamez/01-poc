using Orders.Api.Endpoints;
using Orders.Application;
using Orders.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddOrdersApplication();
builder.Services.AddOrdersInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Local development only. Schema migrations are a follow-up.
    await app.Services.EnsureOrdersDatabaseCreatedAsync();
}

app.MapDefaultEndpoints();

app.MapGroup("/orders")
    .WithTags("Orders")
    .MapPlaceOrder()
    .MapGetOrderById();

await app.RunAsync();

/// <summary>Entry point. Exposed so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
