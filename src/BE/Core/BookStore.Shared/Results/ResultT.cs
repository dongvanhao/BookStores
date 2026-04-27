namespace BookStore.Shared.Results;

public class Result<TValue> : Result
{
    private readonly TValue? _value;
    internal Result(bool isSuccess, Error error, TValue? value)
        : base(isSuccess, error)
    {
        _value = value;
    }
    //Chỉ lấy được Value khi IsSuccess = tránh bug do quên check
    public TValue Value => IsSuccess 
        ? _value 
        : throw new InvalidOperationException("Cannot access value of a failure result.");
    
    //Implicit conversion cho code gọn hơn
    public static implicit operator Result<TValue>(TValue value) => Result.Success(value);
    public static implicit operator Result<TValue>(Error error) => Result.Failure<TValue>(error);

    #region Tại sao dùng Implicit conversion.
    /*
    // Không cần: return Result.Success(book);
    // Không cần: return Result.Failure<Book>(BookErrors.NotFound);
    // Thay vào đó viết thẳng:
    public async Task<Result<Book>> GetByIdAsync(Guid id)
    {
        var book = await _repo.GetByIdAsync(id);
        if (book is null) return BookErrors.NotFound(id);  // implicit cast
        return book;                                        // implicit cast
    }
    */
    
    #endregion
}