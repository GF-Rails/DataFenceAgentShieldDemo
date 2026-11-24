// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class AsyncDemo
    {
        // WHEN TO USE THIS: in a web server that handles many requests at the same time.
        // RunAsync does exactly the same as Run. The only difference is that the thread
        // is free to serve other requests while AgentShield waits (for example for a database).
        //   Run      -> simplest. Use it in console apps, scripts and tests.
        //   RunAsync -> use it in web servers. Put "await" in front of it.
        //   Approve / Reject have the same pair: ApproveAsync and RejectAsync.
        public static async Task StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;
            var user = Caller.Create("user-1001");

            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":25}";

            Console.WriteLine("1. The AI asks for a refund of 25 (using await shield.RunAsync):");
            var small = await shield.RunAsync(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + small.Status);

            Console.WriteLine("\n2. The AI asks for a refund of 5000 (using await shield.RunAsync):");
            aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":5000}";
            var big = await shield.RunAsync(user, aiTool, aiValues);
            Console.WriteLine("   Result: " + big.Status + " (no money was sent)\n");
        }
    }
}
