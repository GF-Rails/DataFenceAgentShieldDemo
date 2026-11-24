# AgentShield API

Namespace: DataFence.AgentShield. Derived from the source. All values are immutable and validated at construction.

## Value types
| Type | Create | On bad input | Description |
|---|---|---|---|
| RequestIdentity | Create(tenantId, callerId, idempotencyKey) | Returns null for empty, over 256 characters or control characters | Tenant, stable caller and idempotency key. |
| RequestBinding | Create(toolName, toolContractVersion, fingerprint) | Returns null for a bad name, a version below 1 or a missing fingerprint | Tool, version and argument fingerprint bound to a request. |
| ArgumentFingerprint | Compute(args), CanonicalBytes(args), FromParts(format, hash) | FromParts returns null for an unknown format or a bad hash. Compute and CanonicalBytes throw for arguments that cannot be fingerprinted (duplicate property names, unsupported values); AgentShield catches this and refuses the request (AST006). | Format agentshield-args-v1: sorted, compact, normalized JSON hashed with SHA-256 (lowercase hex). |
| Caller | Create(userId), Create(tenantId, callerId) | A bad name gives IsValid = false and every request for it is refused (AST005). | The person asking, decided by your application from its own sign-in, never by the AI. Create(userId) uses the tenant "default". |
| PolicyResult | Allow(), Deny(code, message), RequireApproval(code, message) | A bad code is replaced by ASA011. | A policy answer. |
| ToolLimits | Create(executionTimeout, maxConcurrency) | Bad values are replaced by 30 seconds and 1. | Cooperative limits. A timeout cancels the token; it does not reverse side effects. |
| ToolExecutionResult | Completed(output), Failed(code, message), OutcomeUnknown(code, message) | A null output becomes empty; a bad code is replaced. | Handler result. Use OutcomeUnknown for timeouts and exceptions. |
| PendingPayload | Create(kind, version, data) | Returns null for an unknown kind, a version below 1 or empty data | Versioned payload (agentshield-pending-json, version 1). |
| ExecutionOutcome | Completed, Failed, OutcomeUnknown, From(ToolExecutionResult) | A null output becomes empty; a bad code becomes ASX002. | Outcome stored for replay. |
| ReconciliationResolution | Create(outcome, resolvedBy, evidence) | Returns null unless the outcome is Completed or Failed and resolvedBy and evidence are filled in | Explicit resolution of an unknown outcome. |
| ActionRecord | Create(...) | Returns null for an impossible record | Snapshot of a stored action, for store providers. |
| StoreResult | Created, Found, Transitioned, StorageFailure, ... | A missing record or bad failure code gives a StorageFailure result (ASX002). | Result of one store operation. |
| AgentShieldResult | Completed, Denied, PendingApproval, InProgress, Rejected, Expired, InvalidRequest, PolicyError, StateStoreError, Failed, OutcomeUnknown | A bad code is replaced by ASA013; a bad token gives a PolicyError. | Result for the AI-facing caller. Token only for PendingApproval; output only for Completed; separate Audit outcome. |

A factory that returns null on bad input never throws. Check the result for null before you use it. Code that only uses the AgentShield class does not need to do this.

## Interfaces you implement
| Interface | Member | Description |
|---|---|---|
| ITool<TArgs> | Name, ContractVersion, Approval, Limits, Policies, Validate(args), ExecuteAsync(invocation, ct) | One strongly typed tool. |
| IToolPolicy<TArgs> | EvaluateAsync(caller, args, ct) | Resource ownership and business policy. Rechecked on resume. |
| IToolRegistry | Resolve(toolName, version) | Returns Found, NotFound or VersionMismatch. Handlers come only from here. |
| IAuthorizationHooks | AuthorizeToolAsync, AuthorizeOriginalCallerAsync, AuthorizeApproverAsync | Current permissions, never stored claims. |
| IPayloadProtector | ProtectAsync, UnprotectAsync | Your protection of pending payloads. Throws PayloadProtectionException. |
| IAgentAuditLog | WriteAsync(event, ct) | Your audit destination. Events carry no arguments or outputs. |
| IActionLifecycleStore | see below | Provider contract. |

