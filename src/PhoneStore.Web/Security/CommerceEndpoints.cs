using Microsoft.AspNetCore.Antiforgery;
using PhoneStore.Core.Commerce;

namespace PhoneStore.Web.Security;

public static class CommerceEndpoints
{
    public static Actor Actor(HttpContext context)
    {
        var session = SessionAuthentication.Current(context);
        return new(session.Account.Id, session.SessionId);
    }
}
public sealed class CommerceCsrfFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store";
        if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method)) await antiforgery.ValidateRequestAsync(http);
        return await next(context);
    }
}
