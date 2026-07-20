using Microsoft.AspNetCore.Mvc.Filters;

namespace yonetimpaneli.Filters;

public sealed class NoCacheAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate";

        context.HttpContext.Response.Headers.Pragma = "no-cache";
        context.HttpContext.Response.Headers.Expires = "0";

        base.OnResultExecuting(context);
    }
}