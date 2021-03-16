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
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            var options = app.ApplicationServices.GetRequiredService<IOptions<JwsDetachedOptions>>();

            return app.UseMiddleware<JwsDetachedMiddleware>();
        }
    }
}
