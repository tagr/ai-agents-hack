using ai_agents_hack_tariffed.ApiService;
using ai_agents_hack_tariffed.ApiService.Data;
using Newtonsoft.Json;

namespace ai_agents_hack_tariffed.Web
{
    public class PrimaryProducerApiResponse
    {
        public TariffRate TariffRate { get; } = new();
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

    public class AggregateResponse
    {
        public PrimaryProducerApiResponse? PrimaryProducerApiResponse { get; set; }
        public ApiResponse? TariffRateResponse { get; set; }
        public ApiResponse? SpecialResponse { get; set; }
        public ApiResponse? SubstituteResponse { get; set; }
    }
}
