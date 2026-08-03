namespace DesktopRuntime.Core.Permissions;

/// <summary>
/// The capabilities granted to one package. Evaluation is default-deny: a request is
/// refused unless an exactly-matching grant is present. There is no wildcard, no
/// hierarchy, and no implicit widening — <c>network.domain:example.com</c> grants
/// that host and nothing else, not its subdomains and not similarly-spelled hosts.
/// See docs/architecture/permission-model.md.
/// </summary>
public sealed class PermissionSet
{
    private readonly HashSet<Capability> _granted;

    private PermissionSet(HashSet<Capability> granted) => _granted = granted;

    /// <summary>A set granting nothing. The starting point for any untrusted package.</summary>
    public static PermissionSet Empty { get; } = new([]);

    public IReadOnlyCollection<Capability> Granted => _granted;

    /// <summary>
    /// Builds a permission set from declared capability strings.
    /// </summary>
    /// <exception cref="CapabilityFormatException">Any declaration is unknown or malformed.</exception>
    public static PermissionSet FromDeclarations(IEnumerable<string> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        var granted = new HashSet<Capability>();
        foreach (string declaration in declarations)
        {
            granted.Add(Capability.Parse(declaration));
        }

        return new PermissionSet(granted);
    }

    /// <summary>Whether an unscoped capability is granted.</summary>
    public bool IsGranted(string capabilityName) => IsGranted(capabilityName, scope: null);

    /// <summary>
    /// Whether a capability is granted for the given scope. The scope is normalized the
    /// same way a declaration is, so a request for <c>EXAMPLE.COM</c> matches a grant of
    /// <c>example.com</c> while a request for a different host does not.
    /// </summary>
    public bool IsGranted(string capabilityName, string? scope)
    {
        if (string.IsNullOrWhiteSpace(capabilityName)) return false;
        if (!CapabilityCatalog.TryGetDefinition(capabilityName, out var definition)) return false;

        if (definition.RequiresScope)
        {
            // A scoped capability can never be satisfied without a scope; treating a
            // missing scope as "any" would be exactly the kind of implicit widening
            // this model exists to prevent.
            if (string.IsNullOrWhiteSpace(scope)) return false;
            if (!definition.TryNormalizeScope(scope, out string? normalized, out _)) return false;

            scope = normalized;
        }
        else if (scope is not null)
        {
            return false;
        }

        foreach (var capability in _granted)
        {
            if (string.Equals(capability.Name, definition.Name, StringComparison.Ordinal)
                && string.Equals(capability.Scope, scope, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
