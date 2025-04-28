using Azure.AI.Projects;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public interface IAgentParameters
    {
        AIProjectClient ProjectClient { get; }
        string ApiDeploymentName { get; }
    }

    public class AgentParameters(AIProjectClient client, string ApiDeploymentName) : IAgentParameters
    {
        public AIProjectClient ProjectClient { get; } = client;
        public string ApiDeploymentName { get; } = ApiDeploymentName;
    }
}
