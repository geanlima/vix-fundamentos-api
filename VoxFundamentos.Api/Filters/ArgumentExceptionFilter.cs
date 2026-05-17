using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VoxFundamentos.Api.Filters;

public sealed class ArgumentExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ArgumentException ex)
            return;

        context.Result = new BadRequestObjectResult(new { message = ex.Message });
        context.ExceptionHandled = true;
    }
}
