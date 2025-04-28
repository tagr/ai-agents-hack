using ai_agents_hack_tariffed.ApiService.Data;
using Azure.AI.Projects;
using Microsoft.EntityFrameworkCore;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class SpecialAgent(IAgentParameters parameters, TariffRateDb context) 
        : BaseAgent(parameters, context)
    {
        protected override string InstructionsFileName => "Instructions\\SpecialAgent.txt";
        protected override string Schema => "Special";
    }
}
