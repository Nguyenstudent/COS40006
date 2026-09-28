using GradeTracker.Core.Models;

namespace GradeTracker.Core;

/// <summary>
/// Pure calculation logic. Kept in its own DLL so it can be patched
/// (e.g. a change to grade bands) without rebuilding the user interface.
/// </summary>
public static class GradeCalculator
{
    public const double MinMark = 0;
    public const double MaxMark = 100;

    public static Grade GetGrade(double mark)
    {
        if (mark < MinMark || mark > MaxMark)
        {
            throw new ArgumentOutOfRangeException(nameof(mark), mark, "Mark must be between 0 and 100.");
        }

        return mark switch
        {
            >= 80 => Grade.HD,
            >= 70 => Grade.D,
            >= 60 => Grade.C,
            >= 50 => Grade.P,
            _ => Grade.N
        };
    }

    public static string GetGradeName(Grade grade) => grade switch
    {
        Grade.HD => "High Distinction",
        Grade.D => "Distinction",
        Grade.C => "Credit",
        Grade.P => "Pass",
        _ => "Fail"
    };

    /// <summary>Grade points on a 4-point scale (HD = 4 ... N = 0).</summary>
    public static int GetGradePoint(Grade grade) => grade switch
    {
        Grade.HD => 4,
        Grade.D => 3,
        Grade.C => 2,
        Grade.P => 1,
        _ => 0
    };

    /// <summary>Weighted Average Mark: marks weighted by credit points.</summary>
    public static double CalculateWam(IEnumerable<UnitResult> units)
    {
        var list = units.ToList();
        double totalCredit = list.Sum(u => u.CreditPoints);
        if (totalCredit <= 0)
        {
            return 0;
        }

        return list.Sum(u => u.Mark * u.CreditPoints) / totalCredit;
    }

    /// <summary>Grade Point Average: grade points weighted by credit points.</summary>
    public static double CalculateGpa(IEnumerable<UnitResult> units)
    {
        var list = units.ToList();
        double totalCredit = list.Sum(u => u.CreditPoints);
        if (totalCredit <= 0)
        {
            return 0;
        }

        return list.Sum(u => GetGradePoint(GetGrade(u.Mark)) * u.CreditPoints) / totalCredit;
    }
}
