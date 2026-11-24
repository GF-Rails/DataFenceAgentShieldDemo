// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    // YOUR CODE: decides which users may use the AI tools. Here, everyone except "mallory".
    // Your real application would check its own users and roles here.
    internal sealed class EveryoneExceptMallory : IAuthorizationHooks
    {
        public Task<PolicyResult> AuthorizeToolAsync(Caller caller, string toolName, CancellationToken ct)
        {
            if (caller.CallerId == "mallory")
            {
                return Task.FromResult(PolicyResult.Deny("ASA001", "This user may not use this tool."));
            }
            return Task.FromResult(PolicyResult.Allow());
        }

        public Task<PolicyResult> AuthorizeOriginalCallerAsync(string tenantId, string originalCallerId, string toolName, CancellationToken ct)
        {
            return Task.FromResult(PolicyResult.Allow());
        }

        public Task<PolicyResult> AuthorizeApproverAsync(Caller approver, string toolName, Guid actionId, CancellationToken ct)
        {
            return Task.FromResult(PolicyResult.Allow());
        }
    }

    public static class CallerAuthorizationDemo
    {
        public static void StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var shield = new AgentShield();
            shield.DemoMode = true;

            // Tell AgentShield who may use the tools.
            shield.UserAuthorization = new EveryoneExceptMallory();

            shield.Allow<Refund>(myCode.IssueRefund);

            // What the AI sent. This is text from the AI, not something you wrote.
            var aiTool = "IssueRefund";
            var aiValues = "{\"orderId\":\"ORD-48213\",\"amount\":10}";
            var aiUnknownTool = "DeleteEverything";

            // The caller comes from your own sign-in, never from the AI.
            var aliceCaller = Caller.Create("alice");
            var malloryCaller = Caller.Create("mallory");

            Console.WriteLine("1. The user alice asks for a refund of 10:");
            var alice = shield.Run(aliceCaller, aiTool, aiValues);
            Console.WriteLine("   Result: " + alice.Status);

            Console.WriteLine("\n2. The user mallory asks for a refund of 10:");
            var mallory = shield.Run(malloryCaller, aiTool, aiValues);
            Console.WriteLine("   Result: " + mallory.Status + " (" + mallory.SafeMessage + ")");

            Console.WriteLine("\n3. The AI asks for a tool that was never registered (DeleteEverything):");
            var unknown = shield.Run(aliceCaller, aiUnknownTool, "{}");
            Console.WriteLine("   Result: " + unknown.Status + " (" + unknown.SafeMessage + ")");

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
