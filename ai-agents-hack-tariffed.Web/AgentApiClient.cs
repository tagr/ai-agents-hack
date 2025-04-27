using ai_agents_hack_tariffed.ApiService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text.Json;

namespace ai_agents_hack_tariffed.Web
{
    public class AgentApiClient(HttpClient httpClient, [FromServices] IMemoryCache memoryCache)
    {
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
                    return new PrimaryProducerApiResponse(result);
                }

                value = new PrimaryProducerApiResponse(result);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(uint.MaxValue));

                memoryCache.Set(search, value, cacheEntryOptions);
            }
            else
            {
                return value;
            }

            return new PrimaryProducerApiResponse(new ApiResponse());
        }
    }
}
