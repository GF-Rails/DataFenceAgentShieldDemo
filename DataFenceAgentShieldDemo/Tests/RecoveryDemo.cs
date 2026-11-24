// DataFenceComponents Copyright (c) 2026 All rights reserved.
using DataFence.AgentShield;
using DataFence.AgentShield.InMemory;
using static DataFence.AgentShield.Rules;

namespace Demo.Tests
{
    public static class RecoveryDemo
    {
        public static async Task StartDemo()
        {
            // Demos run in DemoMode to illustrate the features. Do not use DemoMode in production.
            var myCode = new YourRefundCode();
            var clock = new DemoClock();   // a clock we control, so the demo does not have to wait
            var store = new InMemoryActionLifecycleStore(clock);   // where AgentShield keeps the state of each request

            var shield = new AgentShield();
            shield.DemoMode = true;
            shield.Clock = clock;
            shield.RequestStatus = store;
            var tool = shield.Allow<Refund>(myCode.IssueRefund);
            tool.If("Amount > 100", NeedsApproval, "TooBig");

            var alice = Caller.Create("alice");
            var ct = CancellationToken.None;

            // Imagine your program crashes while your refund method is running.
            // Nobody knows if the money was sent. The next lines pretend that happened.
            Console.WriteLine("1. The program crashes in the middle of a refund of 50.");
            var binding = RequestBinding.Create("IssueRefund", 1, ArgumentFingerprint.Compute(new Refund("ORD-48213", 50m)))!;
            var request = (await store.RegisterAsync(RequestIdentity.Create("default", "alice", "crashed-request")!, binding, ct)).Record!;
            await store.ClaimExecutionAsync("default", request.ActionId, request.Revision, binding, Guid.NewGuid(), TimeSpan.FromMinutes(1), ct);
            Console.WriteLine("   State: " + (await store.GetAsync("default", request.ActionId, ct)).Record!.State);

            // RecoverAbandonedAsync finds requests that were left unfinished. Call it on a schedule.
            Console.WriteLine("\n2. Five minutes later, recovery runs:");
            clock.Advance(TimeSpan.FromMinutes(5));
            Console.WriteLine("   " + await shield.RecoverAbandonedAsync(10, null, ct));
            Console.WriteLine("   State: " + (await store.GetAsync("default", request.ActionId, ct)).Record!.State);
            Console.WriteLine("   The request is marked OutcomeUnknown. Your method is never run again, so nobody is refunded twice by accident.");

            Console.WriteLine($"\nYour refund method ran {myCode.Calls} time(s).\n");
        }
    }
}
