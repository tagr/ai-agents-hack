using Azure.AI.Projects;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class TariffRateAgent(AIProjectClient client, string modelName) 
        : BaseAgent(client, modelName)
    {
        protected override string AgentName => $"TariffRateAgent-{DateTime.UtcNow.ToString("yyyy-hhmmss")}";
        protected override string InstructionsFileName => "Instructions\\TariffRateAgent.txt";
        protected override string Schema => "TariffRate";
    }
}
