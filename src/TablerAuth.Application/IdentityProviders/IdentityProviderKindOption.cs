namespace TablerAuth.Application.IdentityProviders;

/// <summary>Formdaki tür listesi. Handler'ı kurulu olmayan tür seçilemez.</summary>
public sealed class IdentityProviderKindOption
{
    public required string Value { get; init; }

    public required bool HandlerInstalled { get; init; }

    public string? DefaultCallbackPath { get; init; }

    public string? DefaultScopes { get; init; }

    public bool RequiresAuthority { get; init; }

    public string? DefaultScheme { get; init; }

    public string? DefaultSecretKey { get; init; }
}
