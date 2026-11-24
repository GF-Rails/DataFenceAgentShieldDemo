# AgentShield

AgentShield authorizes AI-requested application functions before your code runs them. It is deterministic: typed argument validation and your own authorization rules, never an LLM judgment.

It complements DataFence.Guardrails and is independent of it.

## Before and after

Before: the rules for an AI-requested function are scattered through your code.

```csharp
if (amount > 100) { /* ask a manager */ }
if (orderId.StartsWith("blocked-")) { return "refused"; }
// ...and the same checks again somewhere else
issueRefund(orderId, amount);
```

After: register the function once and keep the rules together. AgentShield runs the function only when every rule allows it.

```csharp
var shield = new AgentShield();
var refund = shield.Tool<RefundArgs>("IssueRefund", r => issueRefund(r.OrderId, r.Amount));
refund.If("Amount > 100", Rules.NeedsApproval, "OverLimit");
refund.If("OrderId starts with blocked-", Rules.Deny, "BlockedOrder");

var result = shield.Run(user, toolNameFromAI, valuesFromAI);   // web servers: await shield.RunAsync(...)
```

You also get checks on the values the AI sends, human approval, protection against running the same request twice, and an audit record of every decision.

## Status
Early development.

## What it covers
- Explicit tool registration with a name, a contract version, a typed argument class, limits and policies.
- Host-authenticated caller and tenant identity. AI-supplied arguments never establish identity.
- Idempotent requests with a deterministic argument fingerprint.
- Human approval that returns PendingApproval and never waits.
- A tracked lifecycle that records completed, failed and unknown outcomes.

## What it does not do
- It does not promise exactly-once external effects. Use the action id as the destination idempotency key, or reconcile.
- It does not re-run an action after a lease expires.
- It does not encrypt pending payloads. You provide the protection.

## Licensing
AgentShield runs as a 30-day full-function trial. After the trial, or with an invalid license, the engine denies requests with the code ASL001 and never runs a tool. Activate a license key once per developer computer with DataFenceLicense.TryActivate, or use the Manage License menu in the demo. TryDeactivate(out message) releases the computer without typing the key, so the license can be used elsewhere. DataFenceInfo.Report() returns a text report for support: version, runtime, license status and the version of every AgentShield DLL (About License in the demo menu).

## SQL Server provider
SqlServerStore provides a durable SQL Server lifecycle store using the database clock. Call EnsureSchemaAsync once to create the table.

## Demo
The Demo project is a console menu. The menu follows the order you learn the component, in six parts: start here (quick start); write your rules (argument validation, policies, caller authorization, your own rule from scratch, with the code shown); human approval (approvals, approval expiration); reliability (idempotency, recovery of abandoned executions); security and audit (payload protection, audit); going to production (dependency injection, an optional SQL Server provider). Each item runs several examples with plain-language explanations. Every item except the SQL Server one uses the in-memory store and needs no database. The SQL Server item asks for a test database connection string, or you can press Enter to skip it. License management is in the Manage License menu.

## Quick start
You already have functions the AI may call. Register each one with AgentShield, add your rules, then pass the AI's request to Run (or RunAsync in a web server). Approve and Reject have the same pairs.

```csharp
var shield = new AgentShield();
shield.DemoMode = true;   // testing only. Production sets RequestStatus and UserAuthorization instead.

var refundTool = shield.Allow<RefundArgs>(refundService.IssueRefund);   // the AI asks for it by the method name, "IssueRefund"
// To choose the AI-facing name yourself, or to pass a lambda, use Tool instead:
// shield.Tool<RefundArgs>("IssueRefund", refund => refundService.IssueRefund(refund.OrderId, refund.Amount));
// If is a business rule for a request that is already valid. Read as: "if the amount is over 100, it needs approval".
refundTool.If("Amount > 100", Rules.NeedsApproval);
// Validate is not a rule. It checks that the values from the AI are well formed, and refuses a bad request outright.
// Read as: "if the amount is 0 or less, the request is bad: refuse it with this message".
refundTool.Validate("Amount <= 0", "The amount must be greater than zero.");

shield.ApproverGroup = "Managers";   // only people in the Managers group may approve or reject. "Managers" is just an example: use any group name you like.

var user = Caller.Create("user-1001");   // who is asking, from your own sign-in, never from the AI
var maria = Caller.Create("maria", "Managers");   // sign-in says maria is in Managers
var result = shield.Run(user, toolNameFromAI, valuesFromAI);   // web servers: await shield.RunAsync(...)
```

