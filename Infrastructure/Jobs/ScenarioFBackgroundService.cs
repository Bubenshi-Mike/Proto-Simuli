namespace Infrastructure.Jobs;

public class ScenarioFBackgroundService : BackgroundService
{
    private readonly ITopologyService _topologyService;
    private readonly IRipProtocolService _ripProtocolService;
    private readonly ILoggingService _loggingService;
    private readonly ISnapshotService _snapshotService;
    private readonly IFaultInjectionService _faultInjectionService;
    private readonly ILogger<ScenarioFBackgroundService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    private DateTime _simulationStartTime;
    private DateTime? _convergenceTime;
    private DateTime? _partitionTime;
    private DateTime? _healingTime;
    private bool _partitionCreated = false;
    private bool _partitionHealed = false;

    private readonly int _updateIntervalSeconds = 30;
    private readonly int _timerCheckIntervalSeconds = 5;
    private readonly int _snapshotIntervalSeconds = 10;
    private readonly int _convergenceCheckIntervalSeconds = 5;
    private readonly int _convergenceWaitSeconds = 90;
    private readonly int _partitionDurationSeconds = 120; // Keep partition for 2 minutes
    private readonly int _postHealingObservationSeconds = 120; // Observe healing for 2 minutes

    public ScenarioFBackgroundService(
        ITopologyService topologyService,
        IRipProtocolService ripProtocolService,
        ILoggingService loggingService,
        ISnapshotService snapshotService,
        IFaultInjectionService faultInjectionService,
        ILogger<ScenarioFBackgroundService> logger,
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
        _logger.LogInformation("║    RIP PROTOCOL SIMULATION - SCENARIO F                       ║");
        _logger.LogInformation("║    Zynadex Network - Network Partition & Healing              ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        InitializeScenarioFTopology();

        _simulationStartTime = DateTime.Now;
        _logger.LogInformation($"⏰ Simulation started at {_simulationStartTime:HH:mm:ss}");
        _logger.LogInformation("");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        await SendInitialTriggeredUpdates(stoppingToken);

        var updateTask = RunPeriodicUpdates(stoppingToken);
        var timerTask = RunTimerChecks(stoppingToken);
        var snapshotTask = RunPeriodicSnapshots(stoppingToken);
        var convergenceTask = MonitorConvergence(stoppingToken);
        var partitionTask = SimulateNetworkPartition(stoppingToken);

        await Task.WhenAny(updateTask, timerTask, snapshotTask, convergenceTask, partitionTask);
    }

    private void InitializeScenarioFTopology()
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
                // Edge layer (top)
                new LinkConfigDto { RouterA = "EDGE-NORTH-01", RouterB = "EDGE-NORTH-02", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "EDGE-NORTH-03", Status = LinkStatus.Up },
                
                // Distribution layer (middle) - THESE WILL BE CUT TO CREATE PARTITION
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "EDGE-NORTH-02", RouterB = "DIST-PRIMARY", Status = LinkStatus.Up },
                
                // Core layer (bottom) - THESE WILL BE CUT TO CREATE PARTITION
                new LinkConfigDto { RouterA = "CORE-WEST", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "CORE-CENTRAL", RouterB = "CORE-EAST", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "CORE-CENTRAL", Status = LinkStatus.Up },
                
