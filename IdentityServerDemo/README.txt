This solution have 3 projects based on ASP.NET CORE(.NET 5.0)
1. Identity Server Configuration
2. WebApi Project Protexted with Identity Server
3. Web Application Authenticate By Open id connect

for user authentication.

* Authorization Code flow - This is the recommended approach to OpenId Connect authentication. It will redirect the user to a Identity Server login page before returning to your app. See `Startup.cs` for configuring this approach.
* Open Id connect authentication is authenticate based on identity server, In identity server we have defined clients with client Id and client secret password see IdentityServerDemo - Config.cs

This app also includes an example of obtaining an OAuth2 `access_token` for use in accessing the APIs The `Dashboard` route in the `Controllers/HomeController.cs` 
demonstrates how to use that token to fetch a list of apps that are accessible by a user and then provides a way to launch the apps in `Views/Home/Dashboard.cshtml`.

1. IdsConfig Projects

##Used Niget Packages as below

-> IdentityServer4 //To Configure Identity Server
-> IdentityServer4.EntityFramework
-> Microsoft.EntityFrameworkCore.SqlServer //use to connect sql server with identity server
-> Microsoft.EntityFrameworkCore.Tools
	
	
## Add Migartion using Package Manage Console
-> Provide your connection string in appsettings.Json

add-migration InitialIdentityServerMigartion -c PersistedGrantDbContext // For Operation store of Identity Server
add-migration InitialIdentityServerMigartion -c ConfigurationDbContext // For Configuration store of Identity server
add-migration InitialIdentityServerMigartion -c IdsDbContext //Manage login user from database
update-database -Context PersistedGrantDbContext
update-database -Context ConfigurationDbContext

2. IdsWebApi Project
##Used Niget Packages as below
-> IdentityServer4.AccessTokenValidation


3. 
##Used Niget Packages as below
-> IdentityModel
-> Microsoft.AspNetCore.Authentication.OpenIdConnect
