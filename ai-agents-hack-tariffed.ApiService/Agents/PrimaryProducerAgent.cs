using ai_agents_hack_tariffed.ApiService.Data;
using Azure.AI.Projects;
using Microsoft.EntityFrameworkCore;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class PrimaryProducerAgent(IAgentParameters parameters, TariffRateDb context) 
        : BaseAgent(parameters, context)
    {
    }
}
