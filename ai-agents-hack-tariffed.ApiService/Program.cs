using ai_agents_hack_tariffed.ApiService;
using ai_agents_hack_tariffed.ApiService.Agents;
using ai_agents_hack_tariffed.ApiService.Data;
using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Azure.Identity;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;

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


string apiDeploymentName = builder.Configuration["Azure:ModelName"] ?? throw new InvalidOperationException("Azure:ModelName is not set in the configuration.");
string projectConnectionString = builder.Configuration["Azure:AiAgentService"] ?? throw new InvalidOperationException("Azure:AiAgentService is not set in the configuration.");
AIProjectClient projectClient = new(projectConnectionString, new AzureCliCredential());

builder.Services.AddSingleton<IAgentParameters>(x => 
    new AgentParameters(
        projectClient,
        apiDeploymentName));

builder.Services.AddScoped<PrimaryProducerAgent>();
builder.Services.AddScoped<HtsLookupAgent>();
builder.Services.AddScoped<TariffRateAgent>();
builder.Services.AddScoped<SpecialAgent>();

var app = builder.Build();

//Initialize agents

using var scope = app.Services.CreateScope();
//var tariffDbContext = scope.ServiceProvider.GetRequiredService<TariffRateDb>();
//await using PrimaryProducerAgent ppAgent = app.Services.GetKeyedService<PrimaryProducerAgent>("primaryProducerAgent") //new(projectClient, apiDeploymentName);
//await using HtsLookupAgent htsAgent = new(projectClient, apiDeploymentName);
//await using TariffRateAgent tAgent = new(projectClient, apiDeploymentName);
//await using SpecialAgent sAgent = new(projectClient, apiDeploymentName);

var ppAgent = scope.ServiceProvider.GetRequiredService<PrimaryProducerAgent>();
//var htsAgent = scope.ServiceProvider.GetRequiredService<HtsLookupAgent>();
//var tAgent = scope.ServiceProvider.GetRequiredService<TariffRateAgent>();
//var sAgent = scope.ServiceProvider.GetRequiredService<SpecialAgent>();


//if (htsAgent != null) await htsAgent.RunAsync();
//if (tAgent != null) await tAgent.RunAsync();
//if (sAgent != null) await sAgent.RunAsync();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Returns the primary producing nation for a particular good.
app.MapPost("/producer/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    var response = new ApiResponse
    {
        Success = false
    };

    await ppAgent.RunAsync("PrimaryProducerAgent", "Instructions\\PrimaryProducerAgent.txt");

    response = await ppAgent.GetResponseAsync(search);
    var tr = await db.TariffRates.FirstOrDefaultAsync(t => t.Country == response.Message);

    var returnValue = (tr == null) ? response : new ApiResponse
    {
        Message = JsonConvert.SerializeObject(tr),
        Error = string.Empty,
        Success = true
    };

    await ppAgent.DisposeAsync();
    return returnValue;

});

//Returns the tariff rate given a producing country. Should use tools to query the database.
app.MapPost("/tariff/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    await ppAgent.RunAsync("TariffAgent", "Instructions\\TariffRateAgent.txt");

    var prompt = $"Use your tools to query [TariffRate] table and return the applicable rate for country: {search}?";
    var response = await ppAgent.GetResponseAsync(prompt, ".");

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        response.Error = response.Message;
        response.Message = string.Empty;
    }
    else
    {
        response.Success = true;
    }

        
    await ppAgent.DisposeAsync();
    return response;
});

//Returns tariff agreements for a specified country. Should use tools to query the database.
app.MapPost("/special/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    await ppAgent.RunAsync("SpecialAgent", "Instructions\\SpecialAgent.txt");

    var prompt = $"Use your tools to query [Special] table and return top trade agreements for country: {search}?";
    var response = await ppAgent.GetResponseAsync(prompt);

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        response.Error = response.Message;
        response.Message = string.Empty;
    }
    else
    {
        response.Success = true;
    }

    await ppAgent.DisposeAsync();
    return response;
});

//Returns the best matching Harmonized tariff schedule number for a particular good.
app.MapPost("/hts/{search}", async ([FromRoute] string search) =>
{
    await ppAgent.RunAsync("HtsAgent", "Instructions\\HtsLookupAgent.txt");

    var prompt = $"Find substitute goods for {search} produced in the US.";
    var response = await ppAgent.GetResponseAsync(prompt);

    if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    {
        response.Error = response.Message;
        response.Message = string.Empty;
    }
    else
    {
        response.Success = true;
    }

    await ppAgent.DisposeAsync();
    return response;
});

app.MapDefaultEndpoints();

await app.RunAsync();