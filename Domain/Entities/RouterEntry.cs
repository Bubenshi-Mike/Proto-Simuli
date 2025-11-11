namespace Domain.Entities;

public class RouteEntry
{
    public string DestinationNetwork { get; set; }
    public int Metric { get; set; }
    public string NextHop { get; set; } // Router name or null for direct
    public string LearnedFrom { get; set; } // Router that advertised this route
    public DateTime LastUpdated { get; set; }
    public RouteStatus Status { get; set; }
    public DateTime? InvalidatedAt { get; set; }
    public DateTime? HoldDownStartedAt { get; set; }

    public bool IsDirect => string.IsNullOrEmpty(NextHop) || NextHop == "direct";

    public RouteEntryDto ToDto()
    {
        return new RouteEntryDto
        {
            DestinationNetwork = DestinationNetwork,
            Metric = Metric,
            NextHop = IsDirect ? "direct" : NextHop,
            LastUpdated = LastUpdated,
            Status = Status
        };
    }
}

public class Router
{
    public string Name { get; set; }
    public Dictionary<string, RouteEntry> RoutingTable { get; private set; }
    public List<string> Neighbors { get; set; }
    public List<string> DirectNetworks { get; set; }
    public List<NetworkInterface> Interfaces { get; set; }

    // RIP Timers (in seconds) - RFC 2453 standard values
    public int UpdateInterval { get; set; } = 30;
    public int InvalidTimer { get; set; } = 180;  // 6 × UpdateInterval
    public int HoldDownTimer { get; set; } = 180; // Same as InvalidTimer
    public int FlushTimer { get; set; } = 240;    // InvalidTimer + 60

    public DateTime LastUpdateSent { get; set; }
    public DateTime BootTime { get; set; }

    // Configuration
    public bool SplitHorizonEnabled { get; set; } = true;
    public bool PoisonReverseEnabled { get; set; } = false;

    public Router(string name, List<string> directNetworks, List<NetworkInterface> interfaces = null)
    {
        Name = name;
        RoutingTable = new Dictionary<string, RouteEntry>();
        Neighbors = new List<string>();
        DirectNetworks = directNetworks ?? new List<string>();
        Interfaces = interfaces ?? new List<NetworkInterface>();
        BootTime = DateTime.Now;
        LastUpdateSent = DateTime.MinValue;

        // Initialize routing table with direct networks
        foreach (var network in DirectNetworks)
        {
            RoutingTable[network] = new RouteEntry
            {
                DestinationNetwork = network,
                Metric = 1,
                NextHop = null,
                LearnedFrom = null,
                LastUpdated = BootTime,
                Status = RouteStatus.Valid
            };
        }
    }

    public List<RouteEntryDto> GetRoutingTableSnapshot()
    {
        return RoutingTable.Values
            .OrderBy(r => r.DestinationNetwork)
            .Select(r => r.ToDto())
            .ToList();
    }

    public void UpdateRoute(string network, int metric, string nextHop, string learnedFrom)
    {
        var now = DateTime.Now;

        if (RoutingTable.TryGetValue(network, out var existingRoute))
        {
            // Update existing route if metric is better or from same source
            if (metric < existingRoute.Metric || learnedFrom == existingRoute.LearnedFrom)
            {
                existingRoute.Metric = metric;
                existingRoute.NextHop = nextHop;
                existingRoute.LearnedFrom = learnedFrom;
                existingRoute.LastUpdated = now;
                existingRoute.Status = RouteStatus.Valid;
                existingRoute.InvalidatedAt = null;
                existingRoute.HoldDownStartedAt = null;
            }
        }
        else
        {
            // Add new route
            RoutingTable[network] = new RouteEntry
            {
                DestinationNetwork = network,
                Metric = metric,
                NextHop = nextHop,
                LearnedFrom = learnedFrom,
                LastUpdated = now,
                Status = RouteStatus.Valid
            };
        }
    }

    public void InvalidateRoute(string network)
    {
        if (RoutingTable.TryGetValue(network, out var route) && !route.IsDirect)
        {
            route.Status = RouteStatus.Invalid;
            route.InvalidatedAt = DateTime.Now;
            route.Metric = 16; // Infinity in RIP
        }
    }

    public void CheckTimers()
    {
        var now = DateTime.Now;
        var routesToCheck = RoutingTable.Values.Where(r => !r.IsDirect).ToList();

        foreach (var route in routesToCheck)
        {
            var timeSinceUpdate = (now - route.LastUpdated).TotalSeconds;

            switch (route.Status)
            {
                case RouteStatus.Valid:
                    if (timeSinceUpdate > InvalidTimer)
                    {
                        route.Status = RouteStatus.Invalid;
                        route.InvalidatedAt = now;
                        route.Metric = 16;
                    }
                    break;

                case RouteStatus.Invalid:
                    if (route.InvalidatedAt.HasValue)
                    {
                        var timeSinceInvalid = (now - route.InvalidatedAt.Value).TotalSeconds;
                        if (timeSinceInvalid > FlushTimer)
                        {
                            route.Status = RouteStatus.Flushed;
                        }
                    }
                    break;
            }
        }

        // Remove flushed routes
        var flushedRoutes = RoutingTable.Where(kvp => kvp.Value.Status == RouteStatus.Flushed).ToList();
        foreach (var kvp in flushedRoutes)
        {
            RoutingTable.Remove(kvp.Key);
        }
    }
}

public class Link
{
    public string RouterA { get; set; }
    public string RouterB { get; set; }
    public LinkStatus Status { get; set; }

    public bool ConnectsRouter(string routerName)
    {
        return RouterA == routerName || RouterB == routerName;
    }

    public string GetOtherRouter(string routerName)
    {
        if (RouterA == routerName) return RouterB;
        if (RouterB == routerName) return RouterA;
        return null;
    }
}

public class NetworkInterface
{
    public string InterfaceName { get; set; }
    public string IpAddress { get; set; }
    public string ConnectedTo { get; set; } // Router name or "LAN"
}