namespace Domain.Common;

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public IEnumerable<T> Data { get; set; } = [];

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResponse<T> Ok(
        IEnumerable<T> data,
        int totalCount,
        int page,
        int pageSize,
        string message = "Success")
        => new()
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

    public static PagedResponse<T> Fail(string message, int statusCode = 400)
        => new() { Success = false, Message = message, StatusCode = statusCode };
}