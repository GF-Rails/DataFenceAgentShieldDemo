// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using DataFence.AgentShield.InMemory;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class PayloadProtectionDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();

            // A waiting request has to remember what the AI asked for, until a person decides.
            // The protector locks (encrypts) those values first. This demo's protector keeps its key in memory only.
            var protector = new EncryptingProtector();

            var shield = new AgentShield();
            shield.DemoMode = true;
            shield.PayloadProtection = protector;
            shield.ApproverGroup = "Managers";   // only people in the Managers group may approve
            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            var alice = Caller.Create("alice");
            var manager = Caller.Create("maria", "Managers");   // sign-in says maria is in Managers

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"SECRET-77\",\"amount\":900}";

            Console.WriteLine("1. The AI asks for a refund of 900 for order SECRET-77. It waits for approval:");
            var waiting = shield.Run(alice, aiTool, aiValues);
            string stored = System.Text.Encoding.UTF8.GetString(protector.LastStored!);
            Console.WriteLine("   Result: " + waiting.Status);
            Console.WriteLine("   Can the stored data be read as text? " + (stored.Contains("SECRET-77") ? "Yes" : "No, it is locked."));

            Console.WriteLine("\n2. The manager approves. The values are unlocked and your method runs:");
            var approved = shield.Approve(manager, waiting.ApprovalId!);
            Console.WriteLine("   Result: " + approved.Status);

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
