namespace SharedKernel.DTOs;

public class RouteEntryDto
{
    public string DestinationNetwork { get; set; }
    public int Metric { get; set; }
    public string NextHop { get; set; } // Router name or "direct" for directly connected
    public DateTime LastUpdated { get; set; }
    public RouteStatus Status { get; set; }

    public RouteEntryDto Clone()
    {
        return new RouteEntryDto
        {
            DestinationNetwork = DestinationNetwork,
            Metric = Metric,
            NextHop = NextHop,
            LastUpdated = LastUpdated,
            Status = Status
        };
    }
}

public class RipUpdateMessageDto
{
    public string SourceRouter { get; set; }
    public string DestinationRouter { get; set; }
    public List<RouteEntryDto> Routes { get; set; } = new List<RouteEntryDto>();
    public DateTime Timestamp { get; set; }
    public bool IsTriggered { get; set; }
}

public class RouterSnapshotDto
{
    public string RouterName { get; set; }
    public DateTime Timestamp { get; set; }
    public List<RouteEntryDto> RoutingTable { get; set; } = new List<RouteEntryDto>();
    public int RouteCount { get; set; }
}

public class LogEntryDto
{
    public DateTime Timestamp { get; set; }
    public string RouterName { get; set; }
    public LogEventType EventType { get; set; }
    public string Message { get; set; }
    public object AdditionalData { get; set; }
}

public class NetworkTopologyDto
{
    public List<RouterConfigDto> Routers { get; set; } = new List<RouterConfigDto>();
    public List<LinkConfigDto> Links { get; set; } = new List<LinkConfigDto>();
}

public class RouterConfigDto
{
    public string RouterName { get; set; }
    public List<string> DirectNetworks { get; set; } = new List<string>();
    public List<NetworkInterfaceDto> Interfaces { get; set; } = new List<NetworkInterfaceDto>();
}

public class NetworkInterfaceDto
{
    public string InterfaceName { get; set; }
    public string IpAddress { get; set; }
    public string ConnectedTo { get; set; } // Router name or "LAN"
}

public class LinkConfigDto
{
    public string RouterA { get; set; }
    public string RouterB { get; set; }
    public LinkStatus Status { get; set; }
}