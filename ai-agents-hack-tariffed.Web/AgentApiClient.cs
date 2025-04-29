using ai_agents_hack_tariffed.ApiService;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ai_agents_hack_tariffed.Web
{
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
