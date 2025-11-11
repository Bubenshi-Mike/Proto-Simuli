namespace Infrastructure.Jobs;

public class ScenarioABackgroundService : BackgroundService
{
    private readonly ITopologyService _topologyService;
    private readonly IRipProtocolService _ripProtocolService;
    private readonly ILoggingService _loggingService;
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<ScenarioABackgroundService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    private DateTime _simulationStartTime;
    private DateTime? _convergenceTime;
    private readonly int _updateIntervalSeconds = 30;
    private readonly int _timerCheckIntervalSeconds = 5;
    private readonly int _snapshotIntervalSeconds = 10;
    private readonly int _convergenceCheckIntervalSeconds = 5;
    private readonly int _postConvergenceWaitSeconds = 30; // Wait time after convergence before stopping

    public ScenarioABackgroundService(
        ITopologyService topologyService,
        IRipProtocolService ripProtocolService,
        ILoggingService loggingService,
        ISnapshotService snapshotService,
        ILogger<ScenarioABackgroundService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _topologyService = topologyService;
        _ripProtocolService = ripProtocolService;
        _loggingService = loggingService;
        _snapshotService = snapshotService;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    RIP PROTOCOL SIMULATION - SCENARIO A                       ║");
        _logger.LogInformation("║    Zynadex Corporate Network - Convergence (Happy Path)       ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        // Initialize mesh topology
        InitializeScenarioATopology();

        _simulationStartTime = DateTime.Now;
        _logger.LogInformation($"⏰ Simulation started at {_simulationStartTime:HH:mm:ss}");
        _logger.LogInformation("");

        // Send initial triggered updates (T=0-2s)
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        await SendInitialTriggeredUpdates(stoppingToken);

        // Start main simulation loops
        var updateTask = RunPeriodicUpdates(stoppingToken);
        var timerTask = RunTimerChecks(stoppingToken);
        var snapshotTask = RunPeriodicSnapshots(stoppingToken);
        var convergenceTask = MonitorConvergence(stoppingToken);

        await Task.WhenAny(updateTask, timerTask, snapshotTask, convergenceTask);
    }

    private void InitializeScenarioATopology()
    {
        var topology = new NetworkTopologyDto
        {
            Routers = new List<RouterConfigDto>
            {
                new RouterConfigDto
                {
                    RouterName = "HQ-GATEWAY",
                    DirectNetworks = new List<string> { "192.168.1.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.1.1", ConnectedTo = "HQ-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.0.1", ConnectedTo = "EDGE-NORTH-01" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.1.1", ConnectedTo = "DIST-PRIMARY" },
                        new NetworkInterfaceDto { InterfaceName = "eth3", IpAddress = "10.0.2.1", ConnectedTo = "CORE-WEST" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "BRANCH-GATEWAY",
                    DirectNetworks = new List<string> { "192.168.2.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.2.1", ConnectedTo = "BRANCH-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.7.2", ConnectedTo = "EDGE-NORTH-03" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.8.2", ConnectedTo = "DIST-PRIMARY" },
                        new NetworkInterfaceDto { InterfaceName = "eth3", IpAddress = "10.0.9.2", ConnectedTo = "CORE-EAST" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "EDGE-NORTH-01",
                    DirectNetworks = new List<string> { "192.168.11.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.11.1", ConnectedTo = "EDGE-01-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.0.2", ConnectedTo = "HQ-GATEWAY" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.3.1", ConnectedTo = "EDGE-NORTH-02" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "EDGE-NORTH-02",
                    DirectNetworks = new List<string> { "192.168.12.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.12.1", ConnectedTo = "EDGE-02-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.3.2", ConnectedTo = "EDGE-NORTH-01" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.4.1", ConnectedTo = "EDGE-NORTH-03" },
                        new NetworkInterfaceDto { InterfaceName = "eth3", IpAddress = "10.0.5.1", ConnectedTo = "DIST-PRIMARY" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "EDGE-NORTH-03",
                    DirectNetworks = new List<string> { "192.168.13.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.13.1", ConnectedTo = "EDGE-03-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.4.2", ConnectedTo = "EDGE-NORTH-02" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.7.1", ConnectedTo = "BRANCH-GATEWAY" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "DIST-PRIMARY",
                    DirectNetworks = new List<string> { "192.168.20.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.20.1", ConnectedTo = "DIST-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.1.2", ConnectedTo = "HQ-GATEWAY" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.8.1", ConnectedTo = "BRANCH-GATEWAY" },
                        new NetworkInterfaceDto { InterfaceName = "eth3", IpAddress = "10.0.5.2", ConnectedTo = "EDGE-NORTH-02" },
                        new NetworkInterfaceDto { InterfaceName = "eth4", IpAddress = "10.0.6.1", ConnectedTo = "CORE-CENTRAL" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "CORE-WEST",
                    DirectNetworks = new List<string> { "192.168.31.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.31.1", ConnectedTo = "CORE-WEST-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.2.2", ConnectedTo = "HQ-GATEWAY" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.10.1", ConnectedTo = "CORE-CENTRAL" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "CORE-CENTRAL",
                    DirectNetworks = new List<string> { "192.168.32.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.32.1", ConnectedTo = "CORE-CENTRAL-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.10.2", ConnectedTo = "CORE-WEST" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.11.1", ConnectedTo = "CORE-EAST" },
                        new NetworkInterfaceDto { InterfaceName = "eth3", IpAddress = "10.0.6.2", ConnectedTo = "DIST-PRIMARY" }
                    }
                },
                new RouterConfigDto
                {
                    RouterName = "CORE-EAST",
                    DirectNetworks = new List<string> { "192.168.33.0/24" },
                    Interfaces = new List<NetworkInterfaceDto>
                    {
                        new NetworkInterfaceDto { InterfaceName = "eth0", IpAddress = "192.168.33.1", ConnectedTo = "CORE-EAST-LAN" },
                        new NetworkInterfaceDto { InterfaceName = "eth1", IpAddress = "10.0.11.2", ConnectedTo = "CORE-CENTRAL" },
                        new NetworkInterfaceDto { InterfaceName = "eth2", IpAddress = "10.0.9.1", ConnectedTo = "BRANCH-GATEWAY" }
                    }
                }
            },
            Links = new List<LinkConfigDto>
            {
                // Top row: EDGE routers
                new LinkConfigDto { RouterA = "EDGE-NORTH-01", RouterB = "EDGE-NORTH-02", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "EDGE-NORTH-03", Status = LinkStatus.Up },
                
                // Middle row: Gateway routers
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                
                // Bottom row: CORE routers
                new LinkConfigDto { RouterA = "CORE-WEST", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "CORE-CENTRAL", RouterB = "CORE-EAST", Status = LinkStatus.Up },
                
                // Vertical connections (left column)
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "EDGE-NORTH-01", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "CORE-WEST", Status = LinkStatus.Up },
                
                // Vertical connections (middle column)
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                
                // Vertical connections (right column)
                new LinkConfigDto { RouterA = "EDGE-NORTH-03", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "BRANCH-GATEWAY", RouterB = "CORE-EAST", Status = LinkStatus.Up }
            }
        };

        _topologyService.InitializeTopology(topology);

        // All routers have Split Horizon enabled
        var allRouters = _topologyService.GetAllRouters();
        foreach (var router in allRouters)
        {
            router.SplitHorizonEnabled = true;
        }

        // Log topology
        _logger.LogInformation("📡 Zynadex Corporate Network Topology Initialized:");
        _logger.LogInformation("   ┌──────────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("   │  EDGE-NORTH-01 ─── EDGE-NORTH-02 ─── EDGE-NORTH-03             │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   HQ-GATEWAY ─────── DIST-PRIMARY ────── BRANCH-GATEWAY         │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   CORE-WEST ─────── CORE-CENTRAL ──────── CORE-EAST            │");
        _logger.LogInformation("   └──────────────────────────────────────────────────────────────────┘");
        _logger.LogInformation("");
        _logger.LogInformation("   📍 HQ-GATEWAY has network: 192.168.1.0/24 (Headquarters LAN)");
        _logger.LogInformation("   📍 BRANCH-GATEWAY has network: 192.168.2.0/24 (Branch Office LAN)");
        _logger.LogInformation("   📍 EDGE-NORTH-01 has network: 192.168.11.0/24 (Edge 01 LAN)");
        _logger.LogInformation("   📍 EDGE-NORTH-02 has network: 192.168.12.0/24 (Edge 02 LAN)");
        _logger.LogInformation("   📍 EDGE-NORTH-03 has network: 192.168.13.0/24 (Edge 03 LAN)");
        _logger.LogInformation("   📍 DIST-PRIMARY has network: 192.168.20.0/24 (Distribution LAN)");
        _logger.LogInformation("   📍 CORE-WEST has network: 192.168.31.0/24 (Core West LAN)");
        _logger.LogInformation("   📍 CORE-CENTRAL has network: 192.168.32.0/24 (Core Central LAN)");
        _logger.LogInformation("   📍 CORE-EAST has network: 192.168.33.0/24 (Core East LAN)");
        _logger.LogInformation("");
        _logger.LogInformation($"   ⚙️  Split Horizon: ENABLED on all routers");
        _logger.LogInformation($"   ⏱️  Update Interval: {_updateIntervalSeconds}s");
        _logger.LogInformation("");

        // Log router interfaces
        foreach (var routerConfig in topology.Routers)
        {
            _loggingService.Log(
                routerConfig.RouterName,
                LogEventType.ROUTER_BOOT,
                $"Router booted with {routerConfig.Interfaces.Count} interface(s)",
                new { Interfaces = routerConfig.Interfaces }
            );
        }
    }

    private async Task SendInitialTriggeredUpdates(CancellationToken stoppingToken)
    {
        var routers = _topologyService.GetAllRouters();

        _logger.LogInformation("🚀 Sending initial triggered updates from all routers...");
        _logger.LogInformation("");

        foreach (var router in routers)
        {
            if (stoppingToken.IsCancellationRequested) break;
            _ripProtocolService.SendTriggeredUpdate(router.Name);
            await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
        }
    }

    private async Task MonitorConvergence(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_convergenceCheckIntervalSeconds), stoppingToken);

                if (_convergenceTime.HasValue)
                {
                    // Already converged, check if we should stop
                    var timeSinceConvergence = (DateTime.Now - _convergenceTime.Value).TotalSeconds;
                    if (timeSinceConvergence >= _postConvergenceWaitSeconds)
                    {
                        _logger.LogInformation($"\n⏹️  Stopping simulation {_postConvergenceWaitSeconds}s after convergence...\n");
                        _appLifetime.StopApplication();
                        break;
                    }
                }
                else if (_ripProtocolService.HasNetworkConverged())
                {
                    _convergenceTime = DateTime.Now;
                    var elapsed = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;

                    _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
                    _logger.LogInformation("║    🎉 ZYNADEX NETWORK CONVERGED!                             ║");
                    _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
                    _logger.LogInformation($"\n✅ Convergence achieved at T={elapsed:F1}s");
                    _logger.LogInformation($"   All routers can now reach all networks!");
                    _logger.LogInformation($"   Continuing for {_postConvergenceWaitSeconds}s to verify stability...\n");

                    _loggingService.Log(
                        "SYSTEM",
                        LogEventType.CONVERGENCE_COMPLETE,
                        $"Network converged after {elapsed:F1} seconds",
                        new { ConvergenceTime = elapsed }
                    );
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring convergence");
            }
        }
    }

    private async Task RunPeriodicUpdates(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var routers = _topologyService.GetAllRouters();
                var now = DateTime.Now;

                foreach (var router in routers)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var timeSinceLastUpdate = (now - router.LastUpdateSent).TotalSeconds;
                    if (timeSinceLastUpdate >= _updateIntervalSeconds)
                    {
                        _ripProtocolService.SendScheduledUpdate(router.Name);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in periodic updates");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task RunTimerChecks(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var routers = _topologyService.GetAllRouters();
                foreach (var router in routers)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    _ripProtocolService.CheckRouterTimers(router.Name);
                }
                await Task.Delay(TimeSpan.FromSeconds(_timerCheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking timers");
                await Task.Delay(TimeSpan.FromSeconds(_timerCheckIntervalSeconds), stoppingToken);
            }
        }
    }

    private async Task RunPeriodicSnapshots(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_snapshotIntervalSeconds), stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                var snapshots = _ripProtocolService.GetAllRouterSnapshots();
                foreach (var snapshot in snapshots)
                {
                    _snapshotService.SaveSnapshot(snapshot);
                    _loggingService.Log(snapshot.RouterName, LogEventType.SNAPSHOT_SAVED,
                        $"Snapshot saved with {snapshot.RouteCount} routes",
                        new { RouteCount = snapshot.RouteCount });
                }

                var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
                var status = _convergenceTime.HasValue ? "✅ CONVERGED" : "⏳ CONVERGING";

                _logger.LogInformation($"\n⏱️  T={elapsed:F1}s [{status}] - Routing Table Summary:");
                foreach (var snapshot in snapshots.OrderBy(s => s.RouterName))
                {
                    _logger.LogInformation($"   {snapshot.RouterName,-18}: {snapshot.RouteCount} route(s)");
                }
                _logger.LogInformation("");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving snapshots");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    ZYNADEX SIMULATION COMPLETE                                ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝\n");

        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"⏱️  Total simulation time: {elapsed:F1} seconds");

        if (_convergenceTime.HasValue)
        {
            var convergenceElapsed = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"✅ Network converged after: {convergenceElapsed:F1} seconds");
        }
        _logger.LogInformation("");

        // Get final routing tables
        var finalSnapshots = _ripProtocolService.GetAllRouterSnapshots();
        var allRouters = _topologyService.GetAllRouters();

        // Display final routing tables in formatted table format
        _logger.LogInformation("📊 Final Routing Tables:\n");

        foreach (var router in allRouters.OrderBy(r => r.Name))
        {
            _logger.LogInformation($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            _logger.LogInformation($"Router: {router.Name}");
            _logger.LogInformation($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Display interfaces
            if (router.Interfaces.Any())
            {
                _logger.LogInformation("Interfaces:");
                foreach (var iface in router.Interfaces)
                {
                    _logger.LogInformation($"  {iface.InterfaceName,-6} {iface.IpAddress,-15} → {iface.ConnectedTo}");
                }
                _logger.LogInformation("");
            }

            // Display routing table in formatted table
            var snapshot = finalSnapshots.FirstOrDefault(s => s.RouterName == router.Name);
            if (snapshot?.RoutingTable.Any() == true)
            {
                _logger.LogInformation("Routing Table:");
                _logger.LogInformation("┌────────────────────────┬────────────────────────┬──────────────────┐");
                _logger.LogInformation("│  Destination Network   │    Next Hop (via)      │  Metric          │");
                _logger.LogInformation("├────────────────────────┼────────────────────────┼──────────────────┤");

                foreach (var route in snapshot.RoutingTable.OrderBy(r => r.DestinationNetwork))
                {
                    var destination = route.DestinationNetwork.PadRight(22);
                    var nextHop = route.NextHop.PadRight(22);
                    var metric = route.Metric.ToString().PadRight(16);

                    _logger.LogInformation($"│  {destination}│  {nextHop}│  {metric}│");
                }

                _logger.LogInformation("└────────────────────────┴────────────────────────┴──────────────────┘");
            }
            else
            {
                _logger.LogInformation("Routing Table: (no routes)");
            }
            _logger.LogInformation("");
        }

        // Export to file
        try
        {
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "ScenarioASimulation.txt");
            _snapshotService.ExportConvergedRoutingTables(exportPath, finalSnapshots);
            _logger.LogInformation($"📄 Routing tables exported to: {exportPath}\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export routing tables to file");
        }

        // Convergence status
        _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        if (_convergenceTime.HasValue)
        {
            _logger.LogInformation("✅ CONVERGENCE STATUS: SUCCESS");
            _logger.LogInformation($"   All {allRouters.Count} routers successfully learned all 9 networks");
            _logger.LogInformation("");
            _logger.LogInformation("📊 Zynadex Network Reachability:");
            _logger.LogInformation("   Every router can now reach:");
            _logger.LogInformation("   • 192.168.1.0/24  - HQ-GATEWAY (Headquarters)");
            _logger.LogInformation("   • 192.168.2.0/24  - BRANCH-GATEWAY (Branch Office)");
            _logger.LogInformation("   • 192.168.11.0/24 - EDGE-NORTH-01 (Edge Router 01)");
            _logger.LogInformation("   • 192.168.12.0/24 - EDGE-NORTH-02 (Edge Router 02)");
            _logger.LogInformation("   • 192.168.13.0/24 - EDGE-NORTH-03 (Edge Router 03)");
            _logger.LogInformation("   • 192.168.20.0/24 - DIST-PRIMARY (Distribution Layer)");
            _logger.LogInformation("   • 192.168.31.0/24 - CORE-WEST (Core West)");
            _logger.LogInformation("   • 192.168.32.0/24 - CORE-CENTRAL (Core Central)");
            _logger.LogInformation("   • 192.168.33.0/24 - CORE-EAST (Core East)");
        }
        else
        {
            _logger.LogInformation("⚠️  CONVERGENCE STATUS: INCOMPLETE");
        }
        _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        await base.StopAsync(stoppingToken);
    }
}