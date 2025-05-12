using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;

namespace SapRfcMicroservice
{
    public class Startup
    {
        //private readonly string StaticToken;

        public Startup()
        {
            //StaticToken = Environment.GetEnvironmentVariable("STATIC_API_TOKEN") ?? "Bearer fallback-token";
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<SapService>();
            services.AddSingleton<AesCryptoService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SAP RFC Microservice API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter your static token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAP RFC Microservice API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                //if (!context.Request.Headers.TryGetValue("Authorization", out var token) || token != StaticToken)
                //{
                //    context.Response.StatusCode = 401;
                //    await context.Response.WriteAsync("Unauthorized");
                //    return;
                //}
                await next();
            });

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
