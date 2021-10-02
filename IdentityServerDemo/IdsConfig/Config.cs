using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdsConfig
{
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
}
