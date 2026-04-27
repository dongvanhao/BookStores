namespace BookStore.Shared.Results;

public sealed class Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new("General.Null", "A null value was provided.", ErrorType.Failure);

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound); // Lỗi không tìm thấy
    public static Error Validation(string code, string description)
        => new(code, description, ErrorType.Validation); // Lỗi xác thực
    public static Error Failure(string code, string description)
        => new(code, description, ErrorType.Failure); // Lỗi chung
    public static Error Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict); // Lỗi xung đột
    public static Error Unauthorized(string code, string description)
        => new(code, description, ErrorType.Unauthorized); // Lỗi không có quyền truy cập
        public static Error Unexpected(string code, string description)
        => new(code, description, ErrorType.Unexpected); // Lỗi không mong muốn

    public override string ToString() => $"{Code} - {Description}";
}

