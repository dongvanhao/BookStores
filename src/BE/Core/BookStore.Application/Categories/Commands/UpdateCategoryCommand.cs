namespace BookStore.Application.Categories.Commands;

public record UpdateCategoryCommand(string Name, string? Description, Guid? ParentId);