## IActionLifecycleStore
| Method | From | To | Results |
|---|---|---|---|
| RegisterAsync | none | Registered | Created, ExistingMatching, BindingConflict, StorageFailure |
| GetAsync | any | none | Found, NotFound |
| FindByApprovalIdAsync | any | none | Found, NotFound |
| FindPendingApprovalsAsync | none | none | Read only. Records in PendingApproval that have not expired, within one tenant, oldest first, at most a given count. |
| MarkPendingApprovalAsync | Registered | PendingApproval | Transitioned, StateConflict, BindingConflict, NotFound |
| ClaimExecutionAsync | Registered | Executing | Transitioned, StateConflict, BindingConflict, NotFound |
| ApproveAndClaimAsync | PendingApproval | Executing | Transitioned, ApprovalExpired, StateConflict, BindingConflict, NotFound |
| DenyAsync | Registered, PendingApproval | Denied | Transitioned, StateConflict, NotFound |
| RejectApprovalAsync | PendingApproval | Rejected | Transitioned, StateConflict, NotFound |
| ExpireApprovalAsync | PendingApproval | Expired | Transitioned, StateConflict, NotFound |
| FinalizeAsync | Executing | Completed, Failed, OutcomeUnknown | Transitioned, StateConflict, NotFound |
| AbandonExecutionAsync | Executing | OutcomeUnknown | Transitioned, StateConflict, NotFound |
| ReconcileAsync | OutcomeUnknown | Completed, Failed | Transitioned, StateConflict, NotFound |

Every method takes a tenant id and a CancellationToken. A StorageFailure carries IsOutcomeUncertain: when true the mutation may have been applied, so read the record before deciding anything.

## Defaults (AgentShieldDefaults)
| Property | Value |
|---|---|
| ApprovalLifetime | 24 hours |
| ExecutionLeaseMargin | 1 minute added to the tool timeout |
| RecommendedRetention | 30 days |

DataFenceComponents Copyright (c) 2026 All rights reserved.

## AgentShieldEngine (advanced)
> Most applications should use the AgentShield facade described at the end of this document (Tool, Rule, RunAsync). AgentShieldEngine below is the advanced, typed layer that the facade is built on. Use it when you need your own tool classes (ITool) or full control of the registry.

Constructor: AgentShieldEngine(registry, store, authorization, payloadProtector = null, auditLog = null, options = null, clock = null). Safe for concurrent use. Request, policy and store problems are returned as results, not thrown.
| Method | Parameters | Returns | Description |
|---|---|---|---|
| ExecuteAsync<TArgs> | Caller caller, toolName, toolContractVersion, idempotencyKey, arguments, cancellationToken | AgentShieldResult | Resolves the registered tool, validates and fingerprints the arguments, registers the request, evaluates authorization and policies, then executes, denies or returns PendingApproval. Never waits for a human. A duplicate returns the recorded result and does not run the tool again. |
| ApproveAsync | Caller approver, approvalId, cancellationToken | AgentShieldResult | Resolves the action in the approver tenant, authorizes the approver, decodes and verifies the stored payload against the stored fingerprint, rechecks the original caller and policies, atomically consumes the approval and claims execution, then runs the registered handler. Retrying after completion returns the recorded result. |
| RejectAsync | Caller approver, approvalId, cancellationToken | AgentShieldResult | Authorizes the approver and records the rejection. |

Options (AgentShieldEngineOptions): ApprovalLifetime (24 hours), ExecutionLeaseMargin (1 minute), RequireDifferentApprover (true).

Execution notes: a handler timeout or exception is recorded as OutcomeUnknown, never as Failed, and is never retried. Only a handler that reports Failed produces Failed. If recording the outcome fails, the result is OutcomeUnknown with code ASX004. An audit failure sets AgentShieldResult.Audit to Failed and does not change the execution status. A policy that throws or returns Unknown is a PolicyError, never an Allow. Tools that declare NotSpecified for approval are a PolicyError. Per-tool concurrency is limited by ToolLimits.MaxConcurrency within one engine instance.

## InMemoryStore
InMemoryActionLifecycleStore(TimeProvider clock = null) implements IActionLifecycleStore for one process. State is lost on restart, so duplicate protection does not survive a restart.

## Recovery and retention

