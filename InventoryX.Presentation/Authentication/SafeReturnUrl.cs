namespace InventoryX.Presentation.Authentication;

/// <summary>Restricts OAuth redirects to same-app paths or configured frontend origins.</summary>
public static class SafeReturnUrl
{
    public static string Normalize(string? returnUrl, IReadOnlyList<string> allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal))
            return returnUrl;

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var target))
            return "/";

        foreach (var origin in allowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var allowed))
                continue;

            if (string.Equals(
                    target.GetLeftPart(UriPartial.Authority),
                    allowed.GetLeftPart(UriPartial.Authority),
                    StringComparison.OrdinalIgnoreCase))
                return returnUrl;
        }

        return "/";
    }
}
