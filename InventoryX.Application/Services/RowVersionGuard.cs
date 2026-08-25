using InventoryX.Application.Exceptions;

namespace InventoryX.Application.Services;

/// <summary>Shared optimistic-concurrency check for If-Match / ExpectedRowVersion.</summary>
public static class RowVersionGuard
{
    public static void EnsureMatch(byte[]? current, byte[]? expected)
    {
        if (expected is null || expected.Length == 0) return;
        if (current is null || current.Length == 0 || !current.AsSpan().SequenceEqual(expected))
            throw new ConflictException("The resource was modified by another request. Reload and retry with the latest ETag.");
    }
}
