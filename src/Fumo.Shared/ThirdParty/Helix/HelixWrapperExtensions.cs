using MiniTwitch.Helix.Models;

namespace Fumo.Shared.ThirdParty.Helix;

public static class HelixWrapperExtensions
{
    public static async ValueTask<List<TData>> PaginationHelper<TResult, TData>(
        this Task<HelixResult<TResult>> initial,
        Action<HelixResult<TResult>> onError,
        CancellationToken ct)
        where TResult : PaginableResponse<TData>
        => await (await initial).PaginationHelper<TResult, TData>(onError, ct);

    public static async ValueTask<List<TData>> PaginationHelper<TResult, TData>(
        this HelixResult<TResult> initial,
        Action<HelixResult<TResult>> onError,
        CancellationToken ct)
    where TResult : PaginableResponse<TData>
    {
        List<TData> results = [];

        var current = initial;

        do
        {
            if (!current.Success)
            {
                onError(current);
                break;
            }

            results.AddRange(current.Value.Data);

            if (!current.CanPaginate) break;

            current = await current.Paginate(ct);
        } while (current.CanPaginate);

        return results;
    }
}
