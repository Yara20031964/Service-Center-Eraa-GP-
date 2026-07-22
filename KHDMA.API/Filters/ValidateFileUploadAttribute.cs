using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KHDMA.API.Filters;

public class ValidateFileUploadAttribute : ActionFilterAttribute
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var files = context.HttpContext.Request.Form.Files;

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!AllowedExtensions.Contains(ext))
            {
                context.Result = new BadRequestObjectResult(
                    new { success = false, message = $"File type {ext} not allowed. Allowed: jpg, png, pdf" });
                return;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                context.Result = new BadRequestObjectResult(
                    new { success = false, message = "File size exceeds 10MB limit" });
                return;
            }
        }
    }
}