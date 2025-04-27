using ai_agents_hack_tariffed.ApiService;
using ai_agents_hack_tariffed.ApiService.Data;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ai_agents_hack_tariffed.Web
{
    public class PrimaryProducerApiResponse
    {
        public readonly TariffRate TariffRate;
        public readonly ApiResponse ApiResponse;
        public PrimaryProducerApiResponse(ApiResponse api)
        {

            ApiResponse = api ?? new ApiResponse();

            if (api == null)
            {
                return;
            }

            try
            {
                TariffRate = JsonConvert.DeserializeObject<TariffRate>(api.Message) ?? new TariffRate();
            }
            catch
            {
                TariffRate = new TariffRate();
            }
        }
    }
}
