using MiniTwitch.Helix.Models;

namespace Fumo.Shared.ThirdParty.Helix;

public static class HelixWrapperExtensions
{
    public static async ValueTask<IEnumerable<TData>> PaginationHelper<TResult, TData>(
        this Task<HelixResult<TResult>> initial,
        Action<HelixResult<TResult>> onError,
        CancellationToken ct)
        where TResult : PaginableResponse<TData>
        => await (await initial).PaginationHelper<TResult, TData>(onError, ct);

    public static async ValueTask<IEnumerable<TData>> PaginationHelper<TResult, TData>(
        this HelixResult<TResult> initial,
        Action<HelixResult<TResult>> onError,
        CancellationToken ct)
    where TResult : PaginableResponse<TData>
    {
        List<TData> results = [];

        if (!initial.Success)
        {
            onError(initial);
            return [];
        }

        results.AddRange(initial.Value.Data);

        if (!initial.CanPaginate) return results;

        var current = initial;

        while (await current.Paginate(ct) is HelixResult<TResult> next)
        {
            if (!next.Success)
            {
                onError(next);
                break;
            }

            results.AddRange(next.Value.Data);

            if (!next.CanPaginate) break;

            current = next;

            ct.ThrowIfCancellationRequested();
        }

        return results;
    }
}
