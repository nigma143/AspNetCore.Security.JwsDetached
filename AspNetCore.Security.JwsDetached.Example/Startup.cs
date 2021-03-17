using System;
using System.Threading.Tasks;
using AspNetCore.Security.JwsDetached.DependencyInjection;
using AspNetCore.Security.JwsDetached.Example.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Security.JwsDetached.Example
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJwsDetached<VerifierResolverSelector, SignContextSelector>();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .Use(HandlerJwsDetachedError)
                .UseRouting()
                .UseHttpJwsDetached()
                .UseEndpoints(builder => builder.MapControllers());
        }

        private static async Task HandlerJwsDetachedError(HttpContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (JwsDetachedException ex)
            {
                Console.WriteLine(ex);

                switch (ex.Type)
                {
                    case JwsDetachedType.HeaderNotFound:
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Header not found");
                        break;

                    case JwsDetachedType.InvalidSignature:
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid signature");
                        break;

                    case JwsDetachedType.ReadError:
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Read error");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
