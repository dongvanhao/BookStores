namespace BookStore.Application.Categories.Commands;

public record PatchCategoryCommand(string? Name, string? Description, Guid? ParentId);
