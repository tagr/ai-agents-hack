using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class PrimaryProducerAgent(AIProjectClient client, string modelName, [FromServices] DbContext? context = null) 
        : BaseAgent(client, modelName, context)
    {
        protected override string AgentName => "PrimaryProducerAgent";
        protected override string InstructionsFileName => "Instructions\\PrimaryProducerAgent.txt";
    }
}