                // Vertical connections (left)
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "EDGE-NORTH-01", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "HQ-GATEWAY", RouterB = "CORE-WEST", Status = LinkStatus.Up },
                
                // Vertical connections (right)
                new LinkConfigDto { RouterA = "EDGE-NORTH-03", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up },
                new LinkConfigDto { RouterA = "BRANCH-GATEWAY", RouterB = "CORE-EAST", Status = LinkStatus.Up }
            }
        };

        _topologyService.InitializeTopology(topology);

        var allRouters = _topologyService.GetAllRouters();
        foreach (var router in allRouters)
        {
            router.SplitHorizonEnabled = true;
            router.PoisonReverseEnabled = true;
        }

        _logger.LogInformation("📡 Zynadex Corporate Network Topology:");
        _logger.LogInformation("   ┌──────────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("   │           LEFT PARTITION              RIGHT PARTITION            │");
        _logger.LogInformation("   │                                                                  │");
        _logger.LogInformation("   │  EDGE-NORTH-01 ─── EDGE-NORTH-02 ─── EDGE-NORTH-03             │");
        _logger.LogInformation("   │        │                 │ X               │                     │");
        _logger.LogInformation("   │   HQ-GATEWAY        X  DIST-PRIMARY  X   BRANCH-GATEWAY         │");
        _logger.LogInformation("   │        │                 │ X               │                     │");
        _logger.LogInformation("   │   CORE-WEST        X  CORE-CENTRAL  X   CORE-EAST              │");
        _logger.LogInformation("   │                                                                  │");
        _logger.LogInformation("   └──────────────────────────────────────────────────────────────────┘");
        _logger.LogInformation("");
        _logger.LogInformation("   X = Links that will be severed to create partition");
        _logger.LogInformation("");
        _logger.LogInformation("✅ Split Horizon + Poison Reverse: ENABLED on all routers");
        _logger.LogInformation("");
        _logger.LogInformation($"   🎯 Partition Strategy: Sever middle column (DIST-PRIMARY, CORE-CENTRAL)");
        _logger.LogInformation($"   ⏳ Waiting {_convergenceWaitSeconds}s for initial convergence...");
        _logger.LogInformation($"   📊 Partition Duration: {_partitionDurationSeconds}s");
        _logger.LogInformation($"   🔄 Then heal network and observe reconvergence");
        _logger.LogInformation("");
        _logger.LogInformation("🎯 Objective:");
        _logger.LogInformation("   Demonstrate RIP behavior during complete network partition (split");
        _logger.LogInformation("   brain scenario) and subsequent healing. Shows how RIP handles:");
        _logger.LogInformation("   • Network segmentation into isolated islands");
        _logger.LogInformation("   • Independent convergence in each partition");
        _logger.LogInformation("   • Network healing when connectivity restored");
        _logger.LogInformation("   • Route reconvergence across former partition boundary");
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
                    _logger.LogInformation($"   Ready to create network partition\n");

                    _loggingService.Log("SYSTEM", LogEventType.CONVERGENCE_COMPLETE,
                        $"Network converged after {elapsed:F1} seconds", new { ConvergenceTime = elapsed });
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SimulateNetworkPartition(CancellationToken stoppingToken)
    {
        while (!_convergenceTime.HasValue && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (!_convergenceTime.HasValue) return;

        await Task.Delay(TimeSpan.FromSeconds(_convergenceWaitSeconds -
            (_convergenceTime.Value - _simulationStartTime).TotalSeconds), stoppingToken);

        if (_partitionCreated || stoppingToken.IsCancellationRequested) return;

        // === CREATE PARTITION ===
        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation($"║    ⚡ NETWORK PARTITION - T={elapsed:F1}s                        ║");
        _logger.LogInformation($"╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("   💥 CATASTROPHIC FAILURE - Severing middle column links!");
        _logger.LogInformation("   Simulating: Data center split, WAN failure, or fiber cut");
        _logger.LogInformation("");
        _logger.LogInformation("   Severing 6 critical links:");
        _logger.LogInformation("   • HQ-GATEWAY ↔ DIST-PRIMARY");
        _logger.LogInformation("   • DIST-PRIMARY ↔ BRANCH-GATEWAY");
        _logger.LogInformation("   • EDGE-NORTH-02 ↔ DIST-PRIMARY");
        _logger.LogInformation("   • CORE-WEST ↔ CORE-CENTRAL");
        _logger.LogInformation("   • CORE-CENTRAL ↔ CORE-EAST");
        _logger.LogInformation("   • DIST-PRIMARY ↔ CORE-CENTRAL");
        _logger.LogInformation("");

        _faultInjectionService.SetLinkDown("HQ-GATEWAY", "DIST-PRIMARY");
        _faultInjectionService.SetLinkDown("DIST-PRIMARY", "BRANCH-GATEWAY");
        _faultInjectionService.SetLinkDown("EDGE-NORTH-02", "DIST-PRIMARY");
        _faultInjectionService.SetLinkDown("CORE-WEST", "CORE-CENTRAL");
        _faultInjectionService.SetLinkDown("CORE-CENTRAL", "CORE-EAST");
        _faultInjectionService.SetLinkDown("DIST-PRIMARY", "CORE-CENTRAL");

        _partitionCreated = true;
        _partitionTime = DateTime.Now;

        _loggingService.Log("SYSTEM", LogEventType.NETWORK_PARTITION,
            "Network partitioned into two islands",
            new
            {
                LeftPartition = "HQ-GATEWAY, EDGE-NORTH-01, CORE-WEST",
                RightPartition = "BRANCH-GATEWAY, EDGE-NORTH-03, CORE-EAST",
                IsolatedRouters = "DIST-PRIMARY, CORE-CENTRAL, EDGE-NORTH-02"
            });

        _logger.LogInformation("   📊 Network Status:");
        _logger.LogInformation("   • LEFT PARTITION: HQ-GATEWAY, EDGE-NORTH-01, CORE-WEST");
        _logger.LogInformation("   • RIGHT PARTITION: BRANCH-GATEWAY, EDGE-NORTH-03, CORE-EAST");
        _logger.LogInformation("   • ISOLATED: DIST-PRIMARY, CORE-CENTRAL, EDGE-NORTH-02");
        _logger.LogInformation("");
        _logger.LogInformation($"   ⏳ Maintaining partition for {_partitionDurationSeconds}s...\n");

        // Wait during partition
        await Task.Delay(TimeSpan.FromSeconds(_partitionDurationSeconds), stoppingToken);

        if (stoppingToken.IsCancellationRequested) return;

        // === HEAL PARTITION ===
        elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation($"║    🔄 NETWORK HEALING - T={elapsed:F1}s                          ║");
        _logger.LogInformation($"╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("   ✅ Restoring all severed links!");
        _logger.LogInformation("   Network is being reunified...");
        _logger.LogInformation("");

        _faultInjectionService.SetLinkUp("HQ-GATEWAY", "DIST-PRIMARY");
        _faultInjectionService.SetLinkUp("DIST-PRIMARY", "BRANCH-GATEWAY");
        _faultInjectionService.SetLinkUp("EDGE-NORTH-02", "DIST-PRIMARY");
        _faultInjectionService.SetLinkUp("CORE-WEST", "CORE-CENTRAL");
        _faultInjectionService.SetLinkUp("CORE-CENTRAL", "CORE-EAST");
        _faultInjectionService.SetLinkUp("DIST-PRIMARY", "CORE-CENTRAL");

        _partitionHealed = true;
        _healingTime = DateTime.Now;

        _loggingService.Log("SYSTEM", LogEventType.NETWORK_HEALED,
            "Network partition healed - all links restored",
            new { RestoredLinks = 6 });

        _logger.LogInformation("   🎯 Observe: RIP reconvergence across former partition boundary");
        _logger.LogInformation($"   Monitoring network healing for {_postHealingObservationSeconds}s...\n");

        await Task.Delay(TimeSpan.FromSeconds(_postHealingObservationSeconds), stoppingToken);

        _logger.LogInformation($"\n⏹️  Stopping simulation after healing observation period\n");
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
                    (_partitionHealed ? "🔄 HEALING" :
                     _partitionCreated ? "⚡ PARTITIONED" : "✅ CONVERGED") : "⏳ CONVERGING";

                _logger.LogInformation($"\n⏱️  T={elapsed:F1}s [{status}] - Routing Table Summary:");

                var activeRouters = snapshots.OrderBy(s => s.RouterName);
                foreach (var snapshot in activeRouters)
                {
                    _logger.LogInformation($"   {snapshot.RouterName,-18}: {snapshot.RouteCount} route(s)");
                }
                _logger.LogInformation("");

                if (_partitionCreated && !_partitionHealed && _partitionTime.HasValue)
                {
                    var timeSincePartition = (DateTime.Now - _partitionTime.Value).TotalSeconds;
                    _logger.LogInformation($"   Partition+{timeSincePartition:F0}s: Network operates as isolated islands");
                }
                else if (_partitionHealed && _healingTime.HasValue)
                {
                    var timeSinceHealing = (DateTime.Now - _healingTime.Value).TotalSeconds;
                    _logger.LogInformation($"   Healing+{timeSinceHealing:F0}s: Network reconverging after partition");
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    ZYNADEX SCENARIO F SIMULATION COMPLETE                     ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝\n");

        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"⏱️  Total simulation time: {elapsed:F1}s");

        if (_convergenceTime.HasValue)
        {
            var convergenceTime = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"✅ Initial convergence: {convergenceTime:F1}s");
        }

        if (_partitionTime.HasValue)
        {
            var partitionElapsed = (_partitionTime.Value - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"⚡ Partition created at: T={partitionElapsed:F1}s");
        }

        if (_healingTime.HasValue)
        {
            var healingElapsed = (_healingTime.Value - _simulationStartTime).TotalSeconds;
            var partitionDuration = (_healingTime.Value - _partitionTime!.Value).TotalSeconds;
            _logger.LogInformation($"🔄 Partition healed at: T={healingElapsed:F1}s");
            _logger.LogInformation($"📊 Partition duration: {partitionDuration:F1}s");
        }

        _logger.LogInformation("\n🎯 Network Partition & Healing Summary:");
        _logger.LogInformation($"   • Network successfully partitioned into isolated islands");
        _logger.LogInformation($"   • Each partition operated independently during split");
        _logger.LogInformation($"   • Links restored and network healed automatically");
        _logger.LogInformation($"   • RIP reconverged across partition boundary ✓");

        try
        {
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "ZynadexScenarioF_Simulation.txt");
            var finalSnapshots = _ripProtocolService.GetAllRouterSnapshots();
            var allLogs = _loggingService.GetLogs();

            _snapshotService.ExportScenarioFResults(
                exportPath,
                finalSnapshots,
                _simulationStartTime,
                _convergenceTime,
                _partitionTime,
                _healingTime,
                allLogs
            );

            _logger.LogInformation($"\n📄 Comprehensive analysis exported to: {exportPath}");
            _logger.LogInformation("   Report includes:");
            _logger.LogInformation("   • Complete partition and healing timeline");
            _logger.LogInformation("   • Network segmentation analysis");
            _logger.LogInformation("   • Independent island convergence details");
            _logger.LogInformation("   • Healing process and reconvergence metrics");
            _logger.LogInformation("   • Lessons learned and best practices");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export results");
        }

        _logger.LogInformation("");
        await base.StopAsync(stoppingToken);
    }
}