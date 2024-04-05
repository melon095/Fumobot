namespace Fumo.Application.Startable;

public interface IAsyncStartable
{
    ValueTask Start(CancellationToken ct);
}
