namespace Application.Common.Interfaces;

public interface ILoggingService
{
    void Log(string routerName, LogEventType eventType, string message, object additionalData = null);
    List<LogEntryDto> GetLogs(string routerName = null, DateTime? since = null);
    void ClearLogs();
}