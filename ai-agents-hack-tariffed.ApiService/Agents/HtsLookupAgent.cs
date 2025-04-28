using ai_agents_hack_tariffed.ApiService.Data;
using Azure.AI.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class HtsLookupAgent(IAgentParameters parameters, TariffRateDb context) 
        : BaseAgent(parameters, context)
    {
        protected override string InstructionsFileName => "Instructions\\HtsLookupAgent.txt";
        protected override string Schema => "Hts";
    }
}
