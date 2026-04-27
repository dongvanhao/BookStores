namespace BookStore.Shared.Responses;

public class ApiResponse<T>
{
    private ApiResponse() { }

    public bool    Success   { get; private set; }
    public T?      Data      { get; private set; }
    public string  Message   { get; private set; } = string.Empty;
    public string? ErrorCode { get; private set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, string? errorCode = null)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}

//non-generic cho các response không có data (vd: Delete)
public class ApiResponse
{
    private ApiResponse() { }

    public bool    Success   { get; private set; }
    public string  Message   { get; private set; } = string.Empty;
    public string? ErrorCode { get; private set; }

    public static ApiResponse Ok(string message = "Success")
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, string? errorCode = null)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}
