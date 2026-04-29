using IntelligeN.Core.Abstractions.Runtime;

namespace IntelligeN.Core.Runtime;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
