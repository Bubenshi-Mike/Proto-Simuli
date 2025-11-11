namespace Infrastructure.Services;

public class LoggingService : ILoggingService
{
    private readonly List<LogEntryDto> _logs = new List<LogEntryDto>();
    private readonly object _lockObj = new object();

    public void Log(string routerName, LogEventType eventType, string message, object additionalData = null)
    {
        lock (_lockObj)
        {
            var logEntry = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                RouterName = routerName,
                EventType = eventType,
                Message = message,
                AdditionalData = additionalData
            };

            _logs.Add(logEntry);

            // Console output for real-time monitoring
            Console.WriteLine($"[{logEntry.Timestamp:HH:mm:ss.fff}] [{routerName}] {eventType}: {message}");
        }
    }

    public List<LogEntryDto> GetLogs(string routerName = null, DateTime? since = null)
    {
        lock (_lockObj)
        {
            var query = _logs.AsEnumerable();

            if (!string.IsNullOrEmpty(routerName))
                query = query.Where(l => l.RouterName == routerName);

            if (since.HasValue)
                query = query.Where(l => l.Timestamp >= since.Value);

            return query.OrderBy(l => l.Timestamp).ToList();
        }
    }

    public void ClearLogs()
    {
        lock (_lockObj)
        {
            _logs.Clear();
        }
    }
}
