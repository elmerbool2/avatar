﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nancy.Owin;
using System;
using ei8.Avatar.Port.Adapter.Common;

namespace ei8.Avatar.Port.Adapter.In.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = Environment.GetEnvironmentVariable(EnvironmentVariableKeys.TokenIssuerAddress);
                    options.RequireHttpsMetadata = false;
                    options.ApiSecret = "secret";
                    options.ApiName = "avatar";
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseOwin(buildFunc => buildFunc.UseNancy());
        }
    }
}
