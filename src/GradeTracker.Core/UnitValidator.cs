using GradeTracker.Core.Models;

namespace GradeTracker.Core;

/// <summary>
/// Validates user input before it is stored.
/// </summary>
public static class UnitValidator
{
    public static List<string> Validate(UnitResult unit, IEnumerable<UnitResult> existingUnits)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(unit.Code))
        {
            errors.Add("Unit code is required.");
        }
        else if (existingUnits.Any(u => string.Equals(u.Code, unit.Code, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Unit {unit.Code} has already been added.");
        }

        if (string.IsNullOrWhiteSpace(unit.Name))
        {
            errors.Add("Unit name is required.");
        }

        if (unit.CreditPoints <= 0)
        {
            errors.Add("Credit points must be greater than zero.");
        }

        if (unit.Mark < GradeCalculator.MinMark || unit.Mark > GradeCalculator.MaxMark)
        {
            errors.Add("Mark must be between 0 and 100.");
        }

        return errors;
    }
}