toolNameFromAI and valuesFromAI are exactly what the AI sent: the tool name and the values as JSON. Everything else is your application. AgentShield checks the values, runs your rules in the order written, and then runs your function, asks for approval, or denies the request. Rule names are 1 to 64 letters, digits, underscore or hyphen, and a denial or approval request reports the rule name. Add a reason with Rules.Deny("reason") or Rules.NeedsApproval("reason"): the message then reads "Rule name: reason". Do not put secrets in a reason, because the result can reach the AI. A wrong property name does not throw: the tool refuses every request until it is fixed, and shield.Check() lists what is wrong (print it once after setup). Amount is a plain number. AgentShield does not know its currency, so say which one your method uses in the tool description you give the AI.

Settings (DemoMode, RequestStatus, UserAuthorization, ApproverGroup, PayloadProtection, AuditLog, Clock, Options) lock after the first request. Without DemoMode, RequestStatus is required, and so is either ApproverGroup or UserAuthorization.

The group name is yours to choose. "Managers" in these examples is only an example. Use whatever name your sign-in system already uses for the people who may approve, for example "Approvers", "Finance" or "Support-Leads". AgentShield only compares the name text exactly (capital letters matter), so the name you set in shield.ApproverGroup must be the same text that you put on the manager's Caller.

## Two ways to write a rule

A rule decides whether a request is allowed. To decide, it needs facts. Where those facts come from tells you which kind of rule to write.

**1. The facts are in the request: write the rule as text**

When the AI asks for a refund, it sends an order id and an amount. If your rule only needs those two things, write it as one line of text:

```csharp
tool.If("Amount > 100", NeedsApproval, "RefundOverLimit");
```

Read as: "if the amount is over 100, a person must approve it".

The amount is part of the request, so AgentShield can check it for you. You write no code of your own. See RulesDemo.

**2. The facts are in your own system: write the rule as a method**

Sometimes the request is not enough. Take the rule "people may only refund their own orders". The request names an order, but it does not say who owns it. Only your database knows that.

AgentShield cannot look inside your database, and a line of text cannot either. So you write a small method of your own that:

1. looks up the owner of the order in your database,
2. compares the owner with the person who is signed in, and
3. answers Pass or Deny.

Then you give that method to AgentShield:

```csharp
refundTool.Rule("OwnOrderOnly", myDatabase.CheckOrderOwner);
```

Read as: "before refunding, run my CheckOrderOwner method; if it says Deny, refuse the request". See YourOwnRuleDemo.

**How to choose**

Ask yourself: "Does the request contain everything this rule needs to know?"

- **Yes:** write a text rule.
- **No, I have to look something up in my own system:** write a method.

## Idempotency key

"Idempotency" is a standard software term, not an AI term. Payment and ordering systems have used it for years. It means that sending the same request twice has the same effect as sending it once.

This matters because requests do get sent twice: a network call is retried, or the AI asks again. Without protection, a customer could be refunded twice.

You protect against that by giving each request a key you choose. The key is the last value you pass to Run. Build it from your own data, so the same order always produces the same key:

```csharp
var result = shield.Run(user, toolNameFromAI, valuesFromAI, "refund-ORD-48213");
```

What AgentShield does with it:

