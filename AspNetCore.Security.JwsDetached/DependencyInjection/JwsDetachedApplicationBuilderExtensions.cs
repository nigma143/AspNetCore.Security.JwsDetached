using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore.Security.JwsDetached.DependencyInjection
{
    public static class JwsDetachedApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHttpJwsDetached(this IApplicationBuilder app)
        {
            return app.UseMiddleware<JwsDetachedMiddleware>();
        }
    }
}
