// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using DataFence.AgentShield.DependencyInjection;
using DataFence.AgentShield.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    /// <summary>AgentShield registered in a .NET dependency injection container, as an ASP.NET or worker app
    /// would.</summary>
    // STEPS TO USE DEPENDENCY INJECTION
    //   Step 1. Register your own services, your store and your authorization hooks.
    //   Step 2. Call AddAgentShield and, inside it, set the settings and register your tools.
    //   Step 3. Optionally add the recovery job with AddAgentShieldRecovery.
    //   Step 4. Ask for AgentShield wherever you handle AI requests.
    public static class DependencyInjectionDemo
    {
        public static void StartDemo()
        {
            // ==================== WHAT THE AI SENDS ====================
            string toolNameFromAI = "IssueRefund";
            string valuesFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":30}";
            string unregisteredToolFromAI = "SendEmail";

            // ==================== YOUR APPLICATION ====================
            Console.WriteLine("1. Register your services and AgentShield");
            Console.WriteLine("Your application registers its own store, approver group and refund service. Tools are never discovered automatically, so only tools you register can run.");
            var services = new ServiceCollection();
            services.AddSingleton<YourRefundCode>();
            services.AddSingleton<IActionLifecycleStore, InMemoryActionLifecycleStore>();
            services.AddAgentShield((provider, shield) =>
            {
                shield.RequestStatus = provider.GetRequiredService<IActionLifecycleStore>();
                shield.ApproverGroup = "Managers";   // only people in the Managers group may approve
                var refundService = provider.GetRequiredService<YourRefundCode>();
                var refundTool = shield.Allow<Refund>(refundService.IssueRefund);
                refundTool.If("Amount > 100", NeedsApproval, "RefundOverLimit");
            });
            Console.WriteLine("Registered.\n");

            Console.WriteLine("2. Register the background recovery service");
            Console.WriteLine("One line adds a hosted service that runs a recovery pass on a schedule. This demo only shows that it is registered, because it is meant for a hosted application.");
            services.AddAgentShieldRecovery(TimeSpan.FromMinutes(1));
            using var provider = services.BuildServiceProvider();
            Console.WriteLine($"Hosted services registered: {provider.GetServices<IHostedService>().Count()}\n");

            Console.WriteLine("3. Ask for AgentShield and run a request");
            Console.WriteLine("Anywhere in your application, ask for AgentShield and call RunAsync.");
            var shield = provider.GetRequiredService<AgentShield>();
            var refundCalls = provider.GetRequiredService<YourRefundCode>();
            var alice = Caller.Create("alice");
            var result = shield.Run(alice, toolNameFromAI, valuesFromAI, "dependency-injection-refund");
            Console.WriteLine("Refund of 30" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method ran {refundCalls.Calls} time(s).\n");

            Console.WriteLine("4. A tool that was never registered cannot run");
            Console.WriteLine("SendEmail was not registered, so the request is refused.");
            result = shield.Run(alice, unregisteredToolFromAI, valuesFromAI, "dependency-injection-unregistered-tool");
            Console.WriteLine("Tool SendEmail" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method ran {refundCalls.Calls} time(s).\n");
        }
    }
}
