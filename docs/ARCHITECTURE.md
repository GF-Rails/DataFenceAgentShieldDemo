# AgentShield architecture

## Concepts
- Policy decision: Allow, Deny or RequireApproval.
- Stored state: Registered, PendingApproval, Executing, Completed, Denied, Rejected, Expired, Failed, OutcomeUnknown. Registered means recorded, not authorized.
- Returned status: Completed, Denied, PendingApproval, InProgress, Rejected, Expired, InvalidRequest, PolicyError, StateStoreError, Failed, OutcomeUnknown.

## Flow
1. Your host authenticates the caller and passes a trusted context.
2. The registry resolves the tool by name and contract version. Handlers come only from the registry.
3. Arguments are validated and fingerprinted by the engine.
4. The request is registered once per tenant, caller and idempotency key. A different binding under the same key is rejected.
5. Policies decide Allow, Deny or RequireApproval. Approval returns PendingApproval immediately.
6. An approver resumes the action. The approver and the original caller are authorized again, policies are rechecked, and approval consumption plus execution claim happen in one atomic step.
7. The outcome is stored. Duplicates of a completed action return the recorded output.

## Uncertain outcomes
A timeout or exception does not prove the external effect failed. Such actions are recorded as OutcomeUnknown and are resolved only by explicit reconciliation. AgentShield never re-executes them automatically.

## Storage providers
Providers implement IActionLifecycleStore. Each mutation must check state, binding and revision atomically, decide expiry with the provider clock, and be safe for concurrent callers. The StoreConformance project is the test suite a provider must pass.

## Retention
Duplicate protection lasts only as long as records are kept. The recommended minimum is 30 days.

## Pending payloads
The payload holds the validated arguments as versioned JSON. You supply the protection (IPayloadProtector) and tenant-scoped access. If protection or decoding fails, the action is not executed.

DataFenceComponents Copyright (c) 2026 All rights reserved.

## Durable provider and recovery

SqlServerStore keeps every lifecycle transition in one short transaction, so duplicates are blocked across restarts and servers. A host calls AgentShieldRecovery on a schedule: expired executions become OutcomeUnknown for explicit reconciliation, and old terminal records are purged. Nothing is ever re-executed automatically.

