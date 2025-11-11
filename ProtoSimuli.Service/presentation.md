---
marp: true
theme: default
paginate: true
backgroundColor: #fff
backgroundImage: url('https://marp.app/assets/hero-background.svg')
---

<!-- Title Slide -->
# Routing Information Protocol Simulation

Bubenshi Mike

November 12, 2025

---

<!-- Agenda -->
## Agenda

1. **RIP Protocol Overview**
2. **Zynadex Corporate Network**
3. **Scenario A:** Baseline Convergence
4. **Scenario B:** Count-to-Infinity Problem
5. **Scenario C:** Split Horizon Solution
6. **Scenario D:** Poison Reverse Optimization
7. **Results & Conclusions**

---

<!-- Section: Introduction -->
# Part 1: Introduction to RIP

---

## What is RIP?

**Routing Information Protocol (RIP)** - Interior Routing Protocol using hop-count metric

**Key Concepts:**
- Interior Routing Protocol (IRP) within single Autonomous System (AS)
- Uses **hop-count** as routing metric
- Based on **Distance-Vector** algorithm

**Analogy:**
- **AS** = a town with its own internal road system
- **IRP** = local traffic rules that help you navigate within your town

---

## The Core Mechanism

### Distance-Vector Routing

**Basic Principle:**
Each router maintains a **routing table** ("vector") listing the distance (hop count) to every known destination

**Information Sharing:**
Routers exchange distance vectors with directly connected neighbors only

**The Three Key Vectors:**
1. **Link Cost Vector** - Cost to each neighbor
2. **Distance Vector** - Current total cost to destination
3. **Next-Hop Vector** - Address of next router

---

## RIP Characteristics

### 1. Metric: Hop Count
Cost = sum of hops; each router crossed = 1 hop

### 2. Infinity: 16 Hops
Maximum 15 hops; metric 16 = unreachable (infinity)

### 3. Incremental Updates
Processes updates as they arrive (no synchronization)

### 4. Route Timeout
Route invalid if no updates for 180 seconds

### 5. Route Flush
Route removed after additional 120 seconds (total 240s)

---

## RIP Timers

| Timer | Duration | Purpose |
|-------|----------|---------|
| **Update Timer** | 30 sec | Periodic routing table exchange |
| **Invalid Timer** | 180 sec | Mark route invalid, set metric=16 |
| **Hold-Down Timer** | 180 sec | Wait for neighbor response |
| **Flush Timer** | 60 sec | Remove route after invalid |

---

<!-- The Problem -->
# The Count-to-Infinity Problem

---

## What is Count-to-Infinity?

**Problem:**
Routers **slowly increment hop count to 16** after route becomes invalid

**Result:**
Slow convergence - network takes ~5 minutes to stabilize

> **Convergence** = state where all routers have accurate, up-to-date routing information

---

## How It Happens

**Scenario: Router D fails**

1. Router B notices, marks route via D as "down"
2. Before B advertises this, it receives update from Router A: "I can reach D in 3 hops!"
3. B believes A, updates to: "I can reach D via A in 4 hops"
4. B advertises to A - A updates to 5 hops
5. **Loop continues: 5?6?7?8...?16**

**Root Cause:** Routers believe each other's outdated information

---

## The Solution: Split Horizon

**Split Horizon Rule:**
> Never advertise a route back to the interface from which it was learned

**If Router B learned about route from Router A:**
? Router B should NOT advertise that route back to Router A

**Result:** Breaks the routing loop!

---

<!-- Section: Zynadex Network -->
# Part 2: Zynadex Corporate Network

---

## Network Overview

**Zynadex Corporation**
- 9-router enterprise network
- Headquarters, branches, edge locations, core infrastructure
```
  EDGE-NORTH-01 ??? EDGE-NORTH-02 ??? EDGE-NORTH-03
        ?                 ?                   ?
   HQ-GATEWAY ??????? DIST-PRIMARY ?????? BRANCH-GATEWAY
        ?                 ?                   ?
   CORE-WEST ??????? CORE-CENTRAL ???????? CORE-EAST
```

---

## Router Details

| Router | Role | Network |
|--------|------|---------|
| **HQ-GATEWAY** | HQ Gateway | 192.168.1.0/24 |
| **BRANCH-GATEWAY** | Branch Gateway | 192.168.2.0/24 |
| **EDGE-NORTH-01/02/03** | Edge Networks | 192.168.11-13.0/24 |
| **DIST-PRIMARY** | Distribution | 192.168.20.0/24 |
| **CORE-WEST/CENTRAL/EAST** | Core | 192.168.31-33.0/24 |

