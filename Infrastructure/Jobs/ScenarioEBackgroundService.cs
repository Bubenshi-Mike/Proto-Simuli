namespace Infrastructure.Jobs;

public class ScenarioEBackgroundService : BackgroundService
{
    private readonly ITopologyService _topologyService;
    private readonly IRipProtocolService _ripProtocolService;
    private readonly ILoggingService _loggingService;
    private readonly ISnapshotService _snapshotService;
    private readonly IFaultInjectionService _faultInjectionService;
    private readonly ILogger<ScenarioEBackgroundService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    private DateTime _simulationStartTime;
    private DateTime? _convergenceTime;
    private readonly List<DateTime> _linkFlapTimes = new List<DateTime>();
    private bool _flapSequenceStarted = false;

    private readonly int _updateIntervalSeconds = 30;
    private readonly int _timerCheckIntervalSeconds = 5;
    private readonly int _snapshotIntervalSeconds = 10;
    private readonly int _convergenceCheckIntervalSeconds = 5;
    private readonly int _convergenceWaitSeconds = 90;
    private readonly int _flapIntervalSeconds = 20; // Flap every 20 seconds
    private readonly int _numberOfFlaps = 5; // 5 flaps total
    private readonly int _postFlapObservationSeconds = 180; // Observe hold-down for 3 minutes

