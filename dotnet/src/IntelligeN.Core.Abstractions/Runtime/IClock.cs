namespace IntelligeN.Core.Abstractions.Runtime;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