**Total:** 9 routers, 9 networks

---

## Simulation Architecture

**Technology Stack:**
- ASP.NET Core 8.0
- C# 12.0
- Background Services

**Core Components:**
- Topology Service
- RIP Protocol Service (Bellman-Ford)
- Logging Service
- Snapshot Service
- Fault Injection Service

---

<!-- Section: Scenarios -->
# Part 3: Simulation Scenarios

---

<!-- Scenario A -->
# Scenario A: Baseline Convergence

---

## Scenario A: Objective

**Goal:** Establish baseline performance metrics under normal conditions

**Configuration:**
```
- ? Split Horizon: ENABLED
? Poison Reverse: DISABLED
??  Update Interval: 30 seconds
```

**Measuring:**
- Initial convergence time
- Routing table stability
- Network efficiency

---

## Scenario A: Timeline

**Phase 1: Initialization (T=0s)**
- 9 routers with direct networks only

**Phase 2: Triggered Updates (T=1s)**
- Initial route advertisements

**Phase 3: Convergence (T=30-60s)**
- Distance-vector algorithm propagates routes
- Network achieves full convergence

**Phase 4: Steady State (T=60s+)**
- Periodic updates every 30s
- Stable operation

---

## Scenario A: Results

**Convergence Metrics:**
- **Time to Convergence:** ~60 seconds ?
- **Routes per Router:** 9 total (8 remote + 1 direct)
- **Update Cycles:** 2-3 cycles
- **Status:** Clean convergence, no issues

**Conclusion:**
Baseline established - RIP operating normally under ideal conditions

---

<!-- Scenario B -->
# Scenario B: Count-to-Infinity Problem

---

## Scenario B: Objective

**Goal:** Demonstrate count-to-infinity by DISABLING Split Horizon

**Configuration:**
```
? Split Horizon: DISABLED (intentionally!)
? Poison Reverse: DISABLED
?? Fault: HQ-GATEWAY router failure
```

**Why This Matters:**
Shows WHY Split Horizon is essential

---

## Scenario B: The Fault

**T=90s: HQ-GATEWAY FAILS**
```
?? Network 192.168.1.0/24 unreachable
   3 direct connections lost
```

**What Happens:**
1. EDGE-NORTH-01 detects failure
2. CORE-WEST still has route (metric 1)
3. DIST-PRIMARY hears from CORE-WEST: "I can reach it!"
4. DIST-PRIMARY updates to metric 3 via CORE-WEST
5. CORE-WEST hears from DIST-PRIMARY: "I can reach it!"
6. **Routing Loop Begins:** 3?4?5?6...?16

---

## Scenario B: The Count-to-Infinity

**Metric Progression:**
```
T=90s:  HQ-GATEWAY fails
T=120s: metric=3 (loop detected)
T=150s: metric=4
T=180s: metric=5
T=210s: metric=6
...
T=360s: metric=11
T=390s: metric=16 (INFINITY - finally!)
```

**Total Time:** ~300 seconds (5 minutes!)

---

## Scenario B: Impact

**Performance Metrics:**

| Metric | Value | Impact |
|--------|-------|--------|
| Convergence Time | ~300 sec | 5x slower |
| Update Cycles | ~10 cycles | Excessive |
| Bandwidth Usage | High | 10x normal |
| Route Oscillations | Yes | Unstable |

**Real-World Consequences:**
- 5 minutes of network instability
- Packets bouncing between routers
- Wasted bandwidth & CPU
- Poor user experience

---

<!-- Scenario C -->
# Scenario C: Split Horizon Solution

---

## Scenario C: Objective

**Goal:** Demonstrate how Split Horizon PREVENTS count-to-infinity

**Configuration:**
```
- ? Split Horizon: ENABLED (THE FIX!)
? Poison Reverse: DISABLED
?? Fault: CORE-CENTRAL router failure
```

**The Fix:**
> "Never advertise a route back out the interface from which it was learned"

---

## Scenario C: How Split Horizon Works

**T=90s: CORE-CENTRAL FAILS**

**What Happens:**
1. DIST-PRIMARY detects failure
2. DIST-PRIMARY marks routes invalid (metric 16)
3. **Split Horizon Check:**
   - Did I learn route from CORE-WEST? NO
   - Did I learn from CORE-CENTRAL? YES
   - **Action: BLOCK advertisement**
