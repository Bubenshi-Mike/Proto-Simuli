namespace Infrastructure.Services;

public class RipProtocolService : IRipProtocolService
{
    private readonly ITopologyService _topologyService;
    private readonly ILoggingService _loggingService;
    private readonly ISnapshotService _snapshotService;

    public RipProtocolService(
        ITopologyService topologyService,
        ILoggingService loggingService,
        ISnapshotService snapshotService)
    {
        _topologyService = topologyService;
        _loggingService = loggingService;
        _snapshotService = snapshotService;
    }

    public void SendTriggeredUpdate(string routerName)
    {
        var router = _topologyService.GetRouter(routerName);
        if (router == null) return;

        var activeNeighbors = _topologyService.GetActiveNeighbors(routerName);

        foreach (var neighborName in activeNeighbors)
        {
            var updateMessage = CreateUpdateMessage(routerName, neighborName, isTriggered: true);

            _loggingService.Log(
                routerName,
                LogEventType.UPDATE_SENT,
                $"Sent triggered update to {neighborName} with {updateMessage.Routes.Count} routes",
                new { Destination = neighborName, RouteCount = updateMessage.Routes.Count, IsTriggered = true }
            );

            // Simulate message delivery
            ProcessReceivedUpdate(updateMessage);
        }

        router.LastUpdateSent = DateTime.Now;
    }

    public void SendScheduledUpdate(string routerName)
    {
        var router = _topologyService.GetRouter(routerName);
        if (router == null) return;

        var activeNeighbors = _topologyService.GetActiveNeighbors(routerName);

        foreach (var neighborName in activeNeighbors)
        {
            var updateMessage = CreateUpdateMessage(routerName, neighborName, isTriggered: false);

            _loggingService.Log(
                routerName,
                LogEventType.UPDATE_SENT,
                $"Sent scheduled update to {neighborName} with {updateMessage.Routes.Count} routes",
                new { Destination = neighborName, RouteCount = updateMessage.Routes.Count, IsTriggered = false }
            );

            // Simulate message delivery
            ProcessReceivedUpdate(updateMessage);
        }

        router.LastUpdateSent = DateTime.Now;
    }

    public RipUpdateMessageDto CreateUpdateMessage(string sourceRouter, string destinationRouter, bool isTriggered = false)
    {
        var router = _topologyService.GetRouter(sourceRouter);
        if (router == null) return null;

        var message = new RipUpdateMessageDto
        {
            SourceRouter = sourceRouter,
            DestinationRouter = destinationRouter,
            Timestamp = DateTime.Now,
            IsTriggered = isTriggered,
            Routes = new List<RouteEntryDto>()
        };

        // Include ALL routes from the routing table (including direct networks)
        foreach (var route in router.RoutingTable.Values)
        {
            // Skip invalid routes unless this is a triggered update
            if (route.Status != RouteStatus.Valid && !isTriggered)
                continue;

            var routeEntry = route.ToDto();

            // Apply Split Horizon - don't advertise routes back to where we learned them
            if (router.SplitHorizonEnabled && route.NextHop == destinationRouter)
            {
                if (router.PoisonReverseEnabled)
                {
                    // Poison Reverse: send with metric 16 (infinity)
                    routeEntry.Metric = 16;
                    message.Routes.Add(routeEntry);
                }
                // else: Simple Split Horizon - don't send at all
                continue;
            }

            // Add the route to the update message
            message.Routes.Add(routeEntry);
        }

        return message;
    }

