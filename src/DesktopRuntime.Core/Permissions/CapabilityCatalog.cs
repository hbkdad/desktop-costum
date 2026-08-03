using System.Diagnostics.CodeAnalysis;

namespace DesktopRuntime.Core.Permissions;

/// <summary>
/// The closed set of capabilities a package may declare. Anything not listed here
/// cannot be requested, which is what makes "default deny" enforceable rather than
/// aspirational: there is deliberately no capability for running arbitrary shell
/// commands, scripts, or native code. See docs/architecture/permission-model.md.
/// </summary>
public static class CapabilityCatalog
{
    public const string SystemMetricsRead = "system.metrics.read";
    public const string FilesUserSelectedRead = "files.user-selected.read";
    public const string ClipboardReadOnUserAction = "clipboard.read-on-user-action";
    public const string NetworkDomain = "network.domain";
    public const string ProcessLaunch = "process.launch";

    private static readonly Dictionary<string, CapabilityDefinition> Definitions =
        new(StringComparer.Ordinal)
        {
            [SystemMetricsRead] = new(
                SystemMetricsRead,
                "Read aggregate system metrics (CPU, memory, storage) for display."),

            [FilesUserSelectedRead] = new(
                FilesUserSelectedRead,
                "Read only files the user explicitly picked. Confers no ambient filesystem access."),

            [ClipboardReadOnUserAction] = new(
                ClipboardReadOnUserAction,
                "Read the clipboard, and only in direct response to a user action."),

            [NetworkDomain] = new(
                NetworkDomain,
                "Contact exactly one declared host. One capability entry per host.",
                scopeExample: "api.example.com",
                scopeNormalizer: NormalizeHostScope),

            [ProcessLaunch] = new(
                ProcessLaunch,
                "Launch one specific declared application. Never an arbitrary command line.",
                scopeExample: "declared-application",
                scopeNormalizer: NormalizeApplicationIdScope)
        };

    /// <summary>All capability names this build understands.</summary>
    public static IReadOnlyCollection<string> KnownNames => Definitions.Keys;

    public static bool TryGetDefinition(string name, [NotNullWhen(true)] out CapabilityDefinition? definition) =>
        Definitions.TryGetValue(name, out definition);

    /// <summary>
    /// Validates a network host scope. Intentionally strict: exactly one host, no
    /// wildcards, no scheme, no path, no port. Wildcards would turn one declaration
    /// into an open-ended grant, and a scheme or path would invite parser-confusion
    /// tricks where the declared host is not the host actually contacted.
    /// </summary>
    private static bool NormalizeHostScope(
        string scope,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = null;

        if (scope.Contains('*'))
        {
            error = "wildcards are not permitted; declare each host explicitly.";
            return false;
        }

        if (scope.Contains('/') || scope.Contains('\\'))
        {
            error = "a scheme or path is not permitted; declare a bare host name.";
            return false;
        }

        if (scope.Contains('@'))
        {
            error = "user info is not permitted in a host declaration.";
            return false;
        }

        // Casing is not significant in DNS, so normalize once here and compare
        // ordinally afterwards rather than relying on culture-sensitive comparisons.
        string candidate = scope.Trim().TrimEnd('.').ToLowerInvariant();

        if (candidate.Length == 0)
        {
            error = "the host is empty.";
            return false;
        }

        var hostType = Uri.CheckHostName(candidate);
        if (hostType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
        {
            error = "it is not a valid host name.";
            return false;
        }

        normalized = candidate;
        error = null;
        return true;
    }

    /// <summary>
    /// Validates a declared application identifier. Anything resembling a command line
    /// (arguments, separators, quoting, redirection) is rejected — the whole point of
    /// this capability is that it names an application, not a command to run.
    /// </summary>
    private static bool NormalizeApplicationIdScope(
        string scope,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = null;
        string candidate = scope.Trim();

        if (candidate.Length == 0)
        {
            error = "the application identifier is empty.";
            return false;
        }

        char[] forbidden = [' ', '\t', '"', '\'', '&', '|', ';', '<', '>', '%', '$', '`', '\r', '\n', '/', '\\'];
        if (candidate.IndexOfAny(forbidden) >= 0)
        {
            error = "it must be a plain application identifier, not a command line or path.";
            return false;
        }

        normalized = candidate;
        error = null;
        return true;
    }
}

/// <summary>Metadata describing one capability in the catalog.</summary>
public sealed class CapabilityDefinition
{
    internal delegate bool ScopeNormalizer(
        string scope,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error);

    private readonly ScopeNormalizer? _scopeNormalizer;

    internal CapabilityDefinition(
        string name,
        string description,
        string? scopeExample = null,
        ScopeNormalizer? scopeNormalizer = null)
    {
        Name = name;
        Description = description;
        ScopeExample = scopeExample;
        _scopeNormalizer = scopeNormalizer;
    }

    public string Name { get; }

    /// <summary>Human-readable description, shown in the permission prompt.</summary>
    public string Description { get; }

    public string? ScopeExample { get; }

    public bool RequiresScope => _scopeNormalizer is not null;

    internal bool TryNormalizeScope(
        string scope,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        if (_scopeNormalizer is null)
        {
            normalized = null;
            error = "this capability does not take a scope.";
            return false;
        }

        return _scopeNormalizer(scope, out normalized, out error);
    }
}
