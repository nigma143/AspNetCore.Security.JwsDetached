using System;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Security.JwsDetached.DependencyInjection
{
    public static class JwsDetachedServiceCollectionExtensions
    {
        public static IServiceCollection AddJwsDetached(this IServiceCollection services, Action<JwsDetachedOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.Configure(configure);

            return services;
        }
    }
}
