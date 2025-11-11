namespace Infrastructure.Jobs;

public class ScenarioCBackgroundService : BackgroundService
{
    private readonly ITopologyService _topologyService;
    private readonly IRipProtocolService _ripProtocolService;
    private readonly ILoggingService _loggingService;
    private readonly ISnapshotService _snapshotService;
    private readonly IFaultInjectionService _faultInjectionService;
    private readonly ILogger<ScenarioCBackgroundService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    private DateTime _simulationStartTime;
    private DateTime? _convergenceTime;
    private DateTime? _faultInjectionTime;
    private bool _faultInjected = false;

    private readonly int _updateIntervalSeconds = 30;
    private readonly int _timerCheckIntervalSeconds = 5;
    private readonly int _snapshotIntervalSeconds = 10;
    private readonly int _convergenceCheckIntervalSeconds = 5;
    private readonly int _convergenceWaitSeconds = 90;
    private readonly int _postFaultDurationSeconds = 180; // 3 minutes post-fault

    public ScenarioCBackgroundService(
        ITopologyService topologyService,
        IRipProtocolService ripProtocolService,
        ILoggingService loggingService,
        ISnapshotService snapshotService,
        IFaultInjectionService faultInjectionService,
        ILogger<ScenarioCBackgroundService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _topologyService = topologyService;
        _ripProtocolService = ripProtocolService;
        _loggingService = loggingService;
        _snapshotService = snapshotService;
        _faultInjectionService = faultInjectionService;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    RIP PROTOCOL SIMULATION - SCENARIO C                       ║");
        _logger.LogInformation("║    Zynadex Network - Split Horizon Protection                 ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        InitializeScenarioCTopology();

        _simulationStartTime = DateTime.Now;
        _logger.LogInformation($"⏰ Simulation started at {_simulationStartTime:HH:mm:ss}");
        _logger.LogInformation("");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        await SendInitialTriggeredUpdates(stoppingToken);

        var updateTask = RunPeriodicUpdates(stoppingToken);
        var timerTask = RunTimerChecks(stoppingToken);
        var snapshotTask = RunPeriodicSnapshots(stoppingToken);
        var convergenceTask = MonitorConvergence(stoppingToken);
        var faultTask = MonitorFaultInjection(stoppingToken);

        await Task.WhenAny(updateTask, timerTask, snapshotTask, convergenceTask, faultTask);
    }

    private void InitializeScenarioCTopology()
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
                new LinkConfigDto { RouterA = "EDGE-NORTH-01", RouterB = "EDGE-NORTH-02", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "EDGE-NORTH-03", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "CORE-WEST", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "CORE-CENTRAL", RouterB = "CORE-EAST", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "EDGE-NORTH-01", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "CORE-WEST", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-03", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "BRANCH-GATEWAY", RouterB = "CORE-EAST", Status = LinkStatus.Up }
            }
        };

        _topologyService.InitializeTopology(topology);

        // CRITICAL: Split Horizon ENABLED on ALL routers (this is the protection!)
        var allRouters = _topologyService.GetAllRouters();
        foreach (var router in allRouters)
        {
            router.SplitHorizonEnabled = true;
        }

        _logger.LogInformation("📡 Zynadex Corporate Network Topology:");
        _logger.LogInformation("   ┌──────────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("   │  EDGE-NORTH-01 ─── EDGE-NORTH-02 ─── EDGE-NORTH-03             │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   HQ-GATEWAY ─────── DIST-PRIMARY ────── BRANCH-GATEWAY         │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   CORE-WEST ─────── CORE-CENTRAL ──────── CORE-EAST            │");
        _logger.LogInformation("   └──────────────────────────────────────────────────────────────────┘");
        _logger.LogInformation("");
        _logger.LogInformation("✅ Split Horizon: ENABLED on ALL routers");
        _logger.LogInformation("   This prevents advertising routes back to their source");
        _logger.LogInformation("");
        _logger.LogInformation($"   ⏱️  Update Interval: {_updateIntervalSeconds}s");
        _logger.LogInformation($"   🎯 Fault Scenario: CORE-CENTRAL router will fail after convergence");
        _logger.LogInformation($"   ⏳ Waiting {_convergenceWaitSeconds}s for initial convergence...");
        _logger.LogInformation("");
        _logger.LogInformation("🔍 Objective:");
        _logger.LogInformation("   Demonstrate how Split Horizon prevents routing loops during");
        _logger.LogInformation("   router failure by blocking routes from being advertised back");
        _logger.LogInformation("   to the interface they were learned from.");
        _logger.LogInformation("");
    }