| Member | Purpose |
|---|---|
| IActionLifecycleStore.FindAbandonedExecutionsAsync | Lists Executing records whose lease has expired (provider clock). Read-only. |
| IActionLifecycleStore.PurgeTerminalAsync | Deletes Completed, Failed, Denied, Rejected and Expired records older than a given age. Never deletes OutcomeUnknown, Executing, Registered or PendingApproval. |
| AgentShieldRecovery.RunOnceAsync | One pass: marks expired leases OutcomeUnknown (never re-runs a tool) and optionally purges. Call it from your own scheduler. |

## SqlServerStore

SqlServerActionLifecycleStore(connectionString, options) is a durable store. EnsureSchemaAsync creates the table. Expiry and leases use the SQL Server clock.


## DependencyInjection adapter

| Member | Purpose |
|---|---|
| AddAgentShield(configure) | Registers one AgentShield (the simple facade) for the application. The configure action receives the service provider and the AgentShield, sets its settings and registers tools. Runs once, on first use. |
| AddAgentShield() | Advanced. Registers the tool registry and AgentShieldEngine. The app registers IActionLifecycleStore and IAuthorizationHooks; IPayloadProtector, IAgentAuditLog, AgentShieldEngineOptions and TimeProvider are optional. |
| AddAgentShieldTool | Registers one tool explicitly. Tools are never discovered automatically. |
| AddAgentShieldRecovery | Hosted service that runs AgentShieldRecovery on an interval. |
| ToolRegistry | Immutable registry that rejects duplicate name and version pairs. |

## Audit

Implement IAgentAuditLog to write events to your own log. A DataFence Guardrails audit bridge is not available yet because the Guardrails AuditLogger is internal.

Example: an audit log that adds one line per decision to a file. The full explanation and the notes about its limits are in the README, under "Audit log".

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


## Licensing (namespace DataFence.AgentShield)

DataFenceLicense (static):
- Status: LicenseStatus, one of Unlicensed, Trial or Licensed.
- IsLicensed: true for Trial and Licensed.
- TryActivate(licenseKey, out message): activates this computer through the DataFence license service. The key is not stored. Returns true on success.
- TryDeactivate(licenseKey, out message): releases this computer so the key can be used on another computer.
- TryDeactivate(out message): releases this computer without typing the key, using the license stored on this computer. The message names the deactivated key in masked form (last four characters), or says there is no active license to deactivate.
- Changing keys: When you receive a new key (for example at renewal), the recommended steps are to deactivate the old license (Deactivate needs no key; it uses the license stored on this computer and shows the masked key it released), then activate the new one. If you activate the new key directly, the old key on this computer is replaced automatically; a mistyped new key leaves the old activation unchanged. Deactivate is mainly for moving a license to another computer. If the service cannot release the seat, the local license is still removed and the seat can be released from the console. The Manage License menu shows the active product and masked key (for example "...A1B2").
- NotLicensedMessage and TrialTag: the standard texts.

When Status is Unlicensed, AgentShieldEngine.ExecuteAsync, ApproveAsync and RejectAsync return AgentShieldStatus.Denied with ErrorCode ASL001 and never run a tool, and AgentShieldRecovery.RunOnceAsync does nothing. The trial lasts 30 days.

