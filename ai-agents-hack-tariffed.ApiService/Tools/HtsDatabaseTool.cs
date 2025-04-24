using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using System.Text.Json;

namespace ai_agents_hack_tariffed.ApiService.Tools
{
    public class HtsDatabaseTool
    {
        private readonly SqlConnection _connection;

        public HtsDatabaseTool([FromServices] SqlConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        /// <summary>
        /// This function gets the tariff rate information for a given country provided in a containerized SQL Server database.
        /// </summary>
        public async Task<string> GetCountryTariffRateInfo(string country)
        {
            Utils.LogBlue($"Function Call Tools: {nameof(GetCountryTariffRateInfo)}");
            Utils.LogBlue($"TOO: {nameof(GetCountryTariffRateInfo)} executing query: {country}");

            await _connection.OpenAsync();
            string query = "SELECT TOP 1 * FROM [TariffRate] WHERE Country = @country";

            try
            {
                using var cmd = new SqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@country", country);

                using var reader = await cmd.ExecuteReaderAsync();

                var dataTable = new DataTable();
                dataTable.Load(reader);

                if (dataTable.Rows.Count == 0)
                {
                    return "The query returned no results. Try a different question.";
                }

                Dictionary<string, List<object>> data = [];

                foreach (DataColumn column in dataTable.Columns)
                {
                    data[column.ColumnName] = dataTable.Rows.Cast<DataRow>().Select(row => row[column.ColumnName]).ToList();
                }

                var returnValue = JsonConvert.SerializeObject(data);

                return returnValue;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message, Query = query });
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
    }
}
