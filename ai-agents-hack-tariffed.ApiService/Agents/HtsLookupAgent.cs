using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class HtsLookupAgent(AIProjectClient client, string modelName, [FromServices] DbContext? context = null) 
        : BaseAgent(client, modelName, context)
    {
        protected override string AgentName => "HtsLookupAgent";
        protected override string InstructionsFileName => "Instructions\\HtsLookupAgent.txt";
    }

}
