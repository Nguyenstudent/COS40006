using System.Reflection;
using GradeTracker.Core.Models;
using Newtonsoft.Json;

namespace GradeTracker.Data;

/// <summary>
/// Stores unit results in a JSON file.
///
/// Deployment note: the file lives in %LOCALAPPDATA%\GradeTracker, NOT in the
/// install folder. After an MSI install the app sits in C:\Program Files, which
/// is read-only for standard users, so writing next to the .exe would fail with
/// UnauthorizedAccessException.
/// </summary>
public class JsonUnitRepository
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented
    };

    public JsonUnitRepository(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GradeTracker",
            "units.json");
    }

    public string FilePath { get; }

    /// <summary>The serializer assembly (Newtonsoft.Json.dll), used by the About box.</summary>
    public static Assembly SerializerAssembly => typeof(JsonConvert).Assembly;

    public List<UnitResult> Load()
    {
        if (!File.Exists(FilePath))
        {
            return new List<UnitResult>();
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<List<UnitResult>>(json, Settings) ?? new List<UnitResult>();
        }
        catch (JsonException ex)
        {
            throw new DataStoreException($"The data file is corrupted: {FilePath}", ex);
        }
        catch (IOException ex)
        {
            throw new DataStoreException($"Could not read the data file: {FilePath}", ex);
        }
    }

    public void Save(IEnumerable<UnitResult> units)
    {
        try
        {
            string? folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string json = JsonConvert.SerializeObject(units, Settings);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DataStoreException($"Could not save the data file: {FilePath}", ex);
        }
    }
}
