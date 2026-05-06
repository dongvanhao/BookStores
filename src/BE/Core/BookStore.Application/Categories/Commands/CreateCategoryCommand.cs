namespace BookStore.Application.Categories.Commands;

public record CreateCategoryCommand(string Name, string? Description, Guid? ParentId);
