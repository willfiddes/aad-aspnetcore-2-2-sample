using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using SampleApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Used for OpenIdConnect Debugging Only
            IdentityModelEventSource.ShowPII = true;

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;

            });

            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));


            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme,options =>
            {
                ConfigureOpenIdConnect(options);
            });

            services.AddHttpContextAccessor();
            services.AddSingleton<IMsalService, MsalService>();

            services.AddScoped<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                IMsalService msalService = serviceProvider.GetRequiredService<IMsalService>();
                IHttpContextAccessor httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                string MsGraphInstance = Configuration["MicrosoftGraph:Endpoint"];
                string MsGraphVersion = Configuration["MicrosoftGraph:Version"];
                string MsGraphEndpoint = $"{MsGraphInstance}/{MsGraphVersion}";
                            
                return new GraphServiceClient(MsGraphEndpoint, new MsalAuthenticationProvider(msalService, httpContext, MsGraphInstance));
            });

            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void ConfigureOpenIdConnect(OpenIdConnectOptions options)
        {
            options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("offline_access");
            options.Scope.Add(Configuration["MicrosoftGraph:Scopes"]);

            var codeReceivedHandler = options.Events.OnAuthorizationCodeReceived;

            options.Events.OnAuthorizationCodeReceived = async context =>
            {
                var code = context.ProtocolMessage.Code;
                var msalService = context.HttpContext.RequestServices.GetRequiredService<IMsalService>();
                var AccessToken = String.Empty;

                AccessToken = await msalService.GetTokenUsingAuthorizationCode(code);

                context.HandleCodeRedemption(AccessToken, context.ProtocolMessage.IdToken);
            };
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
