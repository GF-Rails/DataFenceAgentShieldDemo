// DataFenceComponents Copyright (c) 2026 All rights reserved.

using DataFence.AgentShield;
using DataFenceComponents.Core;

// SWITCHING .NET VERSIONS: change <TargetFramework> in the demo project to net10.0 and rebuild. The menu header
// shows which runtime is running.

using Demo.Tests;

namespace Demo
{
    class Program
    {
        // StartDemo in each demo file is demo only, not part of the component.
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("=========================================================");
                Console.WriteLine("=========== DataFenceComponents.com Demo ================");
                Console.WriteLine("[Licensing Status] " + DataFenceLicense.Status);
                Console.WriteLine("[Version] " + DataFenceInfo.Description);
                Console.WriteLine("=========================================================");
                Console.WriteLine("1. Quick Start (register your function, add rules, call Run)");
                Console.WriteLine("2. Rules (allow, deny, or ask a person)");
                Console.WriteLine("3. Approvals (pending, approve, reject)");
                Console.WriteLine("4. Audit (every decision sent to your audit log)");
                Console.WriteLine("5. Idempotency (a repeated request never runs your function twice)");
                Console.WriteLine();
                Console.WriteLine("Advanced");
                Console.WriteLine("6. Your own rule from scratch");
                Console.WriteLine("7. Argument validation (reject bad values before anything else)");
                Console.WriteLine("8. Caller authorization (who may use a tool and who may approve)");
                Console.WriteLine("9. Approval expiration (a pending approval that is too old can no longer be approved)");
                Console.WriteLine("10. Recovery of abandoned executions (never run again)");
                Console.WriteLine("11. Payload protection (encrypted pending arguments)");
                Console.WriteLine("12. Dependency injection");
                Console.WriteLine("13. SQL Server provider (needs a test database, optional)");
                Console.WriteLine("14. Async version for web servers (RunAsync)");
                Console.WriteLine("15. Manage License");
                Console.WriteLine("16. Exit");
                Console.Write("Select an option: ");

                string? choice = Console.ReadLine()?.Trim('\uFEFF', ' ', '\t');
                Console.WriteLine();
                if (choice is null) return;

                try
                {
                    switch (choice)
                    {
                        case "1": QuickStartDemo.StartDemo(); break;
                        case "2": RulesDemo.StartDemo(); break;
                        case "3": ApprovalDemo.StartDemo(); break;
                        case "4": AuditDemo.StartDemo(); break;
                        case "5": IdempotencyDemo.StartDemo(); break;
                        case "6": YourOwnRuleDemo.StartDemo(); break;
                        case "7": ArgumentValidationDemo.StartDemo(); break;
                        case "8": CallerAuthorizationDemo.StartDemo(); break;
                        case "9": ApprovalExpiryDemo.StartDemo(); break;
                        case "10": await RecoveryDemo.StartDemo(); break;
                        case "11": PayloadProtectionDemo.StartDemo(); break;
                        case "12": DependencyInjectionDemo.StartDemo(); break;
                        case "14": await AsyncDemo.StartDemo(); break;
                        case "13": await SqlServerDemo.StartDemo(); break;
                        case "15": ManageLicense(); break;
                        case "16": return;
                        default: Console.WriteLine("Invalid selection.\n"); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n***The demo stopped with an error: {ex.GetType().Name}: {ex.Message}\n");
                }
            }
        }

        static void ManageLicense()
        {
            while (true)
            {
                Console.WriteLine("=============== Manage License ===============");
                Console.WriteLine(DataFenceLicense.LicenseSummary ?? "Not activated.");
                Console.WriteLine("1. Activate your License");
                Console.WriteLine("2. Deactivate your License");
                Console.WriteLine("3. About License (version and support report)");
                Console.WriteLine("4. Back");
                Console.Write("Select an option: ");

                string? choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        Console.Write("Enter your license key: ");
                        DataFenceLicense.TryActivate(Console.ReadLine()?.Trim() ?? string.Empty, out string activateMessage);
                        Console.WriteLine(activateMessage + "\n");
                        break;
                    case "2":
                        DataFenceLicense.TryDeactivate(out string deactivateMessage);
                        Console.WriteLine(deactivateMessage + "\n");
                        break;
                    case "3":
                        Console.WriteLine(DataFenceInfo.Report());
                        break;
                    case "4":
                        return;
                    default:
                        Console.WriteLine("Invalid selection.\n");
                        break;
                }
            }
        }
    }
}


