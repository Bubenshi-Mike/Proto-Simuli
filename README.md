# RIP Protocol Simulator - README

## Overview

A **complete RIP (Routing Information Protocol) simulator** built with ASP.NET Core 8.0 that demonstrates distance-vector routing behavior, convergence patterns, and loop prevention mechanisms in a 9-router corporate network.

### Key Features

- ✅ Full RFC 2453 RIP implementation (Distance-Vector algorithm)
- ✅ 9-router Zynadex corporate network topology
- ✅ Split Horizon & Poison Reverse mechanisms
- ✅ Fault injection for failure scenarios
- ✅ Real-time routing table snapshots
- ✅ Comprehensive event logging
- ✅ Automated scenario execution (A through F)
- ✅ Professional report generation

---

## Quick Start

### Prerequisites
```bash
- .NET 8.0 SDK
- Visual Studio 2022 / VS Code / Rider
- Windows/Linux/macOS
```

### Run the Simulator
```bash
# Clone the repository
git clone [your-repo-url]
cd RipProtocolSimulator

# Restore packages
dotnet restore

# Run the application
dotnet run --project RipProtocolSimulator.Api

# Navigate to
http://localhost:5000/swagger
```

### Run Scenarios
```bash
# Execute all scenarios
POST /api/simulation/scenarios/run-all

# Execute specific scenario
POST /api/simulation/scenarios/run/{scenarioId}
# scenarioId: A, B, C, D, E, or F

# Get scenario results
GET /api/simulation/scenarios/{scenarioId}/results

# Export scenario report
GET /api/simulation/scenarios/{scenarioId}/export
```

---

## Project Structure
```
RipProtocolSimulator/
├── Core/
│   ├── Models/              # Domain models
│   │   ├── Router.cs        # Router entity
│   │   ├── RoutingTableEntry.cs
│   │   ├── Link.cs          # Network link
│   │   └── NetworkTopology.cs
│   ├── Enums/
│   │   ├── LogEventType.cs  # Event types
│   │   └── RouterStatus.cs
│   └── DTOs/                # Data Transfer Objects
│       ├── RouterSnapshotDto.cs
│       └── LogEntryDto.cs
│
├── Services/
│   ├── TopologyService.cs           # Network graph management
│   ├── RipProtocolService.cs        # Core RIP algorithm
│   ├── LoggingService.cs            # Event tracking
│   ├── SnapshotService.cs           # State capture
│   ├── FaultInjectionService.cs     # Failure scenarios
│   └── ReportExportService.cs       # Report generation
│
├── BackgroundServices/
│   ├── ScenarioAService.cs          # Baseline convergence
│   ├── ScenarioBService.cs          # Count-to-infinity
│   ├── ScenarioCService.cs          # Split Horizon
│   ├── ScenarioDService.cs          # Poison Reverse
│   ├── ScenarioEService.cs          # Link flapping
│   └── ScenarioFService.cs          # Network partition
│
├── Controllers/
│   └── SimulationController.cs      # REST API endpoints
│
└── Program.cs                       # Application entry point
```

---

## Architecture Overview

### Layer Diagram
```
┌─────────────────────────────────────────────────┐
│           API Layer (Controllers)               │
│  - SimulationController                         │
│  - Swagger/OpenAPI endpoints                    │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│        Background Services Layer                │
│  - ScenarioA/B/C/D/E/F Services                │
│  - Orchestrate scenario execution              │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│           Business Logic Layer                  │
│  - RipProtocolService (Bellman-Ford)           │
│  - TopologyService (Network graph)             │
│  - FaultInjectionService                       │
│  - SnapshotService                             │
│  - LoggingService                              │
│  - ReportExportService                         │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│              Domain Layer                       │
│  - Models (Router, Link, Topology)             │
│  - DTOs (Snapshots, Logs)                      │
│  - Enums (EventType, Status)                   │
└─────────────────────────────────────────────────┘
```

---

## Core Components

### 1. RipProtocolService

