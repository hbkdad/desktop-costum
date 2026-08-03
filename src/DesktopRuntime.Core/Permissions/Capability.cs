using System.Diagnostics.CodeAnalysis;

namespace DesktopRuntime.Core.Permissions;

/// <summary>
/// A single declared capability, e.g. <c>system.metrics.read</c> or
/// <c>network.domain:api.example.com</c>. See docs/architecture/permission-model.md.
/// <para>
/// Capabilities are parsed and validated up front. An unrecognised or malformed
/// capability is a hard failure, never a silently-ignored entry — silently dropping
/// one would let a package appear to declare less than it does.
/// </para>
/// </summary>
public sealed class Capability : IEquatable<Capability>
{
    private Capability(string name, string? scope)
    {
        Name = name;
        Scope = scope;
    }

    /// <summary>The capability name, e.g. <c>network.domain</c>.</summary>
    public string Name { get; }

    /// <summary>
    /// The scope argument for scoped capabilities, e.g. the host for
    /// <c>network.domain</c>. Null for unscoped capabilities.
    /// </summary>
    public string? Scope { get; }

    public override string ToString() => Scope is null ? Name : $"{Name}:{Scope}";

    /// <summary>
    /// Parses a declared capability string.
    /// </summary>
    /// <exception cref="CapabilityFormatException">
    /// The capability is unknown, malformed, missing a required scope, or carries a
    /// scope it must not have.
    /// </exception>
    public static Capability Parse(string declaration)
    {
        if (!TryParse(declaration, out var capability, out string? error))
        {
            throw new CapabilityFormatException(error);
        }

        return capability;
    }

    public static bool TryParse(
        string? declaration,
        [NotNullWhen(true)] out Capability? capability,
        [NotNullWhen(false)] out string? error)
    {
        capability = null;

        if (string.IsNullOrWhiteSpace(declaration))
        {
            error = "A capability declaration must not be empty.";
            return false;
        }

        // Trailing/leading whitespace is a formatting slip, not an attack, but the
        // parsed value must be canonical so comparisons stay exact.
        declaration = declaration.Trim();

        string name;
        string? scope = null;

        int separator = declaration.IndexOf(':');
        if (separator >= 0)
        {
            name = declaration[..separator];
            scope = declaration[(separator + 1)..];
        }
        else
        {
            name = declaration;
        }

        if (!CapabilityCatalog.TryGetDefinition(name, out var definition))
        {
            // Rejecting unknown names is deliberate: a typo, or a capability from a newer
            // runtime, must fail loudly rather than load with a quietly reduced grant.
            error = $"Unknown capability '{name}'.";
            return false;
        }

        if (definition.RequiresScope)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                error = $"Capability '{name}' requires a scope, e.g. '{name}:{definition.ScopeExample}'.";
                return false;
            }

            if (!definition.TryNormalizeScope(scope, out string? normalizedScope, out string? scopeError))
            {
                error = $"Capability '{name}' has an invalid scope '{scope}': {scopeError}";
                return false;
            }

            scope = normalizedScope;
        }
        else if (scope is not null)
        {
            error = $"Capability '{name}' does not take a scope, but '{scope}' was supplied.";
            return false;
        }

        capability = new Capability(definition.Name, scope);
        error = null;
        return true;
    }

    public bool Equals(Capability? other)
    {
        if (other is null) return false;

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(Scope, other.Scope, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as Capability);

    public override int GetHashCode() => HashCode.Combine(Name, Scope);
}

/// <summary>Raised when a capability declaration cannot be parsed or is not recognised.</summary>
public sealed class CapabilityFormatException(string message) : Exception(message);
