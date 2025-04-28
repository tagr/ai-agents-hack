using Azure.AI.Projects;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class SpecialAgent(AIProjectClient client, string modelName) 
        : BaseAgent(client, modelName)
    {
        protected override string AgentName => $"SpecialAgent-{DateTime.UtcNow.ToString("yyyy-hhmmss")}";
        protected override string InstructionsFileName => "Instructions\\SpecialAgent.txt";
        protected override string Schema => "Special";
    }
}
