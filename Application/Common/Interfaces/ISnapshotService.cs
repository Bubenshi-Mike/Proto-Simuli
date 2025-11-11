namespace Application.Common.Interfaces;

public interface ISnapshotService
{
    void SaveSnapshot(RouterSnapshotDto snapshot);
    List<RouterSnapshotDto> GetSnapshots(string routerName = null, DateTime? since = null);
    RouterSnapshotDto GetLatestSnapshot(string routerName);
    void ClearSnapshots();
    void ExportConvergedRoutingTables(string filePath, List<RouterSnapshotDto> snapshots);
    void ExportScenarioBResults(string filePath, List<RouterSnapshotDto> snapshots,
        DateTime simulationStart, DateTime? convergenceTime, DateTime? faultTime, List<LogEntryDto> logs);
    void ExportScenarioCResults(string filePath, List<RouterSnapshotDto> snapshots,
        DateTime simulationStart, DateTime? convergenceTime, DateTime? faultTime, List<LogEntryDto> logs);
    void ExportScenarioDResults(string filePath, List<RouterSnapshotDto> snapshots,
        DateTime simulationStart, DateTime? convergenceTime, DateTime? faultTime, List<LogEntryDto> logs);
    void ExportScenarioEResults(string exportPath, List<RouterSnapshotDto> finalSnapshots, DateTime simulationStartTime, DateTime? convergenceTime, List<DateTime> linkFlapTimes, List<LogEntryDto> allLogs);
    void ExportScenarioFResults(string exportPath, List<RouterSnapshotDto> finalSnapshots, DateTime simulationStartTime, DateTime? convergenceTime, DateTime? partitionTime, DateTime? healingTime, List<LogEntryDto> allLogs);
}