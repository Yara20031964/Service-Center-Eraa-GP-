namespace Domain.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
        => new() { Success = true, Data = data, Message = message, StatusCode = 201 };

    public static ApiResponse<T> Fail(string message, int statusCode = 400)
        => new() { Success = false, Message = message, StatusCode = statusCode };

    public static ApiResponse<T> NotFound(string message = "Resource not found")
        => Fail(message, 404);

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
        => Fail(message, 401);

    public static ApiResponse<T> Forbidden(string message = "Forbidden")
        => Fail(message, 403);

    public static ApiResponse<T> ServerError(string message = "An unexpected error occurred")
        => Fail(message, 500);
}