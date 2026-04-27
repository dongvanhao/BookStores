namespace BookStore.Shared.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        //không cho phép trạng thái mâu thuẫn
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");
        
        IsSuccess = isSuccess;
        Error = error;
    }
    public bool IsSuccess {get; }
    public bool IsFailure => !IsSuccess;
    public Error Error {get; }
    
    public static Result Success()              => new(true, Error.None);
    public static Result Failure(Error error)   => new(false, error);
    
    //dùng khi có giá trị trả về
    public static Result<TValue> Success<TValue>(TValue value) => new(true, Error.None, value);
    public static Result<TValue> Failure<TValue>(Error error)  => new(false, error, default!);

    

}