**The heart of the simulator** - implements Distance-Vector routing algorithm.
```csharp
public class RipProtocolService
{
    // Core RIP operations
    public void ProcessUpdate(Router router, RouterUpdate update)
    {
        // Bellman-Ford distance-vector algorithm
        foreach (var route in update.Routes)
        {
            var newMetric = route.Metric + 1; // Add one hop
            
            // Split Horizon check
            if (IsSplitHorizonEnabled && LearnedFromThisNeighbor(route))
                continue;
            
            // Update if better route
            if (newMetric < currentMetric)
                UpdateRoutingTable(route);
        }
    }
    
    public void SendPeriodicUpdate(Router router)
    {
        // Every 30 seconds
        foreach (var neighbor in router.Neighbors)
        {
            var update = PrepareUpdate(router, neighbor);
            
            // Poison Reverse: advertise unreachable with metric 16
            if (IsPoisonReverseEnabled)
                AddPoisonedRoutes(update, neighbor);
            
            SendUpdate(neighbor, update);
        }
    }
}
```

**Key Methods:**
- `ProcessUpdate()` - Handle incoming routing updates
- `SendPeriodicUpdate()` - 30-second update timer
- `SendTriggeredUpdate()` - Immediate updates on changes
- `InvalidateRoute()` - Mark route unreachable (metric 16)
- `FlushRoute()` - Remove expired routes

---

### 2. TopologyService

**Manages the network graph** - routers, links, and connections.
```csharp
public class TopologyService
{
    public NetworkTopology CreateZynadexTopology()
    {
        var topology = new NetworkTopology();
        
        // Create 9 routers
        var hqGateway = CreateRouter("HQ-GATEWAY", "192.168.1.0/24");
        var branchGateway = CreateRouter("BRANCH-GATEWAY", "192.168.2.0/24");
        // ... 7 more routers
        
        // Create links (mesh topology)
        CreateLink(hqGateway, edgeNorth01);
        CreateLink(hqGateway, distPrimary);
        CreateLink(hqGateway, coreWest);
        // ... 24 total links
        
        return topology;
    }
    
    public void DisableLink(string routerA, string routerB)
    {
        // Fault injection
        var link = FindLink(routerA, routerB);
        link.IsActive = false;
    }
}
```

**Zynadex Network Topology:**
```
  EDGE-NORTH-01 ─── EDGE-NORTH-02 ─── EDGE-NORTH-03
        │                 │                   │
   HQ-GATEWAY ─────── DIST-PRIMARY ────── BRANCH-GATEWAY
        │                 │                   │
   CORE-WEST ─────── CORE-CENTRAL ──────── CORE-EAST
```

---

### 3. Background Services

**Each scenario runs as a hosted background service** - enables parallel execution and isolation.
```csharp
public class ScenarioCService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Phase 1: Initialize network
        var topology = _topologyService.CreateZynadexTopology();
        
        // Phase 2: Wait for convergence
        await WaitForConvergence(60); // ~60 seconds
        
        // Phase 3: Inject fault
        await Task.Delay(30000); // Wait 30s after convergence
        _faultInjection.DisableRouter("CORE-CENTRAL");
        
        // Phase 4: Observe recovery
        await WaitForConvergence(180); // ~180 seconds
        
        // Phase 5: Capture results
        var snapshots = _snapshotService.CaptureAll();
        var logs = _loggingService.GetLogs();
        
        // Phase 6: Generate report
        _reportService.ExportScenarioCResults(snapshots, logs);
    }
}
```

---

### 4. LoggingService

**Comprehensive event tracking** for analysis and debugging.
```csharp
public enum LogEventType
{
    SIMULATION_START,
    SIMULATION_END,
    ROUTE_LEARNED,
    ROUTE_CHANGED,
    ROUTE_INVALIDATED,
    ROUTE_FLUSHED,
    UPDATE_SENT,
    UPDATE_RECEIVED,
    COUNT_TO_INFINITY_DETECTED,
    SPLIT_HORIZON_BLOCKED,
    POISON_REVERSE_SENT,
    HOLD_DOWN_START,
    LINK_DOWN,
    LINK_UP,
    CONVERGED
}

public void Log(string routerName, LogEventType eventType, string message)
{
    _logs.Add(new LogEntry
    {
        Timestamp = DateTime.UtcNow,
        RouterName = routerName,
        EventType = eventType,
        Message = message
    });
}
```

---

### 5. SnapshotService

