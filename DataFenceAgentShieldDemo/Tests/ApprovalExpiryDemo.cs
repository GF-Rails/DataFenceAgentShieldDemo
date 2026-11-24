// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class ApprovalExpiryDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var clock = new DemoClock();   // a clock we control, so the demo does not have to wait

            var shield = new AgentShield();
            shield.DemoMode = true;
            shield.Clock = clock;
            shield.ApproverGroup = "Managers";   // only people in the Managers group may approve

            // A waiting request is refused if nobody decides within 1 hour.
            shield.Options = new AgentShieldEngineOptions { ApprovalLifetime = TimeSpan.FromHours(1) };

            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            var alice = Caller.Create("alice");
            var manager = Caller.Create("maria", "Managers");   // sign-in says maria is in Managers

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues1 = "{\"orderId\":\"ORD-48213\",\"amount\":400}";
            var aiValues2 = "{\"orderId\":\"ORD-48214\",\"amount\":400}";

            Console.WriteLine("1. A refund of 400 waits. The manager approves after 30 minutes:");
            var onTime = shield.Run(alice, aiTool, aiValues1);
            clock.Advance(TimeSpan.FromMinutes(30));
            var approved = shield.Approve(manager, onTime.ApprovalId!);
            Console.WriteLine("   Result: " + approved.Status + " (in time, so the money was sent)");

            Console.WriteLine("\n2. Another refund of 400 waits. The manager approves after 2 hours:");
            var late = shield.Run(alice, aiTool, aiValues2);
            clock.Advance(TimeSpan.FromHours(2));
            var tooLate = shield.Approve(manager, late.ApprovalId!);
            Console.WriteLine("   Result: " + tooLate.Status + " (too late, so no money was sent)");

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
