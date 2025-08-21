using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;
using SubExplore.Services.Interfaces;

namespace SubExplore.DataAccess
{
    /// <summary>
    /// Database performance monitoring interceptor for tracking query execution times and performance
    /// </summary>
    public class PerformanceInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<PerformanceInterceptor> _logger;
        private readonly IPerformanceProfilingService _performanceService;
        private readonly Dictionary<DbCommand, (Stopwatch stopwatch, string sessionId)> _activeCommands = new();

        public PerformanceInterceptor(ILogger<PerformanceInterceptor> logger, IPerformanceProfilingService performanceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            StartProfiling(command, "Database-Query");
            return base.ReaderExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            StartProfiling(command, "Database-Query");
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            StopProfiling(command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            StopProfiling(command, eventData);
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            StartProfiling(command, "Database-NonQuery");
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            StartProfiling(command, "Database-NonQuery");
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            StopProfiling(command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            StopProfiling(command, eventData);
            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            StartProfiling(command, "Database-Scalar");
            return base.ScalarExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            StartProfiling(command, "Database-Scalar");
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            StopProfiling(command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            StopProfiling(command, eventData);
            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void StartProfiling(DbCommand command, string category)
        {
            try
            {
                var operationName = ExtractOperationName(command.CommandText);
                var sessionId = _performanceService.StartProfiling(operationName, category);
                
                var stopwatch = Stopwatch.StartNew();
                _activeCommands[command] = (stopwatch, sessionId);

                _logger.LogDebug("Started database operation: {Operation} - {SQL}", operationName, TruncateSQL(command.CommandText));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database profiling");
            }
        }

        private void StopProfiling(DbCommand command, CommandExecutedEventData eventData)
        {
            try
            {
                if (_activeCommands.TryGetValue(command, out var profilingData))
                {
                    profilingData.stopwatch.Stop();
                    
                    var additionalMetrics = new Dictionary<string, object>
                    {
                        { "ElapsedMs", eventData.Duration.TotalMilliseconds },
                        { "ParameterCount", command.Parameters.Count },
                        { "CommandType", command.CommandType.ToString() },
                        { "SQLLength", command.CommandText.Length }
                    };

                    // Add connection info if available
                    if (eventData.Connection != null)
                    {
                        additionalMetrics["ConnectionId"] = eventData.ConnectionId.ToString();
                        additionalMetrics["Database"] = eventData.Connection.Database ?? "Unknown";
                    }

                    _performanceService.StopProfiling(profilingData.sessionId, additionalMetrics);
                    _activeCommands.Remove(command);

                    // Log slow queries
                    var executionTime = eventData.Duration.TotalMilliseconds;
                    if (executionTime > 1000) // > 1 second
                    {
                        _logger.LogWarning(
                            "Slow database query detected: {ExecutionTime:F2}ms - {SQL}",
                            executionTime,
                            TruncateSQL(command.CommandText));
                    }
                    else if (executionTime > 100) // > 100ms
                    {
                        _logger.LogInformation(
                            "Database query: {ExecutionTime:F2}ms - {Operation}",
                            executionTime,
                            ExtractOperationName(command.CommandText));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping database profiling");
            }
        }

        private string ExtractOperationName(string sql)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sql))
                    return "Unknown";

                // Extract the operation type and main table
                var cleanSql = sql.Trim().ToUpper();
                
                if (cleanSql.StartsWith("SELECT"))
                {
                    return ExtractSelectOperation(sql);
                }
                else if (cleanSql.StartsWith("INSERT"))
                {
                    return ExtractInsertOperation(sql);
                }
                else if (cleanSql.StartsWith("UPDATE"))
                {
                    return ExtractUpdateOperation(sql);
                }
                else if (cleanSql.StartsWith("DELETE"))
                {
                    return ExtractDeleteOperation(sql);
                }
                else
                {
                    return cleanSql.Split(' ').FirstOrDefault() ?? "Unknown";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private string ExtractSelectOperation(string sql)
        {
            try
            {
                // Look for FROM clause to identify main table
                var fromIndex = sql.ToUpper().IndexOf(" FROM ");
                if (fromIndex > 0)
                {
                    var fromClause = sql.Substring(fromIndex + 6);
                    var tableName = fromClause.Split(' ', '\n', '\r', '\t').FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    
                    // Clean up table name (remove brackets, quotes)
                    tableName = tableName?.Trim('[', ']', '`', '"', '\'') ?? "Unknown";
                    
                    return $"SELECT-{tableName}";
                }
                return "SELECT";
            }
            catch
            {
                return "SELECT";
            }
        }

        private string ExtractInsertOperation(string sql)
        {
            try
            {
                var intoIndex = sql.ToUpper().IndexOf(" INTO ");
                if (intoIndex > 0)
                {
                    var intoClause = sql.Substring(intoIndex + 6);
                    var tableName = intoClause.Split(' ', '(', '\n', '\r', '\t').FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    tableName = tableName?.Trim('[', ']', '`', '"', '\'') ?? "Unknown";
                    return $"INSERT-{tableName}";
                }
                return "INSERT";
            }
            catch
            {
                return "INSERT";
            }
        }

        private string ExtractUpdateOperation(string sql)
        {
            try
            {
                var tokens = sql.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 1)
                {
                    var tableName = tokens[1].Trim('[', ']', '`', '"', '\'');
                    return $"UPDATE-{tableName}";
                }
                return "UPDATE";
            }
            catch
            {
                return "UPDATE";
            }
        }

        private string ExtractDeleteOperation(string sql)
        {
            try
            {
                var fromIndex = sql.ToUpper().IndexOf(" FROM ");
                if (fromIndex > 0)
                {
                    var fromClause = sql.Substring(fromIndex + 6);
                    var tableName = fromClause.Split(' ', '\n', '\r', '\t').FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    tableName = tableName?.Trim('[', ']', '`', '"', '\'') ?? "Unknown";
                    return $"DELETE-{tableName}";
                }
                return "DELETE";
            }
            catch
            {
                return "DELETE";
            }
        }

        private string TruncateSQL(string sql, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(sql))
                return "Empty SQL";

            // Clean up whitespace
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"\s+", " ").Trim();

            if (sql.Length <= maxLength)
                return sql;

            return sql.Substring(0, maxLength) + "...";
        }
    }
}