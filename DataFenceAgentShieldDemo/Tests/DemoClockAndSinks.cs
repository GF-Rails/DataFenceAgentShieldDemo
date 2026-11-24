// DataFenceComponents Copyright (c) 2026 All rights reserved.
using System.Security.Cryptography;
using DataFence.AgentShield;

namespace Demo.Tests
{
    /// <summary>A clock you control. Used only so the demos can show expiry and leases without waiting.</summary>
    internal sealed class DemoClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() { lock (this) return _now; }
        public void Advance(TimeSpan by) { lock (this) _now += by; }
    }

    /// <summary>An audit destination. AgentShield calls WriteAsync for every decision. Events never contain arguments
    /// or outputs.</summary>
    internal sealed class ConsoleAuditLog : IAgentAuditLog
    {
        public Task WriteAsync(AgentAuditEvent e, CancellationToken ct)
        {
            Console.WriteLine($"[audit] {e.Type,-18} caller={e.CallerId} tool={e.ToolName} state={e.State}" + (e.ErrorCode is null ? "" : $" code={e.ErrorCode}"));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A payload protector that encrypts pending arguments with AES-GCM and binds them to the tenant and action.
    /// The demo key lives in memory only. A real application takes its key from its own key management.
    /// </summary>
    internal sealed class EncryptingProtector : IPayloadProtector
    {
        private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
        public int Protected, Unprotected;
        public byte[]? LastStored;

        private static byte[] Aad(string tenantId, Guid actionId)
        {
            return System.Text.Encoding.UTF8.GetBytes(tenantId + "|" + actionId);
        }

        public Task<PendingPayload> ProtectAsync(string tenantId, Guid actionId, ReadOnlyMemory<byte> canonicalArguments, CancellationToken ct)
        {
            var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var cipher = new byte[canonicalArguments.Length];
            using (var aes = new AesGcm(_key, 16)) aes.Encrypt(nonce, canonicalArguments.Span, cipher, tag, Aad(tenantId, actionId));
            var data = nonce.Concat(tag).Concat(cipher).ToArray(); LastStored = data; Protected++;
            return Task.FromResult(PendingPayload.Create(PayloadKind.CustomerProtectedInline, 1, data)!);
        }

        public Task<ReadOnlyMemory<byte>> UnprotectAsync(string tenantId, Guid actionId, PendingPayload payload, CancellationToken ct)
        {
            var d = payload.Data.ToArray(); var plain = new byte[d.Length - 28];
            try { using var aes = new AesGcm(_key, 16); aes.Decrypt(d.AsSpan(0, 12), d.AsSpan(28), d.AsSpan(12, 16), plain, Aad(tenantId, actionId)); }
            catch (CryptographicException ex) { throw new PayloadProtectionException("The pending payload could not be decrypted.", ex); }
            Unprotected++;
            return Task.FromResult<ReadOnlyMemory<byte>>(plain);
        }
    }
}