**Captures routing table state** at specific moments for analysis.
```csharp
public class SnapshotService
{
    public List<RouterSnapshot> CaptureAll(NetworkTopology topology)
    {
        return topology.Routers.Select(router => new RouterSnapshot
        {
            RouterName = router.Name,
            Timestamp = DateTime.UtcNow,
            RoutingTable = router.RoutingTable.Select(entry => new RouteDto
            {
                DestinationNetwork = entry.Destination,
                NextHop = entry.NextHop,
                Metric = entry.Metric,
                LastUpdated = entry.LastUpdated
            }).ToList()
        }).ToList();
    }
}
```

---

## Scenario Descriptions

### Scenario A: Baseline Convergence
**Objective:** Measure normal convergence with Split Horizon enabled

**Configuration:**
- ✅ Split Horizon: ON
- ❌ Poison Reverse: OFF
- ⏱️ Expected convergence: ~60 seconds

**What happens:** Network converges cleanly from initialization

---

### Scenario B: Count-to-Infinity Problem
**Objective:** Demonstrate routing loop by disabling Split Horizon

**Configuration:**
- ❌ Split Horizon: OFF (intentionally!)
- ❌ Poison Reverse: OFF
- 🎯 Fault: HQ-GATEWAY router failure

**What happens:** Routing loop occurs, metrics increment 3→4→5...→16 over ~300 seconds

---

### Scenario C: Split Horizon Solution
**Objective:** Show how Split Horizon prevents count-to-infinity

**Configuration:**
- ✅ Split Horizon: ON (the fix!)
- ❌ Poison Reverse: OFF
- 🎯 Fault: CORE-CENTRAL router failure

**What happens:** No routing loop, convergence via timeout in ~180 seconds

---

### Scenario D: Poison Reverse Optimization
**Objective:** Demonstrate accelerated convergence with Poison Reverse

**Configuration:**
- ✅ Split Horizon: ON
- ✅ Poison Reverse: ON (optimization!)
- 🎯 Fault: CORE-EAST router failure

**What happens:** Active poison propagation, convergence in ~30 seconds (6x faster!)

---

### Scenario E: Link Flapping
**Objective:** Test hold-down timer against intermittent failures

**Configuration:**
- ✅ Split Horizon: ON
- ✅ Hold-Down Timer: 180s
- 🎯 Fault: DIST-PRIMARY ↔ BRANCH-GATEWAY link flaps 5 times

**What happens:** Hold-down dampens oscillations, maintains route stability

---

### Scenario F: Network Partition
**Objective:** Validate behavior during catastrophic split-brain scenario

**Configuration:**
- ✅ Split Horizon: ON
- ✅ Poison Reverse: ON
- 🎯 Fault: 6 links severed, creating 3 isolated islands

**What happens:** Independent operation during partition, automatic healing when restored

---

## API Endpoints

### Simulation Control
```http
# Run all scenarios sequentially
POST /api/simulation/scenarios/run-all
Response: 202 Accepted

# Run specific scenario
POST /api/simulation/scenarios/run/{scenarioId}
Parameters:
  - scenarioId: A, B, C, D, E, or F
Response: 202 Accepted

# Get scenario status
GET /api/simulation/scenarios/{scenarioId}/status
Response: { "status": "Running|Completed|Failed", ... }
```

### Results & Reports
```http
# Get scenario results
GET /api/simulation/scenarios/{scenarioId}/results
Response: {
  "convergenceTime": 60.5,
  "routingTables": [...],
  "eventLogs": [...]
}

# Export scenario report
GET /api/simulation/scenarios/{scenarioId}/export
Response: Text file with comprehensive analysis

# Get all routing tables snapshot
GET /api/simulation/topology/snapshot
Response: Current state of all router routing tables
```

### Topology Management
```http
# Get network topology
GET /api/simulation/topology
Response: Full network graph with routers and links

# Inject fault
POST /api/simulation/fault/inject
Body: {
  "type": "RouterFailure|LinkDown",
  "target": "CORE-CENTRAL"
}
```

---

## Configuration

### appsettings.json
```json
{
  "RipConfiguration": {
    "UpdateInterval": 30,
    "InvalidTimer": 180,
    "FlushTimer": 240,
    "HoldDownTimer": 180,
    "EnableSplitHorizon": true,
    "EnablePoisonReverse": false,
    "EnableTriggeredUpdates": true,
    "MaxMetric": 16
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "RipProtocol": "Debug"
    }
  }
}
```

