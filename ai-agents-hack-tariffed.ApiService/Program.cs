using ai_agents_hack_tariffed.ApiService;
using ai_agents_hack_tariffed.ApiService.Agents;
using ai_agents_hack_tariffed.ApiService.Data;
using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.SqlServer;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddSqlServerClient(connectionName: "ai-agent-hackathon");

builder.AddSqlServerDbContext<TariffRateDb>(connectionName: "ai-agent-hackathon");

// Database migration and seeding
builder.Services.AddHostedService<ConsumeScopedServiceHostedService>();
builder.Services.AddScoped<IScopedProcessingService, ScopedProcessingService>();

var app = builder.Build();

string apiDeploymentName = app.Configuration["Azure:ModelName"] ?? throw new InvalidOperationException("Azure:ModelName is not set in the configuration.");
string projectConnectionString = app.Configuration["Azure:AiAgentService"] ?? throw new InvalidOperationException("Azure:AiAgentService is not set in the configuration.");

AIProjectClient projectClient = new(projectConnectionString, new AzureCliCredential());

//Initialize agents
using var scope = app.Services.CreateScope();
var tariffDbContext = scope.ServiceProvider.GetRequiredService<TariffRateDb>();

await using PrimaryProducerAgent ppAgent = new(projectClient, apiDeploymentName);
await using HtsLookupAgent htsAgent = new(projectClient, apiDeploymentName);
await using TariffRateAgent tAgent = new(projectClient, apiDeploymentName);
await using SpecialAgent sAgent = new(projectClient, apiDeploymentName);

await ppAgent.RunAsync(tariffDbContext);
await htsAgent.RunAsync(tariffDbContext);
await tAgent.RunAsync(tariffDbContext);
await sAgent.RunAsync(tariffDbContext);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Returns the primary producing nation for a particular good.
app.MapPost("/producer/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    var response = await ppAgent.GetResponseAsync(search);
    var tr = db.TariffRates.FirstOrDefault(t => t.Country == response.Message);

    return (tr == null) ? response : new ApiResponse
    {
        Message = JsonConvert.SerializeObject(tr),
        Error = string.Empty,
        Success = true
    };

});

//Returns the tariff rate given a producing country. Should use tools to query the database.
app.MapPost("/tariff/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    var prompt = $"What is the tariff rate for {search}?";
    var response = await tAgent.GetResponseAsync(prompt, ".");

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        return new ApiResponse
        {
            Message = string.Empty,
            Error = response.Message,
            Success = false
        };
    }

    return response;
});

//Returns tariff agreements for a specified country. Should use tools to query the database.
app.MapPost("/special/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    var prompt = $"List all applicable trade agreements for {search}?";
    var response = await sAgent.GetResponseAsync(prompt);

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        return new ApiResponse
        {
            Message = string.Empty,
            Error = response.Message,
            Success = false
        };
    }

    return response;
});

//Returns the best matching Harmonized tariff schedule number for a particular good.
app.MapPost("/hts/{search}", async ([FromRoute] string search) =>
{
    var prompt = $"Find substitute goods for {search} produced in the US.";
    var response = await htsAgent.GetResponseAsync(prompt);

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        return new ApiResponse
        {
            Message = string.Empty,
            Error = response.Message,
            Success = false
        };
    }

    return response;
});

app.MapDefaultEndpoints();

app.Run();