using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

namespace ai_agents_hack_tariffed.ApiService
{
    internal interface IScopedProcessingService
    {
        Task<bool> DoWork(CancellationToken stoppingToken);
    }

    internal class ScopedProcessingService : IScopedProcessingService
    {
        private int executionCount = 0;
        private readonly ILogger _logger;
        private readonly SqlConnection _connection;

        public ScopedProcessingService(ILogger<ScopedProcessingService> logger, SqlConnection connection)
        {
            _logger = logger;
            _connection = connection;
            _connection.ConnectionString = string.IsNullOrEmpty(_connection.ConnectionString)
                ? _connection.ConnectionString
                : _connection.ConnectionString += ";Command Timeout=0"; ;
        }

        /// <summary>
        /// Executes a task that processes SQL initialization logic while monitoring for cancellation
        /// requests.
        /// </summary>
        /// <remarks>This method performs SQL initialization by executing a stored procedure. It logs
        /// progress and errors during execution. The method will terminate if the <paramref name="cancellationToken"/>
        /// is triggered.</remarks>
        /// <param name="cancellationToken">A token that can be used to signal the cancellation of the operation. The method will stop processing if the
        /// token is canceled.</param>
        /// <returns>A task that represents the asynchronous operation.
        public async Task<bool> DoWork(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                executionCount++;

                _logger.LogInformation(
                    "Scoped Processing Service is working. Count: {Count}", executionCount);

                try
                {
                    if (_connection.State != System.Data.ConnectionState.Closed)
                    {
                        return true;
                    }

                    using (_connection)
                    {
                        if (string.IsNullOrEmpty(_connection.ConnectionString))
                        {
                            return true;
                        }

                        await _connection.OpenAsync(cancellationToken);

                        var sql = """
                          EXEC [dbo].[spSeedHts]
                          """;

                        using var command = new SqlCommand(sql, _connection);
                        await command.ExecuteNonQueryAsync(cancellationToken);

                        _logger.LogInformation("✅ SQL initialization complete.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing the SQL command.");
                    return true;
                }
            }
            return false;
        }
       
    }
}
