namespace Tbtb.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
