// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using DataFence.AgentShield.SqlServer;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    /// <summary>
    /// The same scenarios on SQL Server. Records are written to the database, so they survive a restart and are shared
    /// by every server.
    /// Connection: the AGENTSHIELD_DEMO_SQL environment variable, or typed at the prompt. No credentials are stored in
    /// this demo.
    /// Use a test database. EnsureSchemaAsync creates the AgentShield tables if they do not exist.
    /// </summary>
    // STEPS TO USE SQL SERVER STORAGE
    //   Step 1. Create SqlServerActionLifecycleStore with your connection string and call EnsureSchemaAsync once at startup.
    //   Step 2. Assign the store to shield.RequestStatus instead of using DemoMode.
    //   Step 3. Pending approvals and results now survive a restart and are shared by every server.
    public static class SqlServerDemo
    {
        public static async Task StartDemo()
        {
            string? connectionString = Environment.GetEnvironmentVariable("AGENTSHIELD_DEMO_SQL");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("This demo needs a SQL Server test database. Every other demo works without one.");
                Console.WriteLine("Example: Server=localhost;Database=TestDatabase;Integrated Security=true;TrustServerCertificate=true");
                Console.Write("Connection string (press Enter to skip): ");
                connectionString = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(connectionString)) { Console.WriteLine("Skipped.\n"); return; }
            }

            // ==================== WHAT THE AI SENDS ====================
            string toolNameFromAI = "IssueRefund";
            string smallFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":20}";
            string largeFromAI = "{\"orderId\":\"ORD-48214\",\"amount\":600}";

            // ==================== YOUR APPLICATION ====================
            var ct = CancellationToken.None;
            string tenant = "demo-" + Guid.NewGuid().ToString("N")[..8];   // unique, so repeated runs never collide
            var alice = Caller.CreateForTenant(tenant, "alice");
            var manager = Caller.CreateForTenant(tenant, "maria", "Managers");   // sign-in says maria is in Managers
            var protector = new EncryptingProtector();

            Console.WriteLine("1. Create the tables");
            Console.WriteLine("EnsureSchemaAsync creates the AgentShield tables if they do not exist. Run it once at startup.");
            var firstStore = new SqlServerActionLifecycleStore(connectionString);
            await firstStore.EnsureSchemaAsync(ct);
            Console.WriteLine("Tables are ready.\n");
            var firstService = new YourRefundCode();
            var firstShield = MakeShield(firstStore, protector, firstService);

            Console.WriteLine("2. A request is stored in the database");
            Console.WriteLine("The same code as the in-memory demos. Only the store changed.");
            var result = await firstShield.RunAsync(alice, toolNameFromAI, smallFromAI, "sql-server-small-refund");
            Console.WriteLine("Refund of 20" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method ran {firstService.Calls} time(s).\n");

            Console.WriteLine("3. A retry is stopped by the database record");
            Console.WriteLine("The duplicate is recognised from the stored record, even from a new connection.");
            result = await firstShield.RunAsync(alice, toolNameFromAI, smallFromAI, "sql-server-small-refund");
            Console.WriteLine("Retry with the same key" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method ran {firstService.Calls} time(s).\n");

            Console.WriteLine("4. A pending approval is stored in the database");
            Console.WriteLine("Nothing runs yet. The encrypted values wait in the table.");
            var pending = await firstShield.RunAsync(alice, toolNameFromAI, largeFromAI, "sql-server-large-refund");
            Console.WriteLine("Refund of 600" + ": " + pending.Status + " " + pending.SafeMessage);
            Console.WriteLine($"Your refund method ran {firstService.Calls} time(s).\n");

            Console.WriteLine("5. The application restarts and a manager approves");
            Console.WriteLine("A brand new store and shield are created, as after a restart or on another server. The pending request is still there and the approval works.");
            var secondStore = new SqlServerActionLifecycleStore(connectionString);
            var secondService = new YourRefundCode();
            var secondShield = MakeShield(secondStore, protector, secondService);
            result = await secondShield.ApproveAsync(manager, pending.ApprovalId!);
            Console.WriteLine("Manager approves after the restart" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method ran {secondService.Calls} time(s).\n");

            Console.WriteLine("6. Recovery runs against the same tables");
            Console.WriteLine("The same recovery pass as in the in-memory demo.");
            Console.WriteLine("Result of the recovery pass: " + await secondShield.RecoverAbandonedAsync(10, null, ct) + "\n");
        }

        // The same setup as every other demo. Only the place where requests are stored (the database) is new.
        private static AgentShield MakeShield(IActionLifecycleStore store, IPayloadProtector protector, YourRefundCode myCode)
        {
            var shield = new AgentShield();
            shield.RequestStatus = store;
            shield.ApproverGroup = "Managers";
            shield.PayloadProtection = protector;
            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");
            return shield;
        }
    }
}
