using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdsConfig.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdsConfig
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        public IConfiguration Configuration { get; set; }

        public IWebHostEnvironment Environment { get; set; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<IdsDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<IdsDbContext>();

            services.AddIdentityServer()
                .AddAspNetIdentity<IdentityUser>()
                .AddConfigurationStore(con =>
                {
                    con.ConfigureDbContext = configDb => configDb.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(os =>
                {
                    os.ConfigureDbContext = configDb => configDb.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));                    
                })
                .AddDeveloperSigningCredential();

            
            // To configure Quickstart view of Identity server
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // this will do the initial DB population
            InitializeDatabase(app);
            // Add Static files Middleware
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();

            // To authorize the user login
            app.UseAuthorization();

            // To configure Defualt controll route
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients.ToList())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources.ToList())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var resource in Config.ApiScopes.ToList())
                    {
                        context.ApiScopes.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.ApiResources.ToList())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                var ctx = serviceScope.ServiceProvider.GetService<IdsDbContext>();
                ctx.Database.Migrate();
                var address = new
                {
                    street_address = "Potenza Street",
                    locality = "Indian",
                    postal_code = 395002,
                    country = "India"
                };
                var userMgr = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var usr = userMgr.FindByNameAsync("Potenza").Result;
                if (usr == null)
                {
                    usr = new IdentityUser
                    {
                        UserName = "Potenza",
                        Email = "potenzatest@gmail.com",
                        EmailConfirmed = true,
                    };
                    var result = userMgr.CreateAsync(usr, "Potenza@123").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    result = userMgr.AddClaimsAsync(usr, new Claim[]
                    {
                        new Claim(JwtClaimTypes.Name, "Potenza User"),
                        new Claim(JwtClaimTypes.GivenName, "Potenza"),
                        new Claim(JwtClaimTypes.FamilyName, "Potenza"),
                        new Claim(JwtClaimTypes.Role, "admin"),
                        new Claim(JwtClaimTypes.WebSite, "https://potenzagloblsolutions.com"),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address)),
                    }).Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }
                }

                var bob = userMgr.FindByNameAsync("Test").Result;
                if (bob == null)
                {
                    bob = new IdentityUser
                    {
                        UserName = "Test",
                        Email = "testuser@gmail.com",
                        EmailConfirmed = true
                    };
                    var result = userMgr.CreateAsync(bob, "Potenza@123").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    result = userMgr.AddClaimsAsync(bob, new Claim[]
                    {
                      new Claim(JwtClaimTypes.Name, "Test User"),
                      new Claim(JwtClaimTypes.GivenName, "User"),
                      new Claim(JwtClaimTypes.FamilyName, "Test"),
                      new Claim(JwtClaimTypes.WebSite, "http://testuser.com"),
                      new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address))
                    }).Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }
                }
            }
        }
    }
}
