// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class RulesDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            var user = Caller.Create("user-1001");

            // This line tells AgentShield: "the AI is allowed to ask for a refund".
            //   "IssueRefund" is the name the AI uses when it asks.
            //   refund is the information the AI sent (the order and the amount).
            //   myCode.IssueRefund is your own method. AgentShield runs it only if your rules allow it.
            var tool = shield.Allow<Refund>(myCode.IssueRefund);

            // A rule can give three answers:
            //   Pass            = this rule has no objection.
            //   NeedsApproval() = wait. A person can approve it (your method then runs) or reject it.
            //   Deny()          = refuse it for good. Nobody can approve it and your method never runs.
            //
            // RULE 1. A rule has two parts: a name, then a test.
            //   "RefundOverLimit" is the rule name. When this rule stops a request, AgentShield puts this name
            //   in the result, so you can see which rule made the decision.
            //   The test is the text in quotes. It looks at the refund and decides.
            //   Read as: "if the refund amount is over 100, a person must approve it. Otherwise it is allowed."
            tool.If("Amount > 100", NeedsApproval, "RefundOverLimit");

            // RULE 2. Same idea, with a different name. This one uses Deny() because a blocked order must never be refunded.
            //   Read as: "if the order id starts with blocked-, refuse it. Otherwise it is allowed."
            tool.If("OrderId starts with blocked-", Deny, "RefundBlockedOrder");

            // What the AI sent (made-up sample values). This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":25}";

            Console.WriteLine("1. The AI asks for a refund of 25:");
            var small = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + small.Status);

            Console.WriteLine("\n2. The AI asks for a refund of 100. Exactly 100 is not over 100, so it is allowed:");
            aiValues = "{\"orderId\":\"ORD-48214\",\"amount\":100}";
            var exact = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + exact.Status);

            Console.WriteLine("\n3. The AI asks for a refund of 5000. Rule 1 says a person must approve:");
            aiValues = "{\"orderId\":\"ORD-48215\",\"amount\":5000}";
            var big = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + big.Status + " (" + big.SafeMessage + ")");

            Console.WriteLine("\n4. The AI asks for a refund on order blocked-77. Rule 2 refuses it:");
            aiValues = "{\"orderId\":\"blocked-77\",\"amount\":10}";
            var blocked = shield.Run(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + blocked.Status + " (" + blocked.SafeMessage + ")");

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
