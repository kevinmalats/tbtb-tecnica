using Tbtb.Application.Abstractions;

namespace Tbtb.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
