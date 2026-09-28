namespace GradeTracker.Core.Models;

/// <summary>
/// One completed unit of study and the mark received for it.
/// </summary>
public class UnitResult
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Credit points for the unit (a standard unit is 12.5).</summary>
    public double CreditPoints { get; set; } = 12.5;

    /// <summary>Final mark out of 100.</summary>
    public double Mark { get; set; }
}
