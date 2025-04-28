using ai_agents_hack_tariffed.ApiService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ai_agents_hack_tariffed.Web
{
    public class AgentApiClient(HttpClient httpClient, [FromServices] IMemoryCache memoryCache)
    {
        private const uint MemoryCacheExpirationInSeconds = 30;
        public async Task<PrimaryProducerApiResponse?> GetPrimaryProducerAsync(string search)
        {
            if (!memoryCache.TryGetValue(search, out PrimaryProducerApiResponse? value))
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

                    memoryCache.Set(search, value, cacheEntryOptions);

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
            if (!memoryCache.TryGetValue(search, out ApiResponse? value))
            {

                var response = await httpClient.PostAsync($"/tariff/{search}", null);

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

                    memoryCache.Set(search, result, cacheEntryOptions);

                    return result;
                }
            }
            else
            {
                return value;
            }
            
            return new ApiResponse();
        }

        public async Task<ApiResponse?> GetSpecialTradeAgreementsAsync(string search)
        {
            if (!memoryCache.TryGetValue(search, out ApiResponse? value))
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

                    memoryCache.Set(search, result, cacheEntryOptions);

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
            if (!memoryCache.TryGetValue(search, out ApiResponse? value))
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
                    ////convert [text](url) to hyperlinks
                    //string pattern = @"\[(.*?)\]\((.*?)\)";
                    //string replacement = "<a href=\"$2\">$1</a>";

                    //result.Message = Regex.Replace(result.Message, pattern, replacement);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                    memoryCache.Set(search, result, cacheEntryOptions);

                    return result;
                } 
            }
            else
            {
                return value;
            }

            return new ApiResponse();
        }
    }
}
