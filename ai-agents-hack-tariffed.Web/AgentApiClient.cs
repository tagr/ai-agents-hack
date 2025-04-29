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
        public async Task<PrimaryProducerApiResponse?> GetPrimaryProducerAsync(string search)
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

        public async Task<ApiResponse?> GetTariffRateAsync(string search)
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

        public async Task<PercentOfTradeResponse?> GetPercentOfTradeAsync(string search)
        {
            var response = await httpClient.PostAsync($"/percent/{search}", null);

            var json = await response.Content.ReadAsStringAsync();
            await Console.Out.WriteLineAsync($"{json}");

            var result = JsonConvert.DeserializeObject<ApiResponse>(json);
            result ??= new ApiResponse();

            var returnValue = new PercentOfTradeResponse(result);
            return returnValue;
        }

        public async Task<ApiResponse?> GetSubstitutes(string search)
        {
            //if (!memoryCache.TryGetValue($"sub-{search}", out ApiResponse? value))
            //{
                var response = await httpClient.PostAsync($"/hts/{search}", null);

                var json = await response.Content.ReadAsStringAsync();
                await Console.Out.WriteLineAsync($"{json}");

                var result = JsonConvert.DeserializeObject<ApiResponse>(json);
                if (result == null)
                {
                    return new ApiResponse();
                }

            return result;

                //if (result.Success)
                //{
                //    var cacheEntryOptions = new MemoryCacheEntryOptions()
                //    .SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                //    memoryCache.Set($"sub-{search}", result, cacheEntryOptions);

                //    return result;
                //} 
            //}
            //else
            //{
            //    return value;
            //}

            //return new ApiResponse();
        }

        public async Task<AggregateResponse> Get(string search)
        {
            //if (!memoryCache.TryGetValue($"get-{search}", out AggregateResponse? value))
            //{
                var returnValue = new AggregateResponse
                {
                    PrimaryProducerApiResponse = await GetPrimaryProducerAsync(search),
                    
                };

                returnValue.SubstituteResponse = await GetSubstitutes(search);

                returnValue.PercentOfTradeResponse = await GetPercentOfTradeAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                returnValue.TariffRateResponse = await GetTariffRateAsync(
                    returnValue.PrimaryProducerApiResponse.TariffRate.Country);

                //var cacheEntryOptions = new MemoryCacheEntryOptions()
                //.SetSlidingExpiration(TimeSpan.FromSeconds(MemoryCacheExpirationInSeconds));

                //memoryCache.Set($"get-{search}", returnValue, cacheEntryOptions);

                 return returnValue;
            //}
            //else
            //{
            //    return value;
            //}
        }
    }
}