    public void ProcessReceivedUpdate(RipUpdateMessageDto update)
    {
        var router = _topologyService.GetRouter(update.DestinationRouter);
        if (router == null) return;

        _loggingService.Log(
            update.DestinationRouter,
            LogEventType.UPDATE_RECEIVED,
            $"Received update from {update.SourceRouter} with {update.Routes.Count} routes",
            new { Source = update.SourceRouter, RouteCount = update.Routes.Count }
        );

        bool routingTableChanged = false;

        foreach (var receivedRoute in update.Routes)
        {
            // Skip routes to infinity
            if (receivedRoute.Metric >= 16)
                continue;

            // IMPORTANT: Only skip if this is OUR OWN direct network
            // We should still accept routes to OTHER routers' direct networks
            bool isOurDirectNetwork = router.DirectNetworks.Contains(receivedRoute.DestinationNetwork);
            if (isOurDirectNetwork)
            {
                // Skip - we already have this as a direct network with metric 1
                continue;
            }

            // Calculate new metric (add 1 hop to reach via this neighbor)
            int newMetric = receivedRoute.Metric + 1;

            // RIP maximum metric is 16 (infinity)
            if (newMetric >= 16)
                continue;

            var network = receivedRoute.DestinationNetwork;
            bool isNewRoute = !router.RoutingTable.ContainsKey(network);
            bool isBetterRoute = false;

            if (router.RoutingTable.TryGetValue(network, out var existingRoute))
            {
                // Update if better metric or from same source
                if (newMetric < existingRoute.Metric ||
                    existingRoute.LearnedFrom == update.SourceRouter)
                {
                    int oldMetric = existingRoute.Metric;
                    isBetterRoute = newMetric < existingRoute.Metric;

                    existingRoute.Metric = newMetric;
                    existingRoute.NextHop = update.SourceRouter;
                    existingRoute.LearnedFrom = update.SourceRouter;
                    existingRoute.LastUpdated = DateTime.Now;
                    existingRoute.Status = RouteStatus.Valid;
                    existingRoute.InvalidatedAt = null;
                    existingRoute.HoldDownStartedAt = null;

                    routingTableChanged = true;

                    if (isBetterRoute)
                    {
                        _loggingService.Log(
                            router.Name,
                            LogEventType.ROUTE_CHANGED,
                            $"Better route to {network}: metric {oldMetric}→{newMetric} via {update.SourceRouter}",
                            new { Network = network, OldMetric = oldMetric, NewMetric = newMetric, NextHop = update.SourceRouter }
                        );
                    }
                    else
                    {
                        _loggingService.Log(
                            router.Name,
                            LogEventType.ROUTE_CHANGED,
                            $"Updated route to {network}: metric {oldMetric}→{newMetric} via {update.SourceRouter}",
                            new { Network = network, OldMetric = oldMetric, NewMetric = newMetric, NextHop = update.SourceRouter }
                        );
                    }
                }
            }
            else
            {
                // New route - install it
                router.UpdateRoute(network, newMetric, update.SourceRouter, update.SourceRouter);
                routingTableChanged = true;
                isNewRoute = true;

                _loggingService.Log(
                    router.Name,
                    LogEventType.ROUTE_INSTALLED,
                    $"Installed new route to {network}: metric {newMetric} via {update.SourceRouter}",
                    new { Network = network, Metric = newMetric, NextHop = update.SourceRouter }
                );
            }
        }
    }

    public void CheckRouterTimers(string routerName)
    {
        var router = _topologyService.GetRouter(routerName);
        if (router == null) return;

        router.CheckTimers();
    }

    public RouterSnapshotDto GetRouterSnapshot(string routerName)
    {
        var router = _topologyService.GetRouter(routerName);
        if (router == null) return null;

        var snapshot = new RouterSnapshotDto
        {
            RouterName = routerName,
            Timestamp = DateTime.Now,
            RoutingTable = router.GetRoutingTableSnapshot(),
            RouteCount = router.RoutingTable.Count
        };

        return snapshot;
    }

    public List<RouterSnapshotDto> GetAllRouterSnapshots()
    {
        var routers = _topologyService.GetAllRouters();
        return routers.Select(r => GetRouterSnapshot(r.Name)).ToList();
    }

    public bool HasNetworkConverged()
    {
        var allNetworks = _topologyService.GetAllNetworks();
        var routers = _topologyService.GetAllRouters();

        // Check if every router can reach every network
        foreach (var router in routers)
        {
            foreach (var network in allNetworks)
            {
                // Check if router has a valid route to this network
                if (!router.RoutingTable.ContainsKey(network) ||
                    router.RoutingTable[network].Status != RouteStatus.Valid)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
