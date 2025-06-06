namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that the attributed code should be excluded from code coverage information.
/// </summary>
/// <remarks>
/// This attribute was added to the assembly because it's not otherwise
/// available to portable class libraries. Marked internal to avoid reuse
/// outside this specific library.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Constructor
        | AttributeTargets.Method
        | AttributeTargets.Property
        | AttributeTargets.Event
        | AttributeTargets.Assembly,
    AllowMultiple = false,
    Inherited = false)]
internal sealed class ExcludeFromCodeCoverageAttribute : Attribute;