4. CORE-WEST stops receiving updates
5. CORE-WEST's route times out (180s)
6. Route flushed (240s)

**NO COUNT-TO-INFINITY!** ?

---

## Scenario C: Visual Comparison

**Scenario B (No Split Horizon):**
```
CORE-WEST ?----? DIST-PRIMARY
  (metric 3) ? (metric 3)    Loop!
  (metric 4) ? (metric 4)    Loop!
  ...continues to 16
```

**Scenario C (With Split Horizon):**
```
CORE-WEST        DIST-PRIMARY
  (metric 1)  ? [BLOCKED]
  ...wait 180s...
  (route timeout)
  Clean convergence! ?
```

---

## Scenario C: Results

**Performance Comparison:**

| Metric | Scenario B | Scenario C | Improvement |
|--------|------------|------------|-------------|
| Convergence | ~300 sec | ~180 sec | **40% faster** |
| Count-to-? | 10+ events | **0 events** | **100% prevented** |
| Bandwidth | High | Normal | **90% reduction** |
| Loops | Yes | **No** | **Stable** |

**Conclusion:**
Split Horizon is ESSENTIAL - must NEVER be disabled

---

<!-- Scenario D -->
# Scenario D: Poison Reverse Optimization

---

## Scenario D: Objective

**Goal:** Demonstrate how Poison Reverse ACCELERATES convergence

**Configuration:**
```
? Split Horizon: ENABLED
? Poison Reverse: ENABLED (OPTIMIZATION!)
?? Fault: CORE-EAST router failure
```

**The Enhancement:**
Instead of blocking routes ? **Actively advertise with metric 16**

---

## Scenario D: The Difference

**Scenario C (Passive):**
- Don't tell neighbors about failed routes
- Wait for timeout (180 seconds)

**Scenario D (Active):**
- **Actively advertise** routes with metric 16
- Neighbors invalidate **IMMEDIATELY**
- Convergence in ONE update cycle (~30s)

---

## Scenario D: Poison in Action

**T=90s: CORE-EAST FAILS**

1. CORE-CENTRAL detects failure
2. Marks 192.168.33.0/24 invalid (metric 16)
3. **Poison Reverse:** Actively advertises metric 16
4. DIST-PRIMARY receives poison
5. DIST-PRIMARY **IMMEDIATELY invalidates**
6. DIST-PRIMARY propagates poison
7. **Cascade:** Poison spreads in ONE cycle

**Convergence: 30 seconds!** ?

---

## Scenario D: Complete Comparison

| Scenario | Config | Time | Count-to-? | Result |
|----------|--------|------|------------|--------|
| **A (Baseline)** | Split | ~60s | 0 | Normal |
| **B (Problem)** | None | ~300s | 10+ | **BAD** |
| **C (Protected)** | Split | ~180s | 0 | Good |
| **D (Optimized)** | Split+Poison | **~30s** | 0 | **BEST** |

**Speed Improvements:**
- B ? D: **10x faster** (300s ? 30s)
- C ? D: **6x faster** (180s ? 30s)

---

## Scenario D: Trade-offs

**Advantages:**
- ? Fastest convergence (3-6x faster)
- ? Immediate failure notification
- ? Proactive communication

**Disadvantages:**
? Increased bandwidth (~20-30%)
? More processing overhead
? Update storm risk in large networks

**Trade-off Analysis:**
20% bandwidth increase for 600% speed improvement = **Excellent ROI**

---

## When to Use Each Approach

**Split Horizon Only (C):**
- Bandwidth-constrained links (WAN, satellite)
- Large networks with many routes
- Branch office deployments

**Poison Reverse (D):**
- Enterprise networks (like Zynadex)
- Data center networks
- Critical infrastructure
- Fast convergence required

**Zynadex Choice:** Poison Reverse  ?
(9 routers, good bandwidth, critical applications)

---

<!-- Supplementary -->
# Supplementary Scenarios

---

## Scenario E: Link Flapping

**Problem:** Intermittent link failures (loose cable)

**Solution:** Hold-Down Timer

**Without Hold-Down:**
```
Link UP ? advertise
Link DOWN ? remove
Link UP ? advertise
Result: Route THRASHING
```

**With Hold-Down:**
```
Link DOWN ? mark invalid, START HOLD-DOWN
Link UP ? REJECT (hold-down active)
Result: Route STABLE ?
```

---

## Scenario F: Network Partition

**Problem:** Catastrophic network split (6 links severed)

**Result:**
- 3 isolated network islands
- Each operated independently
- Automatic healing when links restored
- Full convergence in ~60 seconds

