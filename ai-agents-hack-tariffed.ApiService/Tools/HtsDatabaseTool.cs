using ai_agents_hack_tariffed.ApiService.Data;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.Text.Json;

namespace ai_agents_hack_tariffed.ApiService.Tools
{
    public static class HtsDatabaseTool
    {
        /// <summary>
        /// This function gets the tariff rate information for a given country provided in a containerized SQL Server database.
        /// </summary>
        public static async Task<string> GetCountryTariffRateInfo(string country, DbContext context)
        {
            Utils.LogBlue($"Function Call Tools: {nameof(GetCountryTariffRateInfo)}");
            Utils.LogBlue($"TOO: {nameof(GetCountryTariffRateInfo)} executing query: {country}");

            var returnValue = await ((TariffRateDb)context).TariffRates.FirstOrDefaultAsync(t => t.Country == country);

            if ( returnValue == null ) { return string.Empty; }

            return returnValue.PreviousRate;
        }
    }
}
