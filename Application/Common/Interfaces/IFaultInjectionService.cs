namespace Application.Common.Interfaces;

public interface IFaultInjectionService
{
    void DisableDirectNetwork(string routerName, string network);
    void EnableDirectNetwork(string routerName, string network);
    bool IsDirectNetworkEnabled(string routerName, string network);
    void SetLinkDown(string routerA, string routerB);
    void SetLinkUp(string routerA, string routerB);
    List<string> GetDisabledNetworks(string routerName);
}