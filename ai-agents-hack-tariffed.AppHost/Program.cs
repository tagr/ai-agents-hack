using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var initScriptPath = Path.Join(Path.GetDirectoryName(typeof(Program).Assembly.Location), "init.sql");
var db = sql.AddDatabase("ai-agent-hackathon")
    .WithCreationScript(await File.ReadAllTextAsync(initScriptPath));

var apiService = builder.AddProject<Projects.ai_agents_hack_tariffed_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.ai_agents_hack_tariffed_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();