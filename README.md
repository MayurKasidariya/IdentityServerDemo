# How to Setup Identity Server with SqlServer

### IdentityServer4 is an OpenID Connect and OAuth 2.0 framework for ASP.NET Core.
It enables the following features in your applications:
- Authentication as a Service
- Single Sign-on / Sign-out
- Access Control for APIs
- Federation Gateway
- Focus on Customization
- Mature Open Source
- Free and Commercial Support

This solution has 3 projects based on ASP.NET CORE(.NET 5.0)
1. Identity Server Configuration
2. WebApi Project Protected with Identity Server
3. Web Application Authentication By Open id connect

for user authentication.

* Authorization Code flow - This is the recommended approach to OpenId Connect authentication. It will redirect the user to an Identity Server login page before returning to your app. See `Startup.cs` for configuring this approach.
* Open Id connect authentication is authenticate based on identity server, In identity server, we have defined clients with client Id and client secret password see IdsConfig - Config.cs

This app also includes an example of obtaining an OAuth2 `access_token` for use in accessing the APIs The `Weather` route in the `Controllers/HomeController.cs` 
demonstrates how to use that token to fetch a list of weather data that are accessible by a user and then provides a way to launch the apps in `Views/Home/Weather.cshtml`.

# How to Setup Identity Server
### 1. IdsConfig Project
1. Create Empty project of ASP.NET CORE(.NET 5.0) using any name in code name is `IdsConfig`
2. For Identity ServerSetup we need Nuget Packages as Below
    - IdentityServer4 //To Configure Identity Server
    - IdentityServer4.AspNetIdentity //user for client authentication
    - IdentityServer4.EntityFramework //to configure identity server with sql server
    - Microsoft.AspNetCore.Identity.EntityFrameworkCore //use for Asp User login
    - Microsoft.EntityFrameworkCore.SqlServer //use to connect sql server with identity server
    - Microsoft.EntityFrameworkCore.Tools //use for data manupulation
3. Add Quickstart UI of Indetity server which you can get from https://github.com/IdentityServer/IdentityServer4.Quickstart.UI, To configure Quickstart UI in project 
    ### Startup.cs
    - add service in `ConfigureServices` 
        ```csharp
        services.AddControllersWithViews();
        ```
    - add Configuration in Configure method to use the file.
        ```csharp
        app.UseStaticFiles();
        ```
    - set EndPoints for controllers
        ```csharp
        app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        ```

4. After installing all the nuget packages now we go for `Startup.cs` file and configure identity server using `services.AddIdentityServer()` in ConfigureService(), also we configure DbContext for ConfigurationStore and OperationalStore.
    ### Startup.cs
    ```csharp
    var connectionString = Configuration.GetConnectionString("DefaultConnection");
    var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
    services.AddIdentityServer()
        .AddAspNetIdentity<IdentityUser>() // this adds asp user identity login
        // this adds the operational data to DB (codes, tokens, consents)
        .AddConfigurationStore(con =>
        {
            con.ConfigureDbContext = configDb => configDb.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
        })
        // this adds the operational data to DB (codes, tokens, consents)
        .AddOperationalStore(os =>
        {
            os.ConfigureDbContext = configDb => configDb.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));                    
        })
        .AddDeveloperSigningCredential();
    ```
    notice the addition of the new call to AddAspNetIdentity<IdentityUser>. AddAspNetIdentity adds the integration layer to allow IdentityServer to access the user data for the ASP.NET Core Identity user database. This is needed when IdentityServer must add claims for the users into tokens.
5. Now add Migration for DbContext that we have config in the above step, before that please check the Connection string in `appsettings.json` and change as per your Connection string, 
    - now run the command as below in Package Manage Console
        ```
            PM> add-migration InitialIdentityServerMigartion -c PersistedGrantDbContext // For Operation store of Identity Server
            PM> add-migration InitialIdentityServerMigartion -c ConfigurationDbContext // For Configuration store of Identity server
        ```
    - now run the update-database command so all tables are created in the database related to the Identity server
        ```
            PM> update-database -Context PersistedGrantDbContext
            PM> update-database -Context ConfigurationDbContext
        ```

6. IdentityServer is designed for flexibility and part of that is allowing you to use any database you want for your users and their data (including passwords). If you are starting with a new user database

7. Add new custom Dbcontext which inherites `IdentityDbContext` which is use to manage login of application using database
    - In code you can get the file `IdsDbContext.cs` in `Data` directory
    - after adding file configure DbContext in services
        ### Startup.cs
        ```csharp
            services.AddDbContext<IdsDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));
        ```
    - add new migration using the command for `IdentityDbContext` and update the database which creates Asp user related tables
        ```
            PM> add-migration InitialIdentityServerMigartion -c IdsDbContext
            PM> update-database -Context IdsDbContext
        ```
    - configure custome DbContext with Identity methods which use two prameters `IdentityUser, IdentityRole` to manage use login
        ### Startup.cs
        ```csharp
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<IdsDbContext>();
        ```
        
    Note that AddIdentity<ApplicationUser, IdentityRole> must be invoked before AddIdentityServer.

