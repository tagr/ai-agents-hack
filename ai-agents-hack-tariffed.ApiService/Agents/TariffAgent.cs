using ai_agents_hack_tariffed.ApiService.Data;

namespace ai_agents_hack_tariffed.ApiService.Agents
{
    public class TariffAgent(IAgentParameters parameters, TariffRateDb context) 
        : BaseAgent(parameters, context)
    {
    }
}
