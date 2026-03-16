using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

/// <summary>
/// Generates and validates HMAC-signed moderation tokens.
/// A token encodes the SHA-256 hash of the moderated text, the resulting
/// <see cref="ModerationStatus"/>, the projectId (to prevent CPMF (Cross Project Moderation Forging)
/// and a UTC timestamp. Because the token  is signed with a shared secret, the API can verify that the moderation
/// result was produced server-side and that the submitted text has not been
/// altered since the check — without needing shared state between instances.
/// </summary>
public class ModerationTokenService(string secret, ILogger<ModerationTokenService> logger) : IModerationTokenService
{
    private readonly byte[] _key = !string.IsNullOrWhiteSpace(secret)
        ? Encoding.UTF8.GetBytes(secret)
        : throw new ArgumentException("Moderation token secret must not be empty.", nameof(secret));
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(10);

    // Tracks consumed tokens (value = expiry timestamp for cleanup)
    private readonly ConcurrentDictionary<string, long> _consumedTokens = new();

    public string GenerateToken(string text, ModerationStatus status, int projectId)
    {
        var textHash = ComputeTextHash(text);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{projectId}|{textHash}|{(byte)status}|{timestamp}";
        var hmac = ComputeHmac(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{hmac}"));
    }

    public ModerationStatus? ValidateToken(string text, string token, int projectId, TimeSpan? maxAge = null)
    {
        try
        {
            // Reject already-consumed tokens
            if (_consumedTokens.ContainsKey(token)) return null;

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split('|');
            if (parts.Length != 5) return null;

            if (!int.TryParse(parts[0], out var tokenProjectId)) return null;
            var textHash = parts[1];
            if (!byte.TryParse(parts[2], out var statusByte)) return null;
            if (!long.TryParse(parts[3], out var timestamp)) return null;
            var hmac = parts[4];

            // Verify HMAC
            var payload = $"{tokenProjectId}|{textHash}|{statusByte}|{timestamp}";

            // Verify projectId matches
            if (tokenProjectId != projectId) return null;
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(ComputeHmac(payload)),
                    Encoding.UTF8.GetBytes(hmac)))
                return null;

            // Verify text hash matches submitted text
            if (ComputeTextHash(text) != textHash) return null;

            // Verify not expired
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
            if (age > (maxAge ?? DefaultMaxAge).TotalSeconds || age < 0) return null;

            return (ModerationStatus)statusByte;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate moderation token");
            return null;
        }
    }

    public void ConsumeToken(string token)
    {
        var expiry = DateTimeOffset.UtcNow.Add(DefaultMaxAge).ToUnixTimeSeconds();
        _consumedTokens.TryAdd(token, expiry);
        PurgeExpiredTokens();
    }

    private void PurgeExpiredTokens()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var kvp in _consumedTokens)
        {
            if (kvp.Value < now)
                _consumedTokens.TryRemove(kvp.Key, out _);
        }
    }

    private static string ComputeTextHash(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hash);
    }

    private string ComputeHmac(string payload)
    {
        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}