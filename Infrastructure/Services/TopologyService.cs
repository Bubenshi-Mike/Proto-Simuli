namespace Infrastructure.Services;

public class TopologyService : ITopologyService
{
    private readonly Dictionary<string, Router> _routers = new Dictionary<string, Router>();
    private readonly List<Link> _links = new List<Link>();
    private readonly object _lockObj = new object();

    public void InitializeTopology(NetworkTopologyDto topology)
    {
        lock (_lockObj)
        {
            _routers.Clear();
            _links.Clear();

            // Create routers
            foreach (var routerConfig in topology.Routers)
            {
                var interfaces = routerConfig.Interfaces?.Select(i => new NetworkInterface
                {
                    InterfaceName = i.InterfaceName,
                    IpAddress = i.IpAddress,
                    ConnectedTo = i.ConnectedTo
                }).ToList();

                var router = new Router(routerConfig.RouterName, routerConfig.DirectNetworks, interfaces);
                _routers[routerConfig.RouterName] = router;
            }

            // Create links and establish neighbors
            foreach (var linkConfig in topology.Links)
            {
                var link = new Link
                {
                    RouterA = linkConfig.RouterA,
                    RouterB = linkConfig.RouterB,
                    Status = linkConfig.Status
                };
                _links.Add(link);

                // Add neighbors (only if link is up initially)
                if (linkConfig.Status == LinkStatus.Up)
                {
                    if (_routers.TryGetValue(linkConfig.RouterA, out var routerA))
                    {
                        if (!routerA.Neighbors.Contains(linkConfig.RouterB))
                            routerA.Neighbors.Add(linkConfig.RouterB);
                    }

                    if (_routers.TryGetValue(linkConfig.RouterB, out var routerB))
                    {
                        if (!routerB.Neighbors.Contains(linkConfig.RouterA))
                            routerB.Neighbors.Add(linkConfig.RouterA);
                    }
                }
            }
        }
    }

    public Router GetRouter(string routerName)
    {
        lock (_lockObj)
        {
            _routers.TryGetValue(routerName, out var router);
            return router;
        }
    }

    public List<Router> GetAllRouters()
    {
        lock (_lockObj)
        {
            return _routers.Values.ToList();
        }
    }

    public List<Link> GetLinks()
    {
        lock (_lockObj)
        {
            return _links.ToList();
        }
    }

    public List<string> GetActiveNeighbors(string routerName)
    {
        lock (_lockObj)
        {
            if (_routers.TryGetValue(routerName, out var router))
            {
                return _links
                    .Where(l => l.Status == LinkStatus.Up && l.ConnectsRouter(routerName))
                    .Select(l => l.GetOtherRouter(routerName))
                    .Where(n => n != null)
                    .ToList();
            }
            return new List<string>();
        }
    }

    public bool IsLinkUp(string routerA, string routerB)
    {
        lock (_lockObj)
        {
            return _links.Any(l => l.Status == LinkStatus.Up &&
                ((l.RouterA == routerA && l.RouterB == routerB) ||
                 (l.RouterA == routerB && l.RouterB == routerA)));
        }
    }

    public void SetLinkStatus(string routerA, string routerB, LinkStatus status)
    {
        lock (_lockObj)
        {
            var link = _links.FirstOrDefault(l =>
                (l.RouterA == routerA && l.RouterB == routerB) ||
                (l.RouterA == routerB && l.RouterB == routerA));

            if (link != null)
            {
                link.Status = status;
            }
        }
    }

    public NetworkTopologyDto GetTopologySnapshot()
    {
        lock (_lockObj)
        {
            return new NetworkTopologyDto
            {
                Routers = _routers.Values.Select(r => new RouterConfigDto
                {
                    RouterName = r.Name,
                    DirectNetworks = new List<string>(r.DirectNetworks),
                    Interfaces = r.Interfaces.Select(i => new NetworkInterfaceDto
                    {
                        InterfaceName = i.InterfaceName,
                        IpAddress = i.IpAddress,
                        ConnectedTo = i.ConnectedTo
                    }).ToList()
                }).ToList(),
                Links = _links.Select(l => new LinkConfigDto
                {
                    RouterA = l.RouterA,
                    RouterB = l.RouterB,
                    Status = l.Status
                }).ToList()
            };
        }
    }

    public List<string> GetAllNetworks()
    {
        lock (_lockObj)
        {
            var allNetworks = new HashSet<string>();
            foreach (var router in _routers.Values)
            {
                foreach (var network in router.DirectNetworks)
                {
                    allNetworks.Add(network);
                }
            }
            return allNetworks.ToList();
        }
    }
}