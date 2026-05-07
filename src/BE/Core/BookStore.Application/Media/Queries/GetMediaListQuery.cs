using BookStore.Domain.Enums;

namespace BookStore.Application.Media.Queries;

public sealed class GetMediaListQuery
{
    public string?    Module { get; init; }
    public MediaType? Type   { get; init; }
    public DateTime?  Before { get; init; }

    private int _limit = 20;
    public int Limit
    {
        get => _limit;
        init => _limit = value <= 0 ? 20 : Math.Min(value, 50);
    }
}
