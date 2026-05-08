using BookStore.Application.Categories.Commands;
using BookStore.Application.Categories.IService;
using BookStore.Domain.Entities;
using BookStore.Domain.Errors;
using BookStore.Domain.IRepository;
using BookStore.Shared.Results;

namespace BookStore.Application.Categories.Services;

public class CategoryCommandService(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork) : ICategoryCommandService
{
    public async Task<Result<Guid>> CreateAsync(CreateCategoryCommand cmd, CancellationToken ct = default)
    {
        if (cmd.ParentId.HasValue)
        {
            var parent = await categoryRepo.GetByIdAsync(cmd.ParentId.Value, ct);
            if (parent is null)
                return CategoryErrors.ParentNotFound(cmd.ParentId.Value);
        }

        var category = Category.Create(cmd.Name, cmd.Description, cmd.ParentId);
        categoryRepo.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateCategoryCommand cmd, CancellationToken ct = default)
    {
        var category = await categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure(CategoryErrors.NotFound(id));

        if (cmd.ParentId.HasValue)
        {
            var parent = await categoryRepo.GetByIdAsync(cmd.ParentId.Value, ct);
            if (parent is null)
                return Result.Failure(CategoryErrors.ParentNotFound(cmd.ParentId.Value));

            var descendants = await categoryRepo.GetDescendantIdsAsync(id, ct);
            if (descendants.Contains(cmd.ParentId.Value))
                return Result.Failure(CategoryErrors.CircularReference);
        }

        var updateResult = category.Update(cmd.Name, cmd.Description, cmd.ParentId);
        if (!updateResult.IsSuccess)
            return updateResult;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PatchAsync(Guid id, PatchCategoryCommand cmd, CancellationToken ct = default)
    {
        var category = await categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure(CategoryErrors.NotFound(id));

        var newName        = cmd.Name        ?? category.Name;
        var newDescription = cmd.Description ?? category.Description;
        var newParentId    = cmd.ParentId    ?? category.ParentId;

        if (cmd.ParentId.HasValue && cmd.ParentId != category.ParentId)
        {
            var parent = await categoryRepo.GetByIdAsync(cmd.ParentId.Value, ct);
            if (parent is null)
                return Result.Failure(CategoryErrors.ParentNotFound(cmd.ParentId.Value));

            var descendants = await categoryRepo.GetDescendantIdsAsync(id, ct);
            if (descendants.Contains(cmd.ParentId.Value))
                return Result.Failure(CategoryErrors.CircularReference);
        }

        var updateResult = category.Update(newName, newDescription, newParentId);
        if (!updateResult.IsSuccess)
            return updateResult;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categoryRepo.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Failure(CategoryErrors.NotFound(id));

        if (await categoryRepo.HasChildrenAsync(id, ct))
            return Result.Failure(CategoryErrors.HasChildren);

        if (await categoryRepo.HasBooksAsync(id, ct))
            return Result.Failure(CategoryErrors.HasBooks);

        categoryRepo.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
