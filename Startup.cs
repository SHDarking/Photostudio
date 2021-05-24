using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Photostudio.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Photostudio
{
    public class Startup
    {
        
        public static IConfiguration Configuration { get; private set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => //CookieAuthenticationOptions
                {
                    options.LoginPath = new PathString("/Account/Login");
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(40);
                    options.Cookie.Name = ".Photostudio.Cookies";
                });
            services.AddControllersWithViews(mvcOptions => { mvcOptions.EnableEndpointRouting = false; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
            app.UseRouting();
            
            app.UseSession();
            app.UseAuthentication();    // аутентификация
            app.UseAuthorization();     // авторизация

            app.UseMvcWithDefaultRoute();

        }
    }
}
