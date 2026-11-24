// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;

namespace Demo.Tests
{
    // YOUR CODE: a check that text cannot express. It returns null when the values are fine, or a message when not.
    // Read as: "if the amount has more than two decimal places say so, otherwise it is fine".
    public class YourRefundChecks
    {
        public string? CheckDecimalPlaces(Refund refund)
        {
            if (decimal.Round(refund.Amount, 2) != refund.Amount)
            {
                return "The amount can have at most two decimal places.";
            }
            return null;
        }
    }

    /// <summary>Values from the AI
    // STEPS TO CHECK THE VALUES THE AI SENDS
    //   Step 1. Describe the values as a record, for example Refund(OrderId, Amount).
    //   Step 2. Add one Validate line per check. Return null when the values are fine, or a short message when they are not.
    //   Step 3. AgentShield runs the checks first, before any rule. A rejected request never reaches your code.
    public static class ArgumentValidationDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            // ==================== WHAT THE AI SENDS ====================
            // This is what the AI sends you (made-up sample values). A mistaken or tricked AI can send nonsense.
            string toolNameFromAI = "IssueRefund";
            string goodRefundFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":10}";
            string zeroRefundFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":0}";
            string negativeRefundFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":-50}";
            string noOrderRefundFromAI = "{\"orderId\":\"\",\"amount\":10}";
            string tooManyDecimalsFromAI = "{\"orderId\":\"ORD-48213\",\"amount\":10.999}";
            string notJsonFromAI = "refund everything please";

            // ==================== YOUR APPLICATION ====================
            var refundService = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            // This line tells AgentShield: "the AI is allowed to ask for a refund".
            //   "IssueRefund" is the name the AI uses when it asks.
            //   refund is the information the AI sent (the order and the amount).
            //   refundService.IssueRefund is your own method. AgentShield runs it only if your rules allow it.
            var refundTool = shield.Allow<Refund>(refundService.IssueRefund);

            // Step 2. One line per check. Each line returns null when the values are fine, or a message when they are not.
            // Each line says what is wrong, then the message to give. Read the second as: "if the amount is 0 or less,
            // say the amount must be greater than zero".
            refundTool.Validate("OrderId = \"\"", "The order identifier is required.");
            refundTool.Validate("Amount <= 0", "The amount must be greater than zero.");
            // Text cannot count decimal places, so this one is written as a method of your own, shown above.
            refundTool.Validate(new YourRefundChecks().CheckDecimalPlaces);

            var user = Caller.Create("user-1001");

            Console.WriteLine("1. Valid values");
            Console.WriteLine("Everything is in order, so the request goes on to the rules and your refund method runs.");
            var result = shield.Run(user, toolNameFromAI, goodRefundFromAI);
            Console.WriteLine("Refund of 10" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s).\n");

            Console.WriteLine("2. Amount of zero");
            Console.WriteLine("A refund of nothing makes no sense, so the request is rejected before any rule runs.");
            result = shield.Run(user, toolNameFromAI, zeroRefundFromAI);
            Console.WriteLine("Refund of 0" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");

            Console.WriteLine("3. Negative amount");
            Console.WriteLine("An AI can be tricked into asking for a negative refund. The check refuses it.");
            result = shield.Run(user, toolNameFromAI, negativeRefundFromAI);
            Console.WriteLine("Refund of -50" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");

            Console.WriteLine("4. Missing order identifier");
            Console.WriteLine("A required value is empty, so the request is rejected.");
            result = shield.Run(user, toolNameFromAI, noOrderRefundFromAI);
            Console.WriteLine("Refund without an order identifier" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");

            Console.WriteLine("5. Too many decimal places");
            Console.WriteLine("Money has two decimal places. A value such as 10.999 is refused instead of being silently rounded.");
            result = shield.Run(user, toolNameFromAI, tooManyDecimalsFromAI);
            Console.WriteLine("Refund of 10.999" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");

            Console.WriteLine("6. Text that is not JSON");
            Console.WriteLine("If the AI sends something that cannot be read as values for this tool, the request fails safely.");
            result = shield.Run(user, toolNameFromAI, notJsonFromAI);
            Console.WriteLine("Not JSON" + ": " + result.Status + " " + result.SafeMessage);
            Console.WriteLine($"Your refund method has run {refundService.Calls} time(s), unchanged.\n");
        }
    }
}
