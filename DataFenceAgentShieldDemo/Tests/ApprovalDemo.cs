// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class ApprovalDemo
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

            var alice = Caller.Create("alice");
            var maria = Caller.Create("maria", "Managers");   // sign-in says maria is in Managers

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiBigValues = "{\"orderId\":\"ORD-48213\",\"amount\":500}";
            var aiSecondValues = "{\"orderId\":\"ORD-48214\",\"amount\":700}";

            // The AI asks for a big refund. It needs a manager, so your method does not run yet.
            // AgentShield saved the request. It does not send alerts, so your application must tell a manager.
            // The last value is a key you choose from your own data, so you can tell which order it is.
            Console.WriteLine("1. The AI asks for a refund of 500, and then of 700:");
            var bigRefund = shield.Run(alice, aiTool, aiBigValues, "refund-ORD-48213");
            Console.WriteLine("   Result: " + bigRefund.Status);
            var secondRefund = shield.Run(alice, aiTool, aiSecondValues, "refund-ORD-48214");
            Console.WriteLine("   Result: " + secondRefund.Status);

            // A manager opens your screen. ListPending shows what AgentShield saved and what this manager may decide.
            // It shows the function, who asked, the key and the times. The values from the AI stay encrypted,
            // so your own screen shows the order details (order id and amount) from your own data.
            Console.WriteLine("\n2. Maria opens her list of waiting requests:");
            foreach (var waiting in shield.ListPending(maria))
                Console.WriteLine("   " + waiting.ToolName + ", asked by " + waiting.RequestedBy + ", key " + waiting.IdempotencyKey);

            // Alice is not in the Managers group, so she sees nothing and may not decide.
            Console.WriteLine("\n3. Alice looks at the list and tries to approve:");
            Console.WriteLine("   Waiting requests she may decide: " + shield.ListPending(alice).Count);
            var notAllowed = shield.Approve(alice, bigRefund);
            Console.WriteLine("   Alice tries to approve: " + notAllowed.Status + " " + notAllowed.SafeMessage);

            // Approve it and your method runs now.
            Console.WriteLine("\n4. Maria approves the first one:");
            var approved = shield.Approve(maria, bigRefund);
            Console.WriteLine("   Result: " + approved.Status);

            // Or reject it and your method never runs.
            Console.WriteLine("\n5. Maria rejects the second one:");
            var rejected = shield.Reject(maria, secondRefund);
            Console.WriteLine("   Result: " + rejected.Status + " (no refund was issued)");
            Console.WriteLine("   Waiting requests left: " + shield.ListPending(maria).Count + "\n");
        }
    }
}
