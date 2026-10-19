// Grade Tracker CLI - SWE40006 Task 4.4 (non-web app in a Docker container).
//
// Reads units (Code,Name,CreditPoints,Mark) from a CSV file, or from standard input when the
// argument is "-", validates every row with GradeTracker.Core and prints grade, WAM and GPA.
//
// Exit codes: 0 = all rows valid, 1 = some rows rejected, 2 = file not found or no valid units.
using System.Globalization;
using GradeTracker.Core;
using GradeTracker.Core.Models;

const string DefaultFile = "/app/sample-units.csv";
var line = new string('-', 75);
var inv = CultureInfo.InvariantCulture;

if (args.Length > 0 && args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: gradetracker-cli [file.csv | -]");
    Console.WriteLine("  file.csv  CSV file with the header Code,Name,CreditPoints,Mark (default: the sample file)");
    Console.WriteLine("  -         read the CSV from standard input (docker run -i ...)");
    return 0;
}

string source = args.Length > 0 ? args[0] : DefaultFile;
TextReader reader;
if (source == "-")
{
    reader = Console.In;
}
else if (File.Exists(source))
{
    reader = File.OpenText(source);
}
else
{
    // A container cannot see files on the host unless the folder is mounted (error E3).
    Console.Error.WriteLine($"Error: file '{source}' not found inside the container.");
    Console.Error.WriteLine("Mount the folder that holds it, e.g.  docker run --rm -v \"${PWD}:/data\" <image> /data/units.csv");
    return 2;
}

var units = new List<UnitResult>();
int rejected = 0;
int lineNo = 0;
bool headerChecked = false;

using (reader)
{
    string? raw;
    while ((raw = reader.ReadLine()) != null)
    {
        lineNo++;
        string text = raw.Trim();
        if (text.Length == 0 || text.StartsWith('#'))
        {
            continue;                                   // blank line or comment
        }
        if (!headerChecked)
        {
            headerChecked = true;
            if (text.StartsWith("Code,", StringComparison.OrdinalIgnoreCase))
            {
                continue;                               // header row
            }
        }

        string[] parts = text.Split(',');
        if (parts.Length != 4)
        {
            Reject($"expected Code,Name,CreditPoints,Mark - got \"{text}\"");
            continue;
        }
        if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, inv, out double creditPoints))
        {
            Reject($"credit points \"{parts[2].Trim()}\" is not a number.");
            continue;
        }
        if (!double.TryParse(parts[3].Trim(), NumberStyles.Float, inv, out double mark))
        {
            Reject($"mark \"{parts[3].Trim()}\" is not a number.");
            continue;
        }

        var unit = new UnitResult
        {
            Code = parts[0].Trim().ToUpperInvariant(),
            Name = parts[1].Trim(),
            CreditPoints = creditPoints,
            Mark = mark
        };

        // Same validation rules as the Task 1 desktop app and the Task 3 web app.
        List<string> errors = UnitValidator.Validate(unit, units);
        if (errors.Count > 0)
        {
            Reject(string.Join(" ", errors));
            continue;
        }
        units.Add(unit);
    }
}

var coreVersion = typeof(GradeCalculator).Assembly.GetName().Version;
Console.WriteLine($"Grade Tracker CLI 1.0 - GradeTracker.Core {coreVersion} - .NET {Environment.Version} on {Environment.MachineName}");
Console.WriteLine();

if (units.Count == 0)
{
    Console.Error.WriteLine("Error: no valid units found.");
    return 2;
}

Console.WriteLine($"{"Code",-10}{"Unit",-38}{"CP",6}{"Mark",7}  Grade");
Console.WriteLine(line);
foreach (var u in units)
{
    Grade grade = GradeCalculator.GetGrade(u.Mark);
    string name = u.Name.Length > 36 ? u.Name[..33] + "..." : u.Name;
    Console.WriteLine(string.Format(inv, "{0,-10}{1,-38}{2,6:0.0}{3,7:0.##}  {4} ({5})",
        u.Code, name, u.CreditPoints, u.Mark, grade, GradeCalculator.GetGradeName(grade)));
}
Console.WriteLine(line);
Console.WriteLine(string.Format(inv, "Units: {0}   Credit points: {1:0.0}   WAM: {2:0.00}   GPA: {3:0.00} / 4",
    units.Count, units.Sum(u => u.CreditPoints), GradeCalculator.CalculateWam(units), GradeCalculator.CalculateGpa(units)));

if (rejected > 0)
{
    Console.WriteLine($"{rejected} row(s) rejected - see the messages above.");
    return 1;
}
return 0;

void Reject(string message)
{
    Console.Error.WriteLine($"Line {lineNo}: {message}");
    rejected++;
}
