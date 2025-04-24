using ai_agents_hack_tariffed.ApiService.Tools;
using Azure.AI.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class PrimaryProducerAgent(AIProjectClient client, string modelName) 
        : BaseAgent(client, modelName)
    {
        protected override string AgentName => "PrimaryProducerAgent";
        protected override string InstructionsFileName => "Instructions\\PrimaryProducerAgent.txt";
    }
}