**Simulates:**
- Data center interconnect failure
- Fiber cut
- Disaster scenarios

---

<!-- Results & Conclusions -->
# Results & Conclusions

---

## Complete Performance Summary

| Scenario | Configuration | Convergence | Events | Verdict |
|----------|---------------|-------------|--------|---------|
| **A** | Split Horizon | 60s | 0 | Baseline |
| **B** | None | 300s | 10+ | ? Never Use |
| **C** | Split Horizon | 180s | 0 | - ? Safe |
| **D** | Split + Poison | 30s | 0 | - ? **Best** |
| **E** | Split + Hold-Down | Stable | 0 | Resilient |
| **F** | Split + Poison | 60s heal | 0 | Disaster Ready |

---

## Key Findings

### 1. Count-to-Infinity is Real
? **Problem:** 5-minute convergence, routing loops
- ? **Solution:** Split Horizon (essential)

### 2. Split Horizon Essential
- ? 100% prevention of count-to-infinity
- ? 40% faster convergence than without
- ? **Must NEVER be disabled in production**

### 3. Poison Reverse Optimal
- ? 6x faster than Split Horizon alone
- ? Acceptable bandwidth cost (~20%)
- ? **Best for enterprise networks**

---

## Zynadex Network Recommendation

**Chosen Configuration:**
```
 ? Split Horizon: ENABLED (essential)
 ? Poison Reverse: ENABLED (optimal)
 ? Hold-Down Timer: 180s (standard)
 ??  Update Interval: 30s
```

**Justification:**
- 9-router network (small enough for Poison Reverse)
- Critical business applications
- Good bandwidth availability
- 6x speed improvement worth 20% bandwidth cost

---

## Real-World Guidelines

**For Network Engineers:**
1. ? Always enable Split Horizon
2.  ? Consider Poison Reverse for < 50 routers
3. ? Keep hold-down timer enabled
4.  ?? Never disable loop prevention
5. ?? Test convergence in your topology

**For Production:**
- Enterprise networks: Poison Reverse
- WAN/Satellite: Split Horizon only
- Data centers: Poison Reverse
- Branch offices: Split Horizon only

---

## Academic Validation

**Theory ? Practice:**
- ? Count-to-infinity validated (not just theoretical)
- ? Split Horizon mechanism proven effective
- ? Poison Reverse optimization quantified
- ? RFC 2453 compliance verified

**Quantitative Results:**
- Precise convergence measurements
- Bandwidth impact analysis
- Performance across configurations
- Real-world applicability

---

## Technical Achievements

**Implementation:**
- Full RIP protocol (RFC 2453)
- Bellman-Ford algorithm
- Complete timer management
- Fault injection framework
- 9-router mesh topology

**Code Quality:**
- Clean architecture
- SOLID principles
- Dependency injection
- Comprehensive logging
- Automated reporting

---

## Lessons Learned

### The Progressive Solution

**Problem** (Scenario B):
Count-to-infinity ? 300 seconds

**Solution** (Scenario C):
Split Horizon ? 180 seconds

**Optimization** (Scenario D):
Poison Reverse ? 30 seconds

**Result:** **10x performance improvement!**

---

## Future Work

**Potential Extensions:**
1. RIPv2 authentication
2. Route summarization
3. VLSM support
4. Real-time visualization
5. RIP vs OSPF comparison
6. Multiple failure scenarios

---

# Conclusions

---

## Project Summary

**Successfully:**
- ? Implemented complete RIP simulator
- ? Demonstrated count-to-infinity problem
- ? Validated Split Horizon solution
- ? Optimized with Poison Reverse
- ? Extended to real-world scenarios

**Key Insight:**
Proper configuration = **10x performance improvement**

---

## Final Recommendations

**Essential (Non-Negotiable):**
- ? Always enable Split Horizon
- ? Never disable loop prevention
- ? Monitor convergence times

**Recommended (Network-Dependent):**
- ? Poison Reverse for enterprise (< 50 routers)
- ? Hold-down for link stability
- ? Triggered updates for fast propagation

**Best Practice:**
Test in lab ? Measure baseline ? Deploy with confidence

---

# Q&A

**Contact:**
- Name: Mike Bubenshi
- Email: [mikebubenshi@gmail.com](mailto:mikebubenshi@gmail.com)
- GitHub: [Protocol Simulator](https://github.com/Bubenshi-Mike/Proto-Simuli)

**"Validating RIP Protocol Behavior Through Simulation"**

---