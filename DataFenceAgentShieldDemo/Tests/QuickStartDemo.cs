// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    // YOUR CODE: the method you already have. The AI will ask to use it.
    public class YourRefundCode
    {
        public int Calls;

        public string IssueRefund(Refund refund)
        {
            Calls++;
            Console.WriteLine("Refund of " + refund.Amount + " issued for order " + refund.OrderId);
            return "Refunded " + refund.Amount;
        }
    }

    // The values the AI sends for a refund: an order id and an amount.
    public record Refund(string OrderId, decimal Amount);

    public static class QuickStartDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            var user = Caller.Create("user-1001");

            // STEP 1. Tell AgentShield about your method.
            var tool = shield.Allow<Refund>(myCode.IssueRefund);

            // STEP 2. Add a rule: a refund over 100 needs a person to approve it.
            tool.If("Amount > 100", NeedsApproval);

            // STEP 3. The AI asks for a tool by name and sends its values as text. Give both to AgentShield.
            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":25}";

            Console.WriteLine("1. The AI asks for a refund of 25:");
            var small = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + small.Status);

            Console.WriteLine("\n2. The AI asks for a refund of 5000:");
            aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":5000}";
            var big = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + big.Status + " (no refund was issued)\n");

            Console.WriteLine($"Your refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
