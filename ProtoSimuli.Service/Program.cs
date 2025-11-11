using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

string choice;
do
{
    Console.Clear();
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                                                               ║");
    Console.WriteLine("║        ROUTING PROTOCOL SIMULATION SYSTEM                     ║");
    Console.WriteLine("║        Powered By Proto Simuli 3.0                            ║");
    Console.WriteLine("║                                                               ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    Console.WriteLine("Select Scenario:");
    Console.WriteLine();
    Console.WriteLine("  [A] Scenario A: Network Convergence (Happy Path)");
    Console.WriteLine("      • All routers converge to learn all networks");
    Console.WriteLine("      • Split Horizon ENABLED");
    Console.WriteLine("      • Demonstrates normal RIP operation");
    Console.WriteLine();
    Console.WriteLine("  [B] Scenario B: Count to Infinity Problem");
    Console.WriteLine("      • Split Horizon DISABLED on key routers");
    Console.WriteLine("      • Network failure causes count-to-infinity");
    Console.WriteLine("      • Metrics increment: 3→4→5...→16");
    Console.WriteLine();
    Console.WriteLine("  [C] Scenario C: Split Horizon Fix");
    Console.WriteLine("      • SAME failure as Scenario B");
    Console.WriteLine("      • Split Horizon ENABLED (the fix!)");
    Console.WriteLine("      • No count-to-infinity, routes timeout normally");
    Console.WriteLine();
    Console.WriteLine("  [D] Scenario D: Poison Reverse (Fastest!)");
    Console.WriteLine("      • Split Horizon + Poison Reverse ENABLED");
    Console.WriteLine("      • Explicitly advertises unreachable routes (metric 16)");
    Console.WriteLine("      • IMMEDIATE failure propagation");
    Console.WriteLine();
    Console.WriteLine("  [E] Scenario E: Link Flap Resilience & Triggered Updates");
    Console.WriteLine("      • Periodic link up/down events (link flaps)");
    Console.WriteLine("      • Triggered updates ENABLED for rapid propagation");
    Console.WriteLine("      • Measures recovery time and stability per flap");
    Console.WriteLine();
    Console.Write("Enter choice (A, B, C, D ,E or F): ");
    choice = Console.ReadLine()?.Trim().ToUpper();

    if (choice == "A" || choice == "B" || choice == "C" || choice == "D" || choice == "E")
    {
        break;
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("Invalid choice. Please enter 'A' or 'B' or 'C' or 'D' or 'E' or 'F'.");
        Console.WriteLine("Press any key to try again...");
        Console.ReadKey();
    }

} while (true);

Console.Clear(); // Optional: clean screen after valid selection

// Now safely proceed based on validated choice
switch (choice)
{
    case "B":
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Starting Scenario B: Count to Infinity Problem");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        break;

    case "C":
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Starting Scenario C: Split Horizon Fix");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        break;

    case "D":
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Starting Scenario D: Poison Reverse");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        break;

    case "E":
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Starting Scenario E: Link Flap Resilience & Triggered Updates");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        break;

    default:
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Starting Scenario A: Network Convergence");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        break;
}

Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop the simulation");
Console.WriteLine();

// Configure services and run
builder.Services.AddProtoSimuliService(builder.Configuration, choice);

var host = builder.Build();
host.Run();