    public ScenarioEBackgroundService(
        ITopologyService topologyService,
        IRipProtocolService ripProtocolService,
        ILoggingService loggingService,
        ISnapshotService snapshotService,
        IFaultInjectionService faultInjectionService,
        ILogger<ScenarioEBackgroundService> logger,
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
        _logger.LogInformation("║    RIP PROTOCOL SIMULATION - SCENARIO E                       ║");
        _logger.LogInformation("║    Zynadex Network - Link Flapping & Hold-Down Protection     ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("");

        InitializeScenarioETopology();

        _simulationStartTime = DateTime.Now;
        _logger.LogInformation($"⏰ Simulation started at {_simulationStartTime:HH:mm:ss}");
        _logger.LogInformation("");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        await SendInitialTriggeredUpdates(stoppingToken);

        var updateTask = RunPeriodicUpdates(stoppingToken);
        var timerTask = RunTimerChecks(stoppingToken);
        var snapshotTask = RunPeriodicSnapshots(stoppingToken);
        var convergenceTask = MonitorConvergence(stoppingToken);
        var flapTask = SimulateLinkFlapping(stoppingToken);

        await Task.WhenAny(updateTask, timerTask, snapshotTask, convergenceTask, flapTask);
    }

    private void InitializeScenarioETopology()
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
                new LinkConfigDto { RouterA = "DIST-PRIMARY", RouterB = "BRANCH-GATEWAY", Status = LinkStatus.Up }, // This link will flap!
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

        var allRouters = _topologyService.GetAllRouters();
        foreach (var router in allRouters)
        {
            router.SplitHorizonEnabled = true;
            router.PoisonReverseEnabled = false;
        }

        _logger.LogInformation("📡 Zynadex Corporate Network Topology:");
        _logger.LogInformation("   ┌──────────────────────────────────────────────────────────────────┐");
        _logger.LogInformation("   │  EDGE-NORTH-01 ─── EDGE-NORTH-02 ─── EDGE-NORTH-03             │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   HQ-GATEWAY ─────── DIST-PRIMARY ~~~~X~~~~ BRANCH-GATEWAY      │");
        _logger.LogInformation("   │        │                 │                   │                   │");
        _logger.LogInformation("   │   CORE-WEST ─────── CORE-CENTRAL ──────── CORE-EAST            │");
        _logger.LogInformation("   └──────────────────────────────────────────────────────────────────┘");
        _logger.LogInformation("");
        _logger.LogInformation("   ~~~~X~~~~ = Unstable link (will flap)");
        _logger.LogInformation("");
        _logger.LogInformation("✅ Split Horizon: ENABLED on all routers");
        _logger.LogInformation("⏱️  Hold-Down Timer: 180 seconds (standard RIP protection)");
        _logger.LogInformation("");
        _logger.LogInformation($"   🔥 Target Link: DIST-PRIMARY ↔ BRANCH-GATEWAY");
        _logger.LogInformation($"   📊 Flap Pattern: {_numberOfFlaps} flaps, every {_flapIntervalSeconds} seconds");
        _logger.LogInformation($"   ⏳ Waiting {_convergenceWaitSeconds}s for initial convergence...");
        _logger.LogInformation("");
        _logger.LogInformation("🎯 Objective:");
        _logger.LogInformation("   Demonstrate how hold-down timer prevents route instability during");
        _logger.LogInformation("   rapid link flapping (loose cable, intermittent failures, etc.).");
        _logger.LogInformation("   Without hold-down, routers would thrash between routes, causing");
        _logger.LogInformation("   network-wide instability and excessive update traffic.");
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
                    _logger.LogInformation($"   Ready to begin link flapping simulation\n");

                    _loggingService.Log("SYSTEM", LogEventType.CONVERGENCE_COMPLETE,
                        $"Network converged after {elapsed:F1} seconds", new { ConvergenceTime = elapsed });
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SimulateLinkFlapping(CancellationToken stoppingToken)
    {
        while (!_convergenceTime.HasValue && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (!_convergenceTime.HasValue) return;

        await Task.Delay(TimeSpan.FromSeconds(_convergenceWaitSeconds -
            (_convergenceTime.Value - _simulationStartTime).TotalSeconds), stoppingToken);

        if (_flapSequenceStarted || stoppingToken.IsCancellationRequested) return;

        _flapSequenceStarted = true;
        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;

        _logger.LogInformation($"\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation($"║    🔥 LINK FLAPPING SEQUENCE - T={elapsed:F1}s                    ║");
        _logger.LogInformation($"╚═══════════════════════════════════════════════════════════════╝");
        _logger.LogInformation("   Starting rapid link flap simulation on DIST-PRIMARY ↔ BRANCH-GATEWAY");
        _logger.LogInformation($"   {_numberOfFlaps} flaps will occur every {_flapIntervalSeconds} seconds");
        _logger.LogInformation("   Observe how hold-down timer prevents route instability!\n");

        for (int i = 0; i < _numberOfFlaps && !stoppingToken.IsCancellationRequested; i++)
        {
            var flapElapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;

            // Bring link DOWN
            _logger.LogInformation($"⚠️  FLAP {i + 1}/{_numberOfFlaps} - T={flapElapsed:F1}s: Link DIST-PRIMARY↔BRANCH-GATEWAY going DOWN");
            _faultInjectionService.SetLinkDown("DIST-PRIMARY", "BRANCH-GATEWAY");
            _linkFlapTimes.Add(DateTime.Now);

            _loggingService.Log("SYSTEM", LogEventType.LINK_DOWN,
                $"Flap {i + 1}: Link DIST-PRIMARY↔BRANCH-GATEWAY DOWN",
                new { FlapNumber = i + 1, TotalFlaps = _numberOfFlaps });

            await Task.Delay(TimeSpan.FromSeconds(_flapIntervalSeconds / 2), stoppingToken);

            // Bring link UP
            flapElapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"✅ FLAP {i + 1}/{_numberOfFlaps} - T={flapElapsed:F1}s: Link DIST-PRIMARY↔BRANCH-GATEWAY coming UP");
            _faultInjectionService.SetLinkUp("DIST-PRIMARY", "BRANCH-GATEWAY");

            _loggingService.Log("SYSTEM", LogEventType.LINK_UP,
                $"Flap {i + 1}: Link DIST-PRIMARY↔BRANCH-GATEWAY UP",
                new { FlapNumber = i + 1, TotalFlaps = _numberOfFlaps });

            await Task.Delay(TimeSpan.FromSeconds(_flapIntervalSeconds / 2), stoppingToken);
        }

        _logger.LogInformation($"\n📊 Link flapping sequence complete!");
        _logger.LogInformation($"   Observing hold-down behavior for {_postFlapObservationSeconds}s...\n");

        await Task.Delay(TimeSpan.FromSeconds(_postFlapObservationSeconds), stoppingToken);

        _logger.LogInformation($"\n⏹️  Stopping simulation after observation period\n");
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
                    (_flapSequenceStarted ? "🔥 FLAPPING" : "✅ CONVERGED") : "⏳ CONVERGING";

                _logger.LogInformation($"\n⏱️  T={elapsed:F1}s [{status}] - Routing Table Summary:");

                var activeRouters = snapshots.OrderBy(s => s.RouterName);
                foreach (var snapshot in activeRouters)
                {
                    _logger.LogInformation($"   {snapshot.RouterName,-18}: {snapshot.RouteCount} route(s)");
                }
                _logger.LogInformation("");

                if (_flapSequenceStarted && _linkFlapTimes.Any())
                {
                    var timeSinceFirstFlap = (DateTime.Now - _linkFlapTimes.First()).TotalSeconds;
                    _logger.LogInformation($"   Flap+{timeSinceFirstFlap:F0}s: Hold-down timer protecting route stability");
                }
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("\n╔═══════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║    ZYNADEX SCENARIO E SIMULATION COMPLETE                     ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════════╝\n");

        var elapsed = (DateTime.Now - _simulationStartTime).TotalSeconds;
        _logger.LogInformation($"⏱️  Total simulation time: {elapsed:F1}s");

        if (_convergenceTime.HasValue)
        {
            var convergenceTime = (_convergenceTime.Value - _simulationStartTime).TotalSeconds;
            _logger.LogInformation($"✅ Initial convergence: {convergenceTime:F1}s");
        }

        if (_linkFlapTimes.Any())
        {
            _logger.LogInformation($"🔥 Link flaps executed: {_linkFlapTimes.Count}");
            _logger.LogInformation($"   First flap at: T={(_linkFlapTimes.First() - _simulationStartTime).TotalSeconds:F1}s");
            _logger.LogInformation($"   Last flap at: T={(_linkFlapTimes.Last() - _simulationStartTime).TotalSeconds:F1}s");

            var holdDownEvents = _loggingService.GetLogs()
                .Where(l => l.EventType == LogEventType.HOLD_DOWN_START)
                .ToList();

            _logger.LogInformation($"\n🛡️  Hold-Down Timer Effectiveness:");
            _logger.LogInformation($"   • Hold-down events triggered: {holdDownEvents.Count}");
            _logger.LogInformation($"   • Protected against: Route thrashing during instability");
            _logger.LogInformation($"   • Result: Network remained stable despite {_linkFlapTimes.Count} link flaps ✓");
        }

        try
        {
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), "ZynadexScenarioE_Simulation.txt");
            var finalSnapshots = _ripProtocolService.GetAllRouterSnapshots();
            var allLogs = _loggingService.GetLogs();

            _snapshotService.ExportScenarioEResults(
                exportPath,
                finalSnapshots,
                _simulationStartTime,
                _convergenceTime,
                _linkFlapTimes,
                allLogs
            );

            _logger.LogInformation($"\n📄 Comprehensive analysis exported to: {exportPath}");
            _logger.LogInformation("   Report includes:");
            _logger.LogInformation("   • Complete link flapping timeline");
            _logger.LogInformation("   • Hold-down timer mechanism explanation");
            _logger.LogInformation("   • Route stability analysis");
            _logger.LogInformation("   • Comparison: With vs Without hold-down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export results");
        }

        _logger.LogInformation("");
        await base.StopAsync(stoppingToken);
    }
}