8. Identity server requires data to do the operation so in code there are some hard-coded in-memory clients and resource definitions in `Config.cs` file those data seeded into database using InitializeDatabase() method which is called in `Configure` method in `Startup.cs` file
    ### Startup.cs
    ```csharp
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

                var testusr = userMgr.FindByNameAsync("Test").Result;
                if (testusr == null)
                {
                    testusr = new IdentityUser
                    {
                        UserName = "Test",
                        Email = "testuser@gmail.com",
                        EmailConfirmed = true
                    };
                    var result = userMgr.CreateAsync(testusr, "Potenza@123").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    result = userMgr.AddClaimsAsync(testusr, new Claim[]
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
    ```
9. Final step run the Project and check the database data is inserted in tables.

# Setup WebApi with Identity Server
### 2. IdsWebApi Project

1. Add New project of ASP.NET CORE(.NET 5.0) using any name in code name is `IdsWebApi`, it includes a default controller called `WeatherForecastController`.
2. to configure API using Identity server we need to install NuGet package `IdentityServer4.AccessTokenValidation` which provide a library to configure API using IdentityServerAuthentication. After installation of the NuGet package we need to add a service in `ConfigureServices` (IdsWebApi/Startup.cs)
    ### Startup.cs
    ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            //Add Identity Server Authentication to secure Api
            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication("Bearer", config => {
                    config.ApiName = "IdsWebApi";
                    config.Authority = "https://localhost:44318/"; //IdsConfig Project URL which you can fing in `appsettings.json` of IdsConfig
                });
            services.AddControllers();
        }
    ```
3. Still Api is not secure until we add Authentication in App and add [Authorize] role on the API Controller please check this
    - Add Authentication in App
        ### Startup.cs
        ```csharp
            app.UseAuthentication();
        ```
    - Add Authorize role
        ### WeatherForecastController.cs
        ```csharp
            [ApiController]
            [Route("[controller]")]
            [Authorize]
            public class WeatherForecastController : ControllerBase
            {
                //Your Apis
            }
        ```
# Setup WebApp with access API with OpenId Connect using Identity server

### 3. IdsWebApp Project
1. add new project of of ASP.NET CORE MVC(.NET 5.0) using any name in code name is `IdsWebApp`, it's include default controllers and Views
2. API call in MVC for that we add new method `Weather` in HomeController.cs in that method we call API using [HttpClient] as
    ```csharp
    var result = client
                .GetAsync("https://localhost:44332/weatherforecast")
                .Result;
    ```
    add one class in the model to get a converted result from API
    ```csharp
    public class WeatherData
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        public string Summary { get; set; }
    }
    ```
    This API is secure with `Bearer-Authentication` so we need to provide Bearer token with API call so one service is configured in code call `TokenService`, if you don't want to add token service you can directly call `TokenService.cs` code in the controller.
    TokenService having a class that has a method for generating token and Interface which is used for injecting in HomeController.cs, you can find both class and interface file in `Services` Directory of `IdsWebApp`.
    Token Service class use IdentityServerSettings for generating token so we import IdentityServerSettings in the project using service
    for that class is created and IdentityServerSettings data is from the `appsettings.json` file of project `IdsWebApp`.

    ### appsettings.json
    ```json
        "IdentityServerSettings": {
            "ApiUrl": "https://localhost:44318/",
            "ClientName": "super.client",
            "ClientPassword": "SecretPassword",
            "UseHttps": true
        },
    ```
    ### Startup.cs
    ```csharp
    services.Configure<IdentityServerSettings>(Configuration.GetSection("IdentityServerSettings"));
    ```
    ### TokenService.cs
    ```csharp
    public async Task<TokenResponse> GetToken(string scope)
    {
        using var client = new HttpClient();
        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = "https://localhost:44318/connect/token", // you can find all meta data here https://localhost:44318/.well-known/openid-configuration

            ClientId = _identityServerSettings.Value.ClientName,
            ClientSecret = _identityServerSettings.Value.ClientPassword,
            Scope = scope
        });


        if (tokenResponse.IsError)
        {
            _logger.LogError($"Unable to get token. Error is: {tokenResponse.Error}");
            throw new Exception("Unable to get token", tokenResponse.Exception);
        }

        return tokenResponse;
    }
    ```

    so Finally we have code for calling API
    ```csharp
        using (var client = new HttpClient())
        {
            var tokenResponse = await _tokenService.GetToken("IdsWebApi.read");
            client
                .SetBearerToken(tokenResponse.AccessToken);

            var result = client
                .GetAsync("https://localhost:44332/weatherforecast")
                .Result;

            if (result.IsSuccessStatusCode)
            {
                var model = result.Content.ReadAsStringAsync().Result;

                data = JsonConvert.DeserializeObject<List<WeatherData>>(model);

                return View(data);
            }
            else
            {
                throw new Exception("Unable to get content");
            }

        }
    ```
3. run the project and visit the page https://localhost:44324/Home/Weather where you get a list of Weather data get using the API.
4. Web API is secure but our web application isn't, so now we add nuget Packages as below To configure OpenIdConnect
    - IdentityModel
    - Microsoft.AspNetCore.Authentication.OpenIdConnect

5. You will find the majority of the important code in [Startup.cs] which is where the OpenId Connect Provider is configured. Add Authentication service with Scheme with `CookieAuthenticationDefaults.AuthenticationScheme` and ChallengeScheme with `OpenIdConnectDefaults.AuthenticationScheme`, add AddCookie and AddOpenIdConnect with service.
    ```csharp
        services.AddAuthentication(config =>
        {
            config.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            config.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,options =>
            {
                options.Authority = "https://localhost:44318";
                options.ClientId = Configuration["oidc:ClientName"];
                options.ClientSecret = Configuration["oidc:ClientPassword"];
                options.ResponseType = "code";
                options.UsePkce = true;
                options.ResponseMode = "query";
                options.Scope.Add("IdsWebApi.read");
                options.SaveTokens = true;
            });
    ```
    after adding this add authentication to the application and add [Authorize] role on the `weather` method of `HomeController.cs`.
6. Build `IdsWebApp` and right-click on solution select multiple projects in startup project, and run the project now all 3 projects are run open https://localhost:44324/Home/Weather here you will redirect to the Login page of identity server now login with a user that you have imported added in hard-code or you can use as per this code
    UserName: Potenza
    Password: Potenza@123

    after login, you will redirect to the listing page with data that is got from API.

# How to setup project in your local

### if you want to run the project without any default data changes 

- Before running the project, please look at the Connection string in `appsettings.json` of IdsConfig Project and change the Server Name, Database Name, User and Password as you have
- Now right-click on Solution -> select properties -> In Common Properties - Startup Project -> select Multiple Startup Projects -> and set options start of all projects listed


### if you want to do changes in data
Before running the project
- please look at the Connection string in `appsettings.json` of IdsConfig Project and change the Server Name, Database Name, User and Password as you have
- Now you have to check default client data that you can find in the `Config.cs` file of IdsConfig Project
- Same for the users that you can find in the `Startup.cs` file of IdsConfig Project
- you can change both as per your choice
    - Note: there was a condition if you change the Client data you need to change the `appsettings.json` file of IdsWebApp Project, In this file, you can see the client data exist those are used for API call and User Authorization, so if you change default client data you need to change same in [appsettings.json]
- Default Data which will Seed in Your database from `Config.cs` file of IdsConfig Project
    -  ```csharp
        public class Config
        {
            public static IEnumerable<IdentityResource> IdentityResources =>
            new[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource
                {
                    Name = "role",
                    UserClaims = new List<string> {"role"}
                }
            };

            public static IEnumerable<ApiScope> ApiScopes =>
            new[]
            {
                new ApiScope("IdsWebApi.read"),
                new ApiScope("IdsWebApi.write"),
            };

            public static IEnumerable<ApiResource> ApiResources => new[]
            {
                new ApiResource("IdsWebApi")
                {
                    Scopes = new List<string> { "IdsWebApi.read", "IdsWebApi.write"},
                    ApiSecrets = new List<Secret> {new Secret("ScopeSecret".Sha256())},
                    UserClaims = new List<string> {"role"}
                }
            };

            public static IEnumerable<Client> Clients =>
            new[]
            {
                // machine to machine client
                new Client
                {
                ClientId = "super.client",
                ClientName = "Super Client",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = {new Secret("SecretPassword".Sha256())},

                AllowedScopes = { "IdsWebApi.read", "IdsWebApi.write" }
                },

                // interactive ASP.NET Core MVC client
                new Client
                {
                ClientId = "interactive",
                ClientSecrets = {new Secret("SecretPassword".Sha256())},

                AllowedGrantTypes = GrantTypes.Code,

                // where to redirect to after login
                RedirectUris = {"https://localhost:44324/signin-oidc"},
                
                // where to redirect to after logout
                PostLogoutRedirectUris = {"https://localhost:44324/signout-callback-oidc"},

                AllowOfflineAccess = true,
                AllowedScopes = {"openid", "profile", "IdsWebApi.read"},
                RequirePkce = true,
                RequireConsent = true,
                AllowPlainTextPkce = false
                },
            };
        }
        ```