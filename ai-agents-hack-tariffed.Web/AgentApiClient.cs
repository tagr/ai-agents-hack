using ai_agents_hack_tariffed.ApiService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace ai_agents_hack_tariffed.Web
{
    /// <summary>
    /// Provides methods for retrieving aggregated data from various APIs, including primary producer, tariff rate,
    /// percent of trade, and substitutes, with caching support to optimize performance.
    /// </summary>
    /// <remarks>This client is designed to interact with multiple APIs to fetch and aggregate data based on a
    /// search term. The results are cached to improve performance and reduce redundant API calls. The caching duration
    /// is determined by a predefined expiration time.</remarks>
    /// <param name="httpClient"></param>
    /// <param name="memoryCache"></param>
    public class AgentApiClient(HttpClient httpClient, [FromServices] IMemoryCache memoryCache)
    {
        private const uint MemoryCacheExpirationInSeconds = uint.MaxValue;
        private async Task<PrimaryProducerApiResponse?> GetPrimaryProducerAsync(string search)
        {
            var response = await httpClient.PostAsync($"/producer/{search}", null);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(json);

            if (result == null)
            {
                return new PrimaryProducerApiResponse(new ApiResponse());
            }

            var returnValue = new PrimaryProducerApiResponse(result);

            return returnValue;
        }

        /// <summary>
        /// Retrieves the tariff rate information for the specified search term by making an asynchronous HTTP POST
        /// request.
        /// </summary>
        /// <remarks>The method sends a POST request to the endpoint `/tariff/{search}` and processes the
        /// response. If the response contains an error message or cannot be deserialized into an <see
        /// cref="ApiResponse"/> object, the returned <see cref="ApiResponse"/> will indicate failure with an
        /// appropriate error message.</remarks>
        /// <param name="search">The search term used to query the tariff rate. This value cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an object with the tariff rate information if the operation is successful, or an error message if the operation
        /// fails.</returns>
        private async Task<ApiResponse?> GetTariffRateAsync(string search)
        {
            var response = await httpClient.PostAsync($"/tariff/{search}", null);

            var json = await response.Content.ReadAsStringAsync();

            if (json.Contains("Error:", StringComparison.InvariantCultureIgnoreCase))
            {
                return new ApiResponse
                {
                    Message = string.Empty,
                    Error = json,
                    Success = false
                };
            }

            var result = JsonConvert.DeserializeObject<ApiResponse>(json);
            if (result == null)
            {
                return new ApiResponse
                {
                    Message = string.Empty,
                    Error = "Error: Tariff Rate response was empty.",
                    Success = false
                };
            }

            return result;
        }

        /// <summary>
        /// Retrieves the percentage of trade data for the specified search term asynchronously.
        /// </summary>
        /// <remarks>
        /// This method sends a POST request to the endpoint with the specified search term and
        /// processes the response. The response is expected to be in JSON format and is deserialized
        /// </remarks>
        /// <param name="search">The search term used to query the trade percentage data. Cannot be null or empty.</param>
        /// <returns>Object containing the trade percentage data,  or <see
        /// langword="null"/> if the response is invalid or deserialization fails.</returns>
        private async Task<PercentOfTradeResponse?> GetPercentOfTradeAsync(string search)
        {
            var response = await httpClient.PostAsync($"/percent/{search}", null);

            var json = await response.Content.ReadAsStringAsync();
            await Console.Out.WriteLineAsync($"{json}");

            var result = JsonConvert.DeserializeObject<ApiResponse>(json);
            result ??= new ApiResponse();

            var returnValue = new PercentOfTradeResponse(result);
            return returnValue;
        }

        /// <summary>
        /// Retrieves substitute goods by performing a POST request to the specified endpoint.
        /// </summary>
        /// <remarks>
        /// This method sends a POST request to the endpoint constructed using the provided
        /// term. The response is expected to be in JSON format and is deserialized into an
        /// ApiResponse object.
        /// </remarks>
        /// <param name="search">The search term used to query the substitutes. This value cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an 
        /// object with the substitute data. If the response cannot be deserialized, an empty ApiResponse
        /// is returned.</returns>
        private async Task<ApiResponse?> GetSubstitutes(string search)
        {
            var response = await httpClient.PostAsync($"/hts/{search}", null);

            var json = await response.Content.ReadAsStringAsync();
            await Console.Out.WriteLineAsync($"{json}");

            var result = JsonConvert.DeserializeObject<ApiResponse>(json);
            if (result == null)
            {
                return new ApiResponse();
            }

            return result;
        }

        /// <summary>
        /// Retrieves an aggregated response based on the specified search term.
        /// </summary>
        /// <param name="search">The search term used to query the data sources. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an object with data from the primary producer, substitutes, percent of trade, and
        /// tariff rate. If the data is already cached, the cached response is returned.</returns>
        public async Task<AggregateResponse> Get(string search)
        {
            if (!memoryCache.TryGetValue($"{search}", out AggregateResponse? value))
            {
                var returnValue = new AggregateResponse
            {
                PrimaryProducerApiResponse = await GetPrimaryProducerAsync(search),
                SubstituteResponse = await GetSubstitutes(search)
            };

                    returnValue.PercentOfTradeResponse = await GetPercentOfTradeAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                returnValue.TariffRateResponse = await GetTariffRateAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                if (returnValue.PrimaryProducerApiResponse != null &&
                    returnValue.SubstituteResponse != null &&
                    returnValue.PercentOfTradeResponse != null &&
                    returnValue.TariffRateResponse != null &&
                    returnValue.PrimaryProducerApiResponse.ApiResponse.Success &&
                    returnValue.SubstituteResponse.Success &&
                    returnValue.PercentOfTradeResponse.ApiResponse.Success &&
                    returnValue.TariffRateResponse.Success
                    )
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                    memoryCache.Set($"{search}", returnValue, cacheEntryOptions);
                }
                
                return returnValue;
            }
            else
            {
                return value;
            }
        }
    }
}
