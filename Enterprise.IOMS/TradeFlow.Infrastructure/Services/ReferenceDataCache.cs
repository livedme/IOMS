namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Cache contract shared by the brand, category and warehouse services.
/// </summary>
/// <remarks>
/// All three previously lived on <see cref="ProductService"/> behind one pair of private constants.
/// Splitting them across three services without hoisting the contract is what left all three
/// referencing identifiers that did not exist in their own file, so the contract is stated once,
/// here. The prefix and lifetime are unchanged, which means existing cached entries stay valid
/// and a write to any of the three still clears all three lists — deliberate, since the lists are
/// small, are read together on most pages, and are cheap to rebuild.
/// </remarks>
internal static class ReferenceDataCache
{
    /// <summary>Key prefix for the brand, category and warehouse reference lists.</summary>
    public const string Prefix = "products:reference:";

    /// <summary>Reference lists change rarely and are read on every page render, so they are cached.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
}
