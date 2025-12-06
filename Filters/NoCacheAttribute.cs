using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Codenex.Filters
{
    /// <summary>
    /// Filter attribute to disable caching for API endpoints.
    /// Adds cache control headers to ensure fresh data after content changes.
    /// </summary>
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult)
            {
                // Set cache control headers to prevent browser caching
                context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";
                
                // Add timestamp header for client-side tracking
                context.HttpContext.Response.Headers["X-Content-Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }

            base.OnActionExecuted(context);
        }
    }
}
