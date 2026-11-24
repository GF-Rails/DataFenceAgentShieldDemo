// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class AuditDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            shield.ApproverGroup = "Managers";   // only people in the Managers group may approve
            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            // Every decision AgentShield makes is sent to your audit log.
            // Here the log just prints each decision. A real application writes to its own file or database.
            // For example: shield.AuditLog = new FileAuditLog();   or   new DatabaseAuditLog();
            // You write that class yourself, see documentation for coding examples
            shield.AuditLog = new ConsoleAuditLog();

            var alice = Caller.Create("alice");
            var manager = Caller.Create("maria", "Managers");   // sign-in says maria is in Managers

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiSmallValues = "{\"orderId\":\"ORD-48213\",\"amount\":20}";
            var aiBigValues = "{\"orderId\":\"ORD-48214\",\"amount\":800}";
            var aiSecondValues = "{\"orderId\":\"ORD-48215\",\"amount\":900}";

            Console.WriteLine("Each line starting with [audit] is one decision written to the log.\n");

            Console.WriteLine("1. A refund of 20 is allowed:");
            shield.Run(alice, aiTool, aiSmallValues);

            Console.WriteLine("\n2. A refund of 800 waits, and then the manager approves it:");
            var waiting = shield.Run(alice, aiTool, aiBigValues);
            shield.Approve(manager, waiting.ApprovalId!);

            Console.WriteLine("\n3. A refund of 900 waits, and then the manager rejects it:");
            var second = shield.Run(alice, aiTool, aiSecondValues);
            shield.Reject(manager, second.ApprovalId!);
            Console.WriteLine();
        }
    }
}
