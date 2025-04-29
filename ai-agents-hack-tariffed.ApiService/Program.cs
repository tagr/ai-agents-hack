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
using System.Text;
using System.Text.Json;

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

builder.Services.AddScoped<IAgentParameters>(x => 
    new AgentParameters(
        projectClient,
        apiDeploymentName));

builder.Services.AddScoped<PrimaryProducerAgent>();
//builder.Services.AddScoped<HtsLookupAgent>();
//builder.Services.AddScoped<TariffRateAgent>();
//builder.Services.AddScoped<SpecialAgent>();

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

#region Tools

var queryTool = new FunctionToolDefinition(
    name: nameof(HtsDatabaseTool.Query),
    description: "This function is used to get tariff rates and country information in the HTS database",
    parameters: BinaryData.FromObjectAsJson(new
    {
        Type = "object",
        Properties = new
        {
            Query = new
            {
                Type = "string",
                Description = "The input should be the country representing a major importer of goods and commodities. The query result will be returned as a JSON object."
            }
        },
        Required = new[] { "query" }
    },
    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
);

var queryAllTool = new FunctionToolDefinition(
    name: nameof(HtsDatabaseTool.QueryAll),
    description: "This function should query all trade agreements and HTS goods.",
    parameters: BinaryData.FromObjectAsJson(new
    {
        Type = "object",
        Properties = new
        {
            Query = new
            {
                Type = "string",
                Description = "The input should be a well-formed T-SQL query selecting all agreements and Harmonized Tariff Schedules. The query result will be returned as a JSON object.."
            }
        },
        Required = new[] { "query" }
    },
    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
);

ConnectionResponse bingConnection = await projectClient.GetConnectionsClient()
               .GetConnectionAsync(Environment.GetEnvironmentVariable("BING_GROUNDING_CONNECTION_NAME"));

var connectionId = bingConnection.Id;

ToolConnectionList connectionList = new()
{
    ConnectionList = { new ToolConnection(connectionId) }
};
BingGroundingToolDefinition bingGroundingTool = new(connectionList);

#endregion


//Returns the primary producing nation for a particular good.
app.MapPost("/producer/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    var response = new ApiResponse
    {
        Success = false
    };

    await ppAgent.RunAsync("PrimaryProducerAgent", "Instructions\\PrimaryProducerAgent.txt", [queryTool]);
    await ppAgent.GetResponseAsync(search);

    var output = ppAgent.OutputBuilder.ToString();

    var tr = await db.TariffRates.FirstOrDefaultAsync(t => t.Country == output);

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
    var response = new ApiResponse
    {
        Success = false
    };

    var value = await db.TariffRates.FirstOrDefaultAsync(t => t.Country == search);

    response.Message = value?.PreviousRate.ToString() ?? string.Empty;

    //await ppAgent.RunAsync("TariffAgent", "Instructions\\TariffRateAgent.txt", [queryTool]);

    //var prompt = $"Return the tariff rate for {search}?";
    //var response = await ppAgent.GetResponseAsync(prompt, ".");

    //if (response.Message.Contains("Rate limit", StringComparison.InvariantCultureIgnoreCase))
    //{
    //    response.Error = response.Message;
    //    response.Message = string.Empty;
    //}
    //else
    //{
    //    response.Success = true;
    //}

    await ppAgent.DisposeAsync();
    response.Success = true;
    return response;
});

//Queries a nation's percent of overall trade with the United States.
app.MapPost("/percent/{search}", async ([FromRoute] string search, TariffRateDb db) =>
{
    await ppAgent.RunAsync("SpecialAgent", "Instructions\\SpecialAgent.txt", []);

    var prompt = $"Create a query to return the percent of trade for {search}.";
    await ppAgent.GetResponseAsync(prompt);

    var output = ppAgent.OutputBuilder.ToString();

    var response = new ApiResponse
    {
        Success = false
    };

    try
    {
        var queryResult = await HtsDatabaseTool.Query(output, db);
        response.Message = queryResult;
    }
    catch
    {
        response.Error = "Error: Invalid query.";
        response.Message = string.Empty;
    }

    await ppAgent.DisposeAsync();
    response.Success = true;
    return response;
});

//Returns the best matching Harmonized tariff schedule number for a particular good.
app.MapPost("/hts/{search}", async ([FromRoute] string search) =>
{
    await ppAgent.RunAsync("HtsAgent", "Instructions\\HtsLookupAgent.txt", [queryAllTool, bingGroundingTool], "'Hts'");

    var prompt = $"Find substitute goods for {search} produced in the US.";
    await ppAgent.GetResponseAsync(prompt);

    var output = ppAgent.OutputBuilder.ToString();


    var response = new ApiResponse
    {
        Message = output,
        Success = output.Length > 0
    };

    await ppAgent.DisposeAsync();
    return response;
});

app.MapDefaultEndpoints();

await app.RunAsync();