## AgentShield facade
| Member | Description |
|---|---|
| Allow<TArgs>(method) | Registers one of your methods under its own method name. Takes a named method; a lambda does not throw but is reported by Check() and the tool refuses requests. Returns a ToolBuilder. |
| Check() | Returns a list of setup problems (bad rule text, bad or duplicate names, missing handler, missing configuration, settings changed after the first request). An empty list means the setup is fine. Nothing in AgentShield throws for a setup mistake. Call it once after registering your tools. |
| Tool<TArgs>(name, handler) | Registers your function (sync or async) under a name you choose, or as a lambda and returns a ToolBuilder. |
| ToolBuilder.Rule(name, rule) | Adds a rule: Func<TArgs, RuleOutcome> or Func<Caller, TArgs, RuleOutcome>. Rules run in written order. |
| ToolBuilder.RuleAsync(name, rule) | Asynchronous rule. |
| ToolBuilder.Validate(check) | Checks that the values from the AI are well formed. Returns a message to refuse bad values (AST007), or null to accept. This is not a business rule, and a refused request never goes to approval. |
| Validate(condition, message) | Refuses a malformed request when the condition is true. The condition describes a bad request, not a policy. Use If for business rules. For example Validate("Amount <= 0", "The amount must be greater than zero."). Validate(check) takes a lambda that returns null or a message. |
| Rules.Pass, Rules.NeedsApproval(), Rules.Deny() | The outcomes of a rule. NeedsApproval(reason) and Deny(reason) add a reason, shown in the result message as "Rule name: reason". |
| RunAsync(user, toolNameFromAI, valuesFromAI, idempotencyKey = null) | Reads the JSON into the registered type and handles the request. Bad JSON returns AST011. Without a key a new one is made, so every call is a new request. See "Idempotency key" in the README. |
| RunAsync<TArgs>(user, toolNameFromAI, values, idempotencyKey = null) | Same, when your code already holds the values as an object. |
| ApproveAsync, RejectAsync(approver, approvalId) | Decide a pending request. |
| ListPendingAsync(approver, maxCount) | Lists the waiting requests this approver may decide, oldest first. Returns WaitingRequest items: ApprovalId, ToolName, RequestedBy, IdempotencyKey, RequestedUtc, ExpiresUtc. Read only, same tenant only, leaves out the approver's own requests and anything the approver may not approve. The values the AI sent are not included. Returns an empty list for any problem. |
| Approve, Reject(approver, result) | Same as ApproveAsync and RejectAsync, taking the AgentShieldResult that Run returned instead of its ApprovalId. |
| RecoverAbandonedAsync(batchSize, purgeOlderThan) | Marks abandoned executions OutcomeUnknown. Never runs a function again. |

Settings: DemoMode, RequestStatus (IActionLifecycleStore), UserAuthorization (IAuthorizationHooks), ApproverGroup, PayloadProtection, AuditLog, Clock, Options. They lock after the first request.

ApproverGroup: only users in this group may approve or reject, for example shield.ApproverGroup = "Managers". The name is yours to choose ("Managers" is only an example, you could use "Approvers" or "Finance"). It must be exactly the same text, including capital letters, as the group you put on the Caller. Your application puts the groups on the Caller from its own sign-in: Caller.Create("maria", "Managers"). Anyone else is refused with ASA003. Caller.CreateForTenant(tenantId, userId, groups) is for applications with several tenants.

Amounts and currency: a number such as Amount is a plain number. AgentShield does not know its currency or unit, so 25, 25.00 and 25.0 are the same value. The currency is whatever your own method uses, so state it in the tool description you give the AI. Do not rely on AgentShield to tell dollars from cents.

## Error codes
| Code | Meaning |
|---|---|
| ASL001 | Not licensed. |
| AST001 | Tool not found or not registered. |
| AST002 | Tool contract version not supported. |
| AST003 | Values do not match the tool. |
| AST004 | Tool registry failed. |
| AST005 | Values are required. |
| AST006 | Values cannot be fingerprinted. |
| AST007 | A Validate check refused the values. |
| AST008 | Values could not be validated. |
| AST009 | A valid idempotency key is required. |
| AST010 | Idempotency key already used for a different request. |
| AST011 | The values from the AI are not valid for this tool. |
| ASA001 | Authorization could not be evaluated. |
| ASA002 | Original user no longer allowed. |
| ASA003 | Approver or user not permitted, or requester approving their own request. |
| ASA010 | A rule answered Deny or NeedsApproval. The message names the rule. |
| ASA011 | A rule or tool is not set up correctly, or a rule returned no answer. The request was refused. Call Check(). |
| ASA012 | AgentShield is not configured. The request was refused. Call Check(). |
| ASA013 | An unexpected internal problem occurred. The request was refused and nothing was run. |
| ASP001 | Approval is required. |
| ASP002 | Approval not found. |
| ASP003 | Approval expired. |
| ASP004 | Approval requires a payload protector, or the payload cannot be read. |
| ASP005 | Pending payload could not be protected. |
| ASP006 | Pending payload does not match the request. |
| ASP007 | Pending payload could not be read. |
| ASP008 | Approval was rejected. |
| ASX001 | Function timed out; effect unknown. |
| ASX002 | Function failed unexpectedly; effect unknown. |
| ASX003 | Function returned no result. |
| ASX004 | Outcome could not be recorded; effect may have happened. |
| ASX006 | State store could not complete the request. |
| ASX008 | Execution lease expired without a recorded outcome. |