    private async Task SendInitialTriggeredUpdates(CancellationToken stoppingToken)
    {
        var routers = _topologyService.GetAllRouters();
        _logger.LogInformation("🚀 Sending initial triggered updates from all routers...");

        foreach (var router in routers)
        {
            if (stoppingToken.IsCancellationRequested) break;
            _ripProtocolService.SendTriggeredUpdate(router.Name);
            await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
        }
        _logger.LogInformation("");
    }

    private async Task MonitorConvergence(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !_convergenceTime.HasValue)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_convergenceCheckIntervalSeconds), stoppingToken);

                if (_ripProtocolService.HasNetworkConverged())
                {
                    _convergenceTime = DateTime.Now;
                    var elapsed = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;

                    _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
                    _logger.LogInformation("║    ✅ ZYNADEX NETWORK CONVERGED!                             ║");
                    _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
                    _logger.LogInformation($"\n   Convergence achieved at T={elapsed:F1}s");
                    _logger.LogInformation($"   All routers learned all networks with Split Horizon enabled\n");

                    _loggingService.Log("SYSTEM", LogEventType.CONVERGENCE_COMPLETE,
                        $"Network converged after {elapsed:F1} seconds", new { ConvergenceTime = elapsed });
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task MonitorFaultInjection(CancellationToken stoppingToken)
    {
        while (!_convergenceTime.HasValue && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (!_convergenceTime.HasValue) return;

        await Task.Delay(TimeSpan.FromSeconds(_convergenceWaitSeconds -
            (_convergenceTime.Value - _simulationStartTime).TotalSeconds), stoppingToken);

        if (_faultInjected || stoppingToken.IsCancellationRequested) return;

        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation($"║    🔥 FAULT INJECTION - T={elapsed:F1}s                          ║");
        _logger.LogInformation($"╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("   💥 CORE-CENTRAL Router FAILURE");
        _logger.LogInformation("   Simulating complete router failure (hardware/power failure)...");
        _logger.LogInformation("");
        _logger.LogInformation("   Impact:");
        _logger.LogInformation("   • CORE-CENTRAL network (192.168.32.0/24) becomes unreachable");
        _logger.LogInformation("   • 4 direct connections lost");
        _logger.LogInformation("   • Network must reconverge using alternate paths");
        _logger.LogInformation("");
        _logger.LogInformation("   ✅ Split Horizon is ENABLED!");
        _logger.LogInformation("   Expected: Clean reconvergence WITHOUT routing loops\n");

        _faultInjectionService.DisableDirectNetwork("CORE-CENTRAL", "192.168.32.0/24");
        _faultInjected = true;
        _faultInjectionTime = DateTime.Now;

        _loggingService.Log("SYSTEM", LogEventType.FAULT_INJECTED,
            "CORE-CENTRAL router failed - complete outage (Split Horizon ENABLED)",
            new { Router = "CORE-CENTRAL", SplitHorizon = true });

        _logger.LogInformation("📊 Observe: Routes should expire cleanly via timeout, NOT count-to-infinity\n");

        await Task.Delay(TimeSpan.FromSeconds(_postFaultDurationSeconds), stoppingToken);

        _logger.LogInformation($"\n⏹️  Stopping simulation after {_postFaultDurationSeconds}s post-fault observation\n");
        _appLifetime.StopApplication();
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
                    if (!router.SplitHorizonEnabled) continue;

                    var timeSinceLastUpdate = (now - router.LastUpdateSent).TotalSeconds;
                    if (timeSinceLastUpdate >= _updateIntervalSeconds)
                    {
                        _ripProtocolService.SendScheduledUpdate(router.Name);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
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
                    if (!router.SplitHorizonEnabled) continue;

                    _ripProtocolService.CheckRouterTimers(router.Name);
                }
                await Task.Delay(TimeSpan.FromSeconds(_timerCheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
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
                }

                var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
                var status = _convergenceTime.HasValue ?
                    (_faultInjected ? "🔥 SPLIT-HORIZON" : "✅ CONVERGED") : "⏳ CONVERGING";

                _logger.LogInformation($"\n⏱️  T={elapsed:F1}s [{status}] - Routing Table Summary:");

                var activeRouters = snapshots.Where(s => s.RouteCount >= 0).OrderBy(s => s.RouterName);
                foreach (var snapshot in activeRouters)
                {
                    var routerStatus = snapshot.RouterName == "CORE-CENTRAL" && _faultInjected ? " [FAILED]" : "";
                    _logger.LogInformation($"   {snapshot.RouterName,-18}{routerStatus}: {snapshot.RouteCount} route(s)");
                }
                _logger.LogInformation("");

                if (_faultInjected && _faultInjectionTime.HasValue)
                {
                    var timeSinceFault = (DateTime.Now - _faultInjectionTime.Value).TotalSeconds;
                    _logger.LogInformation($"   Fault+{timeSinceFault:F0}s: Network reconverging with Split Horizon protection");
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    ZYNADEX SCENARIO C SIMULATION COMPLETE                     ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝\n");

        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"⏱️  Total simulation time: {elapsed:F1}s");

        if (_convergenceTime.HasValue)
        {
            var convergenceTime = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"✅ Initial convergence: {convergenceTime:F1}s");
        }

        if (_faultInjectionTime.HasValue)
        {
            var faultTime = (_faultInjectionTime.Value - _simulationStartTime).TotalSeconds;
            var observationTime = elapsed - faultTime;
            _logger.LogInformation($"🔥 Fault injected at: {faultTime:F1}s");
            _logger.LogInformation($"📊 Observation period: {observationTime:F1}s");
        }

        if (_faultInjectionTime.HasValue)
        {
            _logger.LogInformation("\n🎯 Split Horizon Effectiveness:");

            var routingLoops = _loggingService.GetLogs()
                .Count(l => l.EventType == LogEventType.COUNT_TO_INFINITY_DETECTED);

            _logger.LogInformation($"   • Routing loops detected: {routingLoops} (Expected: 0)");

            if (routingLoops == 0)
            {
                _logger.LogInformation("\n   ✅ SUCCESS! Split Horizon prevented routing loops");
                _logger.LogInformation("   Routes expired cleanly via timeout mechanism");
            }
            else
            {
                _logger.LogInformation("\n   ⚠️  Unexpected: Routing loops detected despite Split Horizon");
            }
        }

        try
        {
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "ZynadexScenarioC_Simulation.txt");
            var finalSnapshots = _ripProtocolService.GetAllRouterSnapshots();
            var allLogs = _loggingService.GetLogs();

            _snapshotService.ExportScenarioCResults(
                exportPath,
                finalSnapshots,
                _simulationStartTime,
                _convergenceTime,
                _faultInjectionTime,
                allLogs
            );

            _logger.LogInformation($"\n📄 Detailed analysis exported to: {exportPath}");
            _logger.LogInformation("   Report includes:");
            _logger.LogInformation("   • Split Horizon mechanism explanation");
            _logger.LogInformation("   • Comparison with Scenario B");
            _logger.LogInformation("   • Clean reconvergence timeline");
            _logger.LogInformation("   • Loop prevention analysis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export results");
        }

        _logger.LogInformation("");
        await base.StopAsync(stoppingToken);
    }
}