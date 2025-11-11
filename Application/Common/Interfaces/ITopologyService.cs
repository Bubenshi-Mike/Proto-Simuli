namespace Application.Common.Interfaces;

public interface ITopologyService
{
    void InitializeTopology(NetworkTopologyDto topology);
    Router GetRouter(string routerName);
    List<Router> GetAllRouters();
    List<Link> GetLinks();
    List<string> GetActiveNeighbors(string routerName);
    bool IsLinkUp(string routerA, string routerB);
    void SetLinkStatus(string routerA, string routerB, LinkStatus status);
    NetworkTopologyDto GetTopologySnapshot();
    List<string> GetAllNetworks(); // Get all networks in the topology
}