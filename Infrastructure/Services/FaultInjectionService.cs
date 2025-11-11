namespace Infrastructure.Services;

public class FaultInjectionService(ITopologyService topologyService, ILoggingService loggingService) : IFaultInjectionService
{
    private readonly ITopologyService _topologyService = topologyService;
    private readonly ILoggingService _loggingService = loggingService;
    private readonly Dictionary<string, HashSet<string>> _disabledNetworks = [];
    private readonly object _lockObj = new();

    public void DisableDirectNetwork(string routerName, string network)
    {
        lock (_lockObj)
        {
            if (!_disabledNetworks.TryGetValue(routerName, out HashSet<string>? value))
            {
                value = [];
                _disabledNetworks[routerName] = value;
            }

            value.Add(network);

            var router = _topologyService.GetRouter(routerName);
            if (router != null && router.RoutingTable.TryGetValue(network, out RouteEntry? route))
            {
                if (route.IsDirect)
                {
                    // Mark as invalid with metric 16 (infinity)
                    route.Metric = 16;
                    route.Status = RouteStatus.Invalid;
                    route.InvalidatedAt = DateTime.Now;

                    _loggingService.Log(
                        routerName,
                        LogEventType.ROUTE_INVALIDATED,
                        $"Direct network {network} disabled - route set to metric 16 (infinity)",
                        new { Network = network, Metric = 16, Reason = "DirectNetworkDisabled" }
                    );
                }
            }
        }
    }

    public void EnableDirectNetwork(string routerName, string network)
    {
        lock (_lockObj)
        {
            if (_disabledNetworks.TryGetValue(routerName, out HashSet<string>? value))
            {
                value.Remove(network);

                if (_disabledNetworks[routerName].Count == 0)
                {
                    _disabledNetworks.Remove(routerName);
                }
            }

            var router = _topologyService.GetRouter(routerName);
            if (router != null && router.DirectNetworks.Contains(network))
            {
                // Restore the direct route
                if (router.RoutingTable.TryGetValue(network, out RouteEntry? route))
                {
                    route.Metric = 1;
                    route.Status = RouteStatus.Valid;
                    route.InvalidatedAt = null;
                    route.LastUpdated = DateTime.Now;
                }

                _loggingService.Log(
                    routerName,
                    LogEventType.ROUTE_INSTALLED,
                    $"Direct network {network} re-enabled",
                    new { Network = network, Metric = 1 }
                );
            }
        }
    }

    public bool IsDirectNetworkEnabled(string routerName, string network)
    {
        lock (_lockObj)
        {
            return !_disabledNetworks.ContainsKey(routerName) ||
                   !_disabledNetworks[routerName].Contains(network);
        }
    }

    public void SetLinkDown(string routerA, string routerB)
    {
        _topologyService.SetLinkStatus(routerA, routerB, LinkStatus.Down);

        _loggingService.Log(
            routerA,
            LogEventType.LINK_DOWN,
            $"Link {routerA}—{routerB} is DOWN",
            new { RouterA = routerA, RouterB = routerB }
        );
    }

    public void SetLinkUp(string routerA, string routerB)
    {
        _topologyService.SetLinkStatus(routerA, routerB, LinkStatus.Up);

        _loggingService.Log(
            routerA,
            LogEventType.LINK_UP,
            $"Link {routerA}—{routerB} is UP",
            new { RouterA = routerA, RouterB = routerB }
        );
    }

    public List<string> GetDisabledNetworks(string routerName)
    {
        lock (_lockObj)
        {
            if (_disabledNetworks.TryGetValue(routerName, out HashSet<string>? value))
            {
                return [.. value];
            }
            return [];
        }
    }
}