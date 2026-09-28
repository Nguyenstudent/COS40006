namespace GradeTracker.Data;

/// <summary>
/// Raised when the data file cannot be read or written.
/// </summary>
public class DataStoreException : Exception
{
    public DataStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
