using ai_agents_hack_tariffed.ApiService;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ai_agents_hack_tariffed.Web
{
    public class AgentApiClient(HttpClient httpClient, [FromServices] IMemoryCache memoryCache)
    {
        private const uint MemoryCacheExpirationInSeconds = uint.MaxValue;
        public async Task<PrimaryProducerApiResponse?> GetPrimaryProducerAsync(string search)
        {
            if (!memoryCache.TryGetValue($"producer-{search}", out PrimaryProducerApiResponse? value))
            {
                var response = await httpClient.PostAsync($"/producer/{search}", null);

                var json = await response.Content.ReadAsStringAsync();
                await Console.Out.WriteLineAsync($"{json}");
                var result = JsonConvert.DeserializeObject<ApiResponse>(json);

                if (result == null)
                {
                    return new PrimaryProducerApiResponse(new ApiResponse());
                }

                if (result.Success)
                {
                    value = new PrimaryProducerApiResponse(result);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                    memoryCache.Set($"producer-{search}", value, cacheEntryOptions);

                    return value;
                }
            }
            else
            {
                return value;
            }

            return new PrimaryProducerApiResponse(new ApiResponse());
        }

        public async Task<ApiResponse?> GetTariffRateAsync(string search)
        {
            if (!memoryCache.TryGetValue($"tariff-{search}", out ApiResponse? value))
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
                if (result.Success)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                    memoryCache.Set($"tariff-{search}", result, cacheEntryOptions);
                }

                return result;
            }
            else
            {
                return value;
            }
        }

        public async Task<ApiResponse?> GetSpecialTradeAgreementsAsync(string search)
        {
            if (!memoryCache.TryGetValue($"special-{search}", out ApiResponse? value))
            {

                var response = await httpClient.PostAsync($"/special/{search}", null);

                var json = await response.Content.ReadAsStringAsync();
                await Console.Out.WriteLineAsync($"{json}");

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
                    return new ApiResponse();
                }
                if (result.Success)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(5));

                    memoryCache.Set($"special-{search}", result, cacheEntryOptions);

                    return result;
                } 
            }
            else
            {
                return value;
            }

            return new ApiResponse();
        }

        public async Task<ApiResponse?> GetSubstitutes(string search)
        {
            if (!memoryCache.TryGetValue($"sub-{search}", out ApiResponse? value))
            {
                var response = await httpClient.PostAsync($"/hts/{search}", null);

                var json = await response.Content.ReadAsStringAsync();
                await Console.Out.WriteLineAsync($"{json}");

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
                    return new ApiResponse();
                }
                if (result.Success)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                    memoryCache.Set($"sub-{search}", result, cacheEntryOptions);

                    return result;
                } 
            }
            else
            {
                return value;
            }

            return new ApiResponse();
        }

        public async Task<AggregateResponse> Get(string search)
        {
            if (!memoryCache.TryGetValue($"get-{search}", out AggregateResponse? value))
            {
                var returnValue = new AggregateResponse
                {
                    PrimaryProducerApiResponse = await GetPrimaryProducerAsync(search),
                    
                };

                await Task.Delay(5000);

                returnValue.SubstituteResponse = await GetSubstitutes(search);

                await Task.Delay(5000);

                returnValue.SpecialResponse = await GetSpecialTradeAgreementsAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                await Task.Delay(5000);

                returnValue.TariffRateResponse = await GetTariffRateAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                memoryCache.Set($"get-{search}", returnValue, cacheEntryOptions);

                 return returnValue;
            }
            else
            {
                return value;
            }
        }
    }
}
