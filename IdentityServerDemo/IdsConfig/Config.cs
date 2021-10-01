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
        public static List<TestUser> Users
        {
            get
            {
                var address = new
                {
                    street_address = "Hacker Street",
                    locality = "Indian",
                    postal_code = 395002,
                    country = "India"
                };

                return new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = "1",
                        Username = "Potenza",
                        Password = "Potenza@123",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Potenza User"),
                            new Claim(JwtClaimTypes.GivenName, "Potenza"),
                            new Claim(JwtClaimTypes.FamilyName, "Potenza"),
                            new Claim(JwtClaimTypes.Email, "info@potenzaglobalsolutions.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.Role, "admin"),
                            new Claim(JwtClaimTypes.WebSite, "https://potenzagloblsolutions.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    },
                    new TestUser
                    {
                        SubjectId = "2",
                        Username = "Test",
                        Password = "123456",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Test User"),
                            new Claim(JwtClaimTypes.GivenName, "User"),
                            new Claim(JwtClaimTypes.FamilyName, "Test"),
                            new Claim(JwtClaimTypes.Email, "testuser@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.Role, "user"),
                            new Claim(JwtClaimTypes.WebSite, "http://potenzagloblsolutions.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    }
                };
            }
        }

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
            // m2m client credentials flow client
            new Client
            {
              ClientId = "super.client",
              ClientName = "Super Client",

              AllowedGrantTypes = GrantTypes.ClientCredentials,
              ClientSecrets = {new Secret("SecretPassword".Sha256())},

              AllowedScopes = { "IdsWebApi.read", "IdsWebApi.write" }
            },

            // interactive client using code flow + pkce
            new Client
            {
              ClientId = "interactive",
              ClientSecrets = {new Secret("SecretPassword".Sha256())},

              AllowedGrantTypes = GrantTypes.Code,

              RedirectUris = {"https://localhost:44324/signin-oidc"},
              FrontChannelLogoutUri = "https://localhost:44324/signout-oidc",
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
