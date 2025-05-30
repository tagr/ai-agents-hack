﻿using ai_agents_hack_tariffed.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Dynamic;

namespace ai_agents_hack_tariffed.ApiService.Tools
{
    public static class HtsDatabaseTool
    {
        /// <summary>
        /// Retrieves the database schema information for the specified entities in JSON format.
        /// </summary>
        /// <remarks>This method queries the database's information schema to retrieve metadata about the
        /// columns  of the specified tables. The result is returned as a JSON string with a root element named
        /// "Schema".</remarks>
        /// <param name="entities">A comma-separated list of table names for which schema information is requested.</param>
        /// <param name="context">The <see cref="DbContext"/> used to execute the database query.</param>
        /// <returns>A JSON string representing the schema information of the specified entities.  The JSON structure includes
        /// details about the columns of the tables.</returns>
        public static async Task<string> GetTariffRateDbSchema(string entities, DbContext context)
        {
            var results = new List<dynamic>();

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = @$"
              SELECT * FROM information_schema.columns 
              WHERE TABLE_NAME IN ({entities})
              FOR JSON PATH, ROOT('Schema')
            ";
            command.CommandType = CommandType.Text;

            await context.Database.OpenConnectionAsync();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new ExpandoObject() as IDictionary<string, object>;

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.GetValue(i);
                    row[name] = await reader.IsDBNullAsync(i) ? null : value;
                }

                results.Add(row);
            }

            var returnValue = JsonConvert.SerializeObject(results);
            return returnValue;
        }

        public static async Task<string> QueryAll(string query, DbContext context) => await Query(query, context, false);

        /// <summary>
        /// This function queries the database and returns the selected records as a JSON string.
        /// By default, it returns the first row of the result set. Set GetFirstRow to false to return all rows.
        /// </summary>
        public static async Task<string> Query(string query, DbContext context, bool GetFirstRow = true)
        {
            string returnValue = string.Empty;
            Utils.LogBlue($"Function Call Tools: {nameof(Query)}");
            Utils.LogBlue($"TOOL: {nameof(Query)} executing query: {query}");

            var results = new List<dynamic>();

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            await context.Database.OpenConnectionAsync();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new ExpandoObject() as IDictionary<string, object>;

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? string.Empty : reader.GetValue(i);
                }

                results.Add(row);
            }

            if (GetFirstRow)
            {
                if (results.Count > 0)
                {
                    returnValue = JsonConvert.SerializeObject(results[0]);
                }
            }
            else
            {
                returnValue = JsonConvert.SerializeObject(results);
            }

            return returnValue;
        }
    }
}
