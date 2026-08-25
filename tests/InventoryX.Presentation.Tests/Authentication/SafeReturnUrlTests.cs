using FluentAssertions;
using InventoryX.Presentation.Authentication;

namespace InventoryX.Presentation.Tests.Authentication;

public sealed class SafeReturnUrlTests
{
    private static readonly string[] Allowed = ["https://app.example.com", "http://localhost:5173"];

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("//evil.example/steal", "/")]
    [InlineData("https://evil.example/capture", "/")]
    [InlineData("https://app.example.com/oauth/callback", "https://app.example.com/oauth/callback")]
    [InlineData("http://localhost:5173/", "http://localhost:5173/")]
    public void Normalize_restricts_external_redirects(string? input, string expected) =>
        SafeReturnUrl.Normalize(input, Allowed).Should().Be(expected);
}
