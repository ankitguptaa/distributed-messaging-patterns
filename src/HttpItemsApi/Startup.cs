using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace HttpItemsApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpContextAccessor();
            
            services.AddStackExchangeRedisCache(options =>
            {
                var redisConf = Configuration.GetSection("redis");
                options.ConfigurationOptions = new ConfigurationOptions
                {
                    Password = redisConf.GetValue<string>("Password"),
                    EndPoints = { redisConf.GetValue<string>("Host") },
                    //silently retry in the background if the Redis connection is temporarily down
                    AbortOnConnectFail = false
                };

                // This allows partitioning a single backend cache for use with multiple apps/services.
                options.InstanceName = "ItemsApi";
            });
            
            // rating limiting config
            services.Configure<ClientRateLimitOptions>(Configuration.GetSection("ClientRateLimiting"));
            services.AddSingleton<IRateLimitCounterStore,DistributedCacheRateLimitCounterStore>();
            services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "HttpItemsApi", Version = "v1"});
            });
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HttpItemsApi v1"));
            }

            app.UseRouting();
            app.UseClientRateLimiting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}