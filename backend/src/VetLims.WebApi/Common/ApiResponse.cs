namespace VetLims.WebApi.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }

    public static ApiResponse<T> SuccessResult(T? data, string? message = null, int statusCode = 200)
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };

    public static ApiResponse<T> FailResult(string message, int statusCode = 400, IEnumerable<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList() ?? new List<string>(),
            StatusCode = statusCode
        };
}
