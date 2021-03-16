using System;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Security.JwsDetached.DependencyInjection
{
    public static class JwsDetachedServiceCollectionExtensions
    {
        public static IServiceCollection AddJwsDetached<TV, TS>(this IServiceCollection services)
            where TV : class, IVerifierResolverSelector
            where TS : class, ISignContextSelector
        {
            return services.AddJwsDetached<TV, TS>(_ => { });
        }


        public static IServiceCollection AddJwsDetached<TV, TS>(this IServiceCollection services,
            Action<JwsDetachedOptions> configure)
            where TV : class, IVerifierResolverSelector
            where TS : class, ISignContextSelector
        {
            services.Configure(configure);

            services.AddSingleton<IVerifierResolverSelector, TV>();
            services.AddSingleton<ISignContextSelector, TS>();

            return services;
        }
    }
}
