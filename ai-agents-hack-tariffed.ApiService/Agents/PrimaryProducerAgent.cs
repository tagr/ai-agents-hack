using Azure.AI.Projects;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class PrimaryProducerAgent(AIProjectClient client, string modelName) 
        : BaseAgent(client, modelName)
    {
        protected override string AgentName => $"PrimaryProducerAgent-{DateTime.UtcNow.ToString("yyyy-hhmmss")}";
        protected override string InstructionsFileName => "Instructions\\PrimaryProducerAgent.txt";
        protected override string Schema => "TariffRate";
    }
}
