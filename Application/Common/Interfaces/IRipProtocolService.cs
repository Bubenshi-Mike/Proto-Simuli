namespace Application.Common.Interfaces;

public interface IRipProtocolService
{
    void SendTriggeredUpdate(string routerName);
    void SendScheduledUpdate(string routerName);
    void ProcessReceivedUpdate(RipUpdateMessageDto update);
    RipUpdateMessageDto CreateUpdateMessage(string sourceRouter, string destinationRouter, bool isTriggered = false);
    void CheckRouterTimers(string routerName);
    RouterSnapshotDto GetRouterSnapshot(string routerName);
    List<RouterSnapshotDto> GetAllRouterSnapshots();
    bool HasNetworkConverged(); // Check if all routers have learned all routes
}