---

## Key Algorithms

### Bellman-Ford Distance-Vector
```csharp
// Simplified core algorithm
foreach (var neighbor in router.Neighbors)
{
    foreach (var route in neighbor.AdvertisedRoutes)
    {
        var newMetric = route.Metric + 1; // Add one hop
        var currentRoute = router.RoutingTable.Find(route.Destination);
        
        if (currentRoute == null || newMetric < currentRoute.Metric)
        {
            // Better route found - update
            router.RoutingTable.Update(
                destination: route.Destination,
                nextHop: neighbor,
                metric: newMetric
            );
            
            // Trigger immediate update to other neighbors
            SendTriggeredUpdate(router, route.Destination);
        }
    }
}
```

### Split Horizon Logic
```csharp
// When preparing update for neighbor
var routesToAdvertise = new List<Route>();

foreach (var route in router.RoutingTable)
{
    // Check if route was learned from this neighbor
    if (route.LearnedFromInterface == neighborInterface)
    {
        if (EnablePoisonReverse && route.Metric >= 16)
        {
            // Poison Reverse: advertise unreachable
            routesToAdvertise.Add(new Route(route.Destination, 16));
        }
        else
        {
            // Split Horizon: don't advertise back
            continue;
        }
    }
    else
    {
        // Normal advertisement
        routesToAdvertise.Add(route);
    }
}
```

---

## Testing

### Run Unit Tests
```bash
dotnet test RipProtocolSimulator.Tests
```

### Test Scenarios
```csharp
[Fact]
public async Task ScenarioB_ShouldDetectCountToInfinity()
{
    // Arrange
    var service = new ScenarioBService(...);
    
    // Act
    await service.ExecuteAsync(CancellationToken.None);
    
    // Assert
    var logs = _loggingService.GetLogs();
    var countToInfinityEvents = logs.Count(
        l => l.EventType == LogEventType.COUNT_TO_INFINITY_DETECTED
    );
    
    Assert.True(countToInfinityEvents > 0);
}
```

---

## Performance Benchmarks

| Scenario | Convergence Time | Update Cycles | Bandwidth |
|----------|-----------------|---------------|-----------|
| A (Baseline) | ~60s | 2-3 | Normal |
| B (Problem) | ~300s | 10+ | High |
| C (Split Horizon) | ~180s | 0 (timeout) | Normal |
| D (Poison Reverse) | ~30s | 1 | Medium |

---

## Troubleshooting

### Common Issues

**Issue:** Scenarios not executing
```bash
# Check background services are registered
# In Program.cs:
builder.Services.AddHostedService<ScenarioAService>();
```

**Issue:** Convergence takes too long
```bash
# Adjust timers in appsettings.json
"UpdateInterval": 10,  // Faster updates (default: 30)
```

**Issue:** Count-to-infinity not detected in Scenario B
```bash
# Ensure Split Horizon is disabled in ScenarioBService:
_ripProtocol.EnableSplitHorizon = false;
```

---

## Contributing

### Code Style

- Follow C# naming conventions
- Use async/await for I/O operations
- Add XML documentation comments
- Write unit tests for new features

### Adding New Scenarios

1. Create new `ScenarioXService : BackgroundService`
2. Implement `ExecuteAsync()` method
3. Register in `Program.cs`
4. Add export method in `ReportExportService`
5. Update API controller

---

## License

MIT License - See LICENSE file for details

---

## References

- **RFC 2453:** RIP Version 2
- **RFC 1058:** RIP Version 1
- Tanenbaum, "Computer Networks", 6th Edition
- Kurose & Ross, "Computer Networking: A Top-Down Approach"

---

## Contact

**Author:** Bubenshi Mike  
**Project:** RIP Protocol Simulator  
**Year:** 2025

**For questions or issues:**
- GitHub Issues: [repository-url]/issues
- Email: [your-email]

---

## Acknowledgments

Special thanks to:
- Zynadex Corporation (fictional case study network)
- RFC authors for RIP specification
- ASP.NET Core team for excellent framework

---

**Happy Simulating! 🚀**
