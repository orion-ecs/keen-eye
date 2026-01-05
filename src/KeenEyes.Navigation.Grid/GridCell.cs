using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Represents a single cell in a navigation grid.
/// </summary>
/// <remarks>
/// Each cell stores walkability information and an area type for cost calculations.
/// </remarks>
public struct GridCell
{
    /// <summary>
    /// Gets or sets whether this cell is walkable.
    /// </summary>
    public bool IsWalkable;

    /// <summary>
    /// Gets or sets the area type of this cell.
    /// </summary>
    public NavAreaType AreaType;

    /// <summary>
    /// Gets or sets the base movement cost for this cell (before area cost multiplier).
    /// </summary>
    /// <remarks>
    /// Default value is 1.0. Higher values make the cell less preferable.
    /// A value of 0 or negative means the cell is unwalkable regardless of <see cref="IsWalkable"/>.
    /// </remarks>
    public float Cost;

    /// <summary>
    /// Gets or sets custom user data for this cell.
    /// </summary>
    /// <remarks>
    /// Can be used to store game-specific information such as terrain type,
    /// elevation, or other metadata.
    /// </remarks>
    public int UserData;

    /// <summary>
    /// Creates a walkable cell with default settings.
    /// </summary>
    public static GridCell Walkable => new()
    {
        IsWalkable = true,
        AreaType = NavAreaType.Walkable,
        Cost = 1f,
        UserData = 0
    };

    /// <summary>
    /// Creates a blocked (unwalkable) cell.
    /// </summary>
    public static GridCell Blocked => new()
    {
        IsWalkable = false,
        AreaType = NavAreaType.NotWalkable,
        Cost = 0f,
        UserData = 0
    };

    /// <summary>
    /// Creates a walkable cell with a specific area type.
    /// </summary>
    /// <param name="areaType">The area type for the cell.</param>
    /// <param name="cost">The base movement cost (default 1.0).</param>
    /// <returns>A new walkable cell.</returns>
    public static GridCell WithAreaType(NavAreaType areaType, float cost = 1f) => new()
    {
        IsWalkable = true,
        AreaType = areaType,
        Cost = cost,
        UserData = 0
    };

    /// <summary>
    /// Gets the effective walkability considering both <see cref="IsWalkable"/> and <see cref="Cost"/>.
    /// </summary>
    /// <returns>True if the cell can be traversed.</returns>
    public readonly bool CanTraverse() => IsWalkable && Cost > 0f;
}
