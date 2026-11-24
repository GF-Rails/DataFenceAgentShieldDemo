// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    // YOUR CODE: stands in for your real database. It returns the owner of an order, or null when the order is not found.
    public class YourOrderDatabase
    {
        public string? GetOrderOwner(string orderId)
        {
            if (orderId == "order-2001") return "alice";
            if (orderId == "order-2002") return "bob";
            return null;
        }

        // YOUR RULE: a normal method. AgentShield calls it with the user and the refund the AI asked for.
        // Read as: "if the owner of the order is this user it passes, otherwise deny it with this reason".
        public RuleOutcome CheckOrderOwner(Caller user, Refund refund)
        {
            string? owner = GetOrderOwner(refund.OrderId);
            if (owner == user.CallerId)
            {
                return Pass;
            }
            return Deny("This order does not belong to you.");
        }
    }

    /// <summary>Shows a rule of your own from start to finish, using facts from your own data.</summary>
    // STEPS TO WRITE A RULE FROM SCRATCH
    //   Step 1. Decide the business rule in plain words: "a user may only refund orders that belong to them".
    //   Step 2. Write it as a normal method that takes the user and the refund. It can look at your own data (here,
    //   the order owner from your database).
    //   Step 3. Answer Pass, NeedsApproval() or Deny(). When the rule lacks the facts it needs, answer Deny() so it
    //   fails safe.
    //   Step 4. Give the method to AgentShield with one Rule line. AgentShield runs your rule with the others, in the
    //   order written.
    public static class YourOwnRuleDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            // ==================== WHAT THE AI SENDS ====================
            // This is what the AI sends you (made-up sample values).
            string toolNameFromAI = "IssueRefund";
            string ownOrderFromAI = "{\"orderId\":\"order-2001\",\"amount\":20}";
            string otherOrderFromAI = "{\"orderId\":\"order-2002\",\"amount\":20}";
            string unknownOrderFromAI = "{\"orderId\":\"order-9999\",\"amount\":20}";

            // ==================== YOUR APPLICATION ====================
            var myDatabase = new YourOrderDatabase();
            var refundService = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            // This line tells AgentShield: "the AI is allowed to ask for a refund".
            //   "IssueRefund" is the name the AI uses when it asks.
            //   refund is the information the AI sent (the order and the amount).
            //   refundService.IssueRefund is your own method. AgentShield runs it only if your rules allow it.
            var refundTool = shield.Allow<Refund>(refundService.IssueRefund);

            // This rule looks at the user as well as the refund. "OwnOrderOnly" is the rule name.
            // myDatabase.CheckOrderOwner is your own method, shown above.
            refundTool.Rule("OwnOrderOnly", myDatabase.CheckOrderOwner);

            var alice = Caller.Create("alice");

            Console.WriteLine("1. Refund of the user's own order");
            Console.WriteLine("The rule looks up the owner of order-2001. It is alice, and alice is the user, so your refund method runs.");
            var result = shield.Run(alice, toolNameFromAI, ownOrderFromAI);
            Console.WriteLine("Alice refunds order-2001" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s).\n");

            Console.WriteLine("2. Refund of someone else's order");
            Console.WriteLine("order-2002 belongs to bob. The rule denies the request, so your refund method does not run.");
            result = shield.Run(alice, toolNameFromAI, otherOrderFromAI);
            Console.WriteLine("Alice refunds order-2002" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");

            Console.WriteLine("3. Refund of an order nobody owns");
            Console.WriteLine("An unknown order has no owner, so the rule denies it. A good rule fails safe when it lacks the facts.");
            result = shield.Run(alice, toolNameFromAI, unknownOrderFromAI);
            Console.WriteLine("Alice refunds order-9999" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");
        }
    }
}
