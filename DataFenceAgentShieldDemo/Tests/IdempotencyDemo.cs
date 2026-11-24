// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class IdempotencyDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            var alice = Caller.Create("alice");

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":10}";
            var aiChangedValues = "{\"orderId\":\"ORD-48213\",\"amount\":99}";

            // Networks retry. The AI may send the same request twice.
            // Your application (not the AI) chooses a key for each request, built from your own data (the last value in
            // Run). The same key means the same request.
            Console.WriteLine("1. The AI asks for a refund of 10. Your application adds the key \"refund-ORD-48213\":");
            var first = shield.Run(alice, aiTool, aiValues, "refund-ORD-48213");
            Console.WriteLine("   Result: " + first.Status);

            Console.WriteLine("\n2. The same request arrives again, and your application adds the same key:");
            var again = shield.Run(alice, aiTool, aiValues, "refund-ORD-48213");
            Console.WriteLine("   Result: " + again.Status + " (the first result is returned, so the money is not sent twice)");

            Console.WriteLine("\n3. The AI sends a different amount (99) and your application adds the same key:");
            var changed = shield.Run(alice, aiTool, aiChangedValues, "refund-ORD-48213");
            Console.WriteLine("   Result: " + changed.Status + " (refused, because one key must always mean the same request)");

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
