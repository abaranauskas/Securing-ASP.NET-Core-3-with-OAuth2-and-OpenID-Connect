using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.IdentityModel.Tokens;
using IdentityModel;
using ImageGallery.Client.HttpHandlers;

namespace ImageGallery.Client
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); //pasalina skirtinga mapinima dabar client claims atitinka JWT token claim names
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                 .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddAuthorization(cfg =>
            {
                cfg.AddPolicy("CanOrderFrame", policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.RequireClaim("country", "be", "usa", "fr", "etc.");
                    policyBuilder.RequireClaim("subscriptionlevel", "PayingUser");
                });
            });

            services.AddHttpContextAccessor(); // for BearerTokenHandler
            services.AddTransient<BearerTokenHandler>();

            // create an HttpClient used for accessing the API
            services.AddHttpClient("APIClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44366/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            })
                .AddHttpMessageHandler<BearerTokenHandler>(); //central way to provide access token for API end points;

            // create an HttpClient used for accessing the IDP
            services.AddHttpClient("IDPClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, option =>
                    {
                        option.AccessDeniedPath = "/Authorization/ActionDenied/";
                    })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.Authority = "https://localhost:5001/";
                        options.ClientId = "imagegalleryclient";
                        options.ResponseType = "code";
                        //options.UsePkce = false; //true by default
                        //options.CallbackPath = new PathString("..."); //RedirectUris "https://localhost:44389/signin-oidc" jei norima pasikeist default
                        //options.SignedOutCallbackPath = new PathString("https://localhost:44389/signout-callback-oidc"); //defaultinis del to jo nereikia bet IDP budina prie client uzregistruoti.
                        options.Scope.Add("openid"); //nebutina nes defaultu pareikalaujama
                        options.Scope.Add("profile");  //nebutina nes defaultu pareikalaujama
                        options.Scope.Add("address");
                        options.Scope.Add("roles");
                        options.Scope.Add("imagegalleryapi");
                        options.Scope.Add("country");
                        options.Scope.Add("subscriptionlevel");
                        options.Scope.Add("offline_access"); // enabling refresh tokens
 
                        //options.ClaimActions.Remove("exp"); //default middleware atfiltruoja claims kuriu clinetui nereikia pvz expire token time. tokiu budu galima perconfiguruoti kad clienta sitas claimas pasiektu
                         options.ClaimActions.DeleteClaim("sid"); //tokiu budu galima pasalinti claims kuriuos middlware neatfiltruoja bet mums ju cliente nereikia
                        options.ClaimActions.DeleteClaim("idp");
                        options.ClaimActions.DeleteClaim("s_hash");
                        options.ClaimActions.DeleteClaim("auth_time");
                        //options.ClaimActions.DeleteClaim("address"); //nebus jo claimsete nes nera mappinimo source code. todel nereikia deletint

                        options.ClaimActions.MapUniqueJsonKey("role", "role"); //defaulto role nera MapUniqueJsonKey source code
                        options.ClaimActions.MapUniqueJsonKey("country", "country");
                        options.ClaimActions.MapUniqueJsonKey("subscriptionlevel", "subscriptionlevel");

                        options.SaveTokens = true;
                        options.ClientSecret = "secret";
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = JwtClaimTypes.GivenName,
                            RoleClaimType = JwtClaimTypes.Role,
                        };
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
                // The default HSTS value is 30 days. You may want to change this for
                // production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}
