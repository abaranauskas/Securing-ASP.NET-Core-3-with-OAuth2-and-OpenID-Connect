// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aidas.IDP
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var aidasIDPDataBAseConnectionString =
                "Server=(localdb)\\mssqllocaldb;Database=AidasIDPDataDB;Trusted_Connection=True;";

            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer()
                //.AddInMemoryIdentityResources(Config.Ids)
                //.AddInMemoryApiResources(Config.Apis)
                //.AddInMemoryClients(Config.Clients)                //instead of using inmemory store using sql server. Solves load balancing issues
                .AddTestUsers(TestUsers.Users);

            // not recommended for production - you need to store your key material somewhere secure
            //builder.AddDeveloperSigningCredential();

            builder.AddSigningCredential(LoadCertificateFromStore());

            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            //set up of sql server for identityserver
            //configurationsstore where client api and so on are stored
            builder.AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(
                        aidasIDPDataBAseConnectionString,
                        opt => opt.MigrationsAssembly(migrationAssembly) //stating that migration can be found in this assemble
                    );
            });

            //AddOperationalStore where tokens and so on are stored
            builder.AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(
                        aidasIDPDataBAseConnectionString,
                        opt => opt.MigrationsAssembly(migrationAssembly) //stating that migration can be found in this assemble
                    );
            });

        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            InitializeDatabase(app);

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
        }

        public X509Certificate2 LoadCertificateFromStore()
        {
            string thumbPrint = "826b1ba5f76de4c7aeca333383b52eca200b5411";

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint,
                    thumbPrint, true);
                if (certCollection.Count == 0)
                {
                    throw new Exception("The specified certificate wasn't found.");
                }

                return certCollection[0];
            }
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider
                    .GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider
                    .GetRequiredService<ConfigurationDbContext>();

                context.Database.Migrate();

                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }

                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.Ids)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.Apis)
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }

                    context.SaveChanges();
                }
            }
        }
    }
}
