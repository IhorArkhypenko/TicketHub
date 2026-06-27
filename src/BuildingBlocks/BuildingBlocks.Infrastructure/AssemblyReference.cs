using System.Reflection;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Marker for assembly scanning. Cross-cutting infrastructure (Outbox/Inbox, MassTransit
/// conventions, Redis wrappers) is added here in later phases.
/// </summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
