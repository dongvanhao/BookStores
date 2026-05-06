using BookStore.Shared.Common;

namespace BookStore.Application.Categories.Queries;

public sealed class GetCategoriesQuery : QueryParams
{
    public Guid? ParentId { get; set; }
}