- **Same key, same values:** this is the same request arriving again. AgentShield does not run your function a second time. It returns the first result.
- **Same key, different values:** this is refused (error AST010), because one key must always mean one request.
- **No key:** AgentShield makes a new one for every call, so every call counts as a new request. Always pass a key when the same request could arrive twice.

A key belongs to one user in one tenant, so two users can use the same key text without affecting each other. Keys are kept with the saved record of the request. If you delete old records (RecoverAbandonedAsync with purgeOlderThan), the same key counts as new again after that.

## When a request needs approval

When a rule says NeedsApproval, AgentShield saves the request, does not run your function, and returns the status PendingApproval. It keeps one record per request in the store you chose (memory, or SQL Server), and that record is also what stops a request being approved twice.

AgentShield does not send alerts, and it does not know your orders. Your application tells the manager and shows the order details. For the manager's screen, AgentShield gives you the list of what is waiting:

```csharp
var result = shield.Run(user, toolNameFromAI, valuesFromAI, "refund-ORD-48213");   // PendingApproval. Choose the key from your own data, so you know which order it is.

var maria = Caller.Create("maria", "Managers");   // from the manager's sign-in
foreach (var waiting in shield.ListPending(maria))   // only what Maria may decide, oldest first
    Console.WriteLine(waiting.ToolName + " asked by " + waiting.RequestedBy + ", key " + waiting.IdempotencyKey);

var first = shield.ListPending(maria)[0];
shield.Approve(maria, first.ApprovalId);   // your function runs now. shield.Reject(...) works the same way.
```

Each item has ApprovalId (the ID of that waiting request, which you pass to Approve or Reject), ToolName, RequestedBy, IdempotencyKey, RequestedUtc and ExpiresUtc. If you still hold the result from Run you can pass it instead: shield.Approve(maria, result).

The list leaves out requests in other tenants, requests that have expired or were already decided, the manager's own requests, and everything if the caller may not approve. It reads what is already saved and changes nothing.

The values the AI sent are encrypted and are only decrypted when the request is approved, so the list does not show an order id or amount. Build the key from your own data (for example refund-ORD-48213) and show your own order details next to the list.

## Audit log

AgentShield tells your own code about every decision it makes: allowed, waiting for approval, approved, rejected, denied and so on. It does not write a log file itself. You write a small class with one method, WriteAsync, and AgentShield calls it each time. What the method does is up to you: write to a file, a database table, or anywhere else.

Here is a class that adds one line per decision to a file:

```csharp
public class FileAuditLog : IAgentAuditLog
{
    private static readonly object fileLock = new object();

    public Task WriteAsync(AgentAuditEvent decision, CancellationToken cancellationToken)
    {
        string line = decision.TimestampUtc + "  " + decision.Type + "  user: " + decision.CallerId + "  function: " + decision.ToolName;

        lock (fileLock)
        {
            File.AppendAllText("audit.log", line + "\n");
        }

        return Task.CompletedTask;
    }
}
```

Use it with:

```csharp
shield.AuditLog = new FileAuditLog();
```

A line in the file looks like this:

```
1/15/2026 2:30:00 PM +00:00  Denied  user: alice  function: IssueRefund
```

The decision also has State, ErrorCode and Message, which are filled in when a request is refused. It never contains the values the AI sent, so they cannot end up in your log.

Good to know:

- The lock makes decisions take turns, so two writes cannot collide inside one running copy of your application. It does not help if several copies write to the same file.
- "audit.log" is created in the application's current folder. Use a full path such as C:\Logs\audit.log in a real application.
- The file keeps growing. Someone has to archive or delete old files.
- Anyone who can open the file can change it. AgentShield does not make tamper-proof logs.
- If writing fails, for example the disk is full, the request that was being handled is not turned into a failure. The audit write is reported as failed.

## Error codes
(ASA010 is a rule answer; ASA011 and ASA012 mean a setup mistake, see shield.Check())
