using Azure.AI.Projects;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class HtsLookupAgent(AIProjectClient client, string modelName) 
        : BaseAgent(client, modelName)
    {
        protected override string AgentName => $"HtsLookupAgent-{DateTime.UtcNow.ToString("yyyy-hhmmss")}";
        protected override string InstructionsFileName => "Instructions\\HtsLookupAgent.txt";
        protected override string Schema => "Hts";
    }

}
