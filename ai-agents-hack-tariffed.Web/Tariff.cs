using ai_agents_hack_tariffed.ApiService;
using ai_agents_hack_tariffed.ApiService.Data;
using Newtonsoft.Json;

namespace ai_agents_hack_tariffed.Web
{
    /// <summary>
    /// Represents the response from a primary producer API, including tariff rate details and the original API
    /// response.
    /// </summary>
    /// <remarks>This class provides access to the deserialized tariff rate information and the raw API
    /// response. If the API response message cannot be deserialized, a default instance
    /// is used.</remarks>
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

    /// <summary>
    /// Represents the response containing the percentage of trade data and the associated API response details.
    /// </summary>
    /// <remarks>This class provides access to the deserialized trade percentage record and the raw API
    /// response. If the provided API response is null or deserialization fails, default values are used.</remarks>
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

    /// <summary>
    /// Represents a record containing the percentage of a trade.
    /// </summary>
    /// <remarks>
    /// This class is used to store and retrieve the percentage value associated with a trade. The
    /// percentage is represented as a string 
    /// </remarks>
    public class PercentOfTradeRecord
    {
        public string PercentOfTrade { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response from a substitute API, including tariff rate details and the original API
    /// </summary>
    public class AggregateResponse
    {
        public PrimaryProducerApiResponse? PrimaryProducerApiResponse { get; set; }
        public ApiResponse? TariffRateResponse { get; set; }
        public PercentOfTradeResponse? PercentOfTradeResponse { get; set; }
        public ApiResponse? SubstituteResponse { get; set; }
    }
}
