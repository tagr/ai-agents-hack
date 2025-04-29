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

    public class PercentOfTradeResponse
    {
        public PercentOfTradeRecord Record { get; } = new();
        public readonly ApiResponse ApiResponse;
        public PercentOfTradeResponse(ApiResponse api)
        {

            ApiResponse = api ?? new ApiResponse();

            if (api == null)
            {
                return;
            }

            try
            {
                Record = JsonConvert.DeserializeObject<PercentOfTradeRecord>(api.Message) ?? new PercentOfTradeRecord();
            }
            catch
            {
                Record = new PercentOfTradeRecord();
            }
        }
    }
    //{"PercentOfTrade":"13.00%"}
    public class PercentOfTradeRecord
    {
        public string PercentOfTrade { get; set; } = string.Empty;
    }

    public class AggregateResponse
    {
        public PrimaryProducerApiResponse? PrimaryProducerApiResponse { get; set; }
        public ApiResponse? TariffRateResponse { get; set; }
        public PercentOfTradeResponse? PercentOfTradeResponse { get; set; }
        public ApiResponse? SubstituteResponse { get; set; }
    }
}
