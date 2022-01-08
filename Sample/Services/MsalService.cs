using SampleApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Mvc;

namespace SampleApp.Services
{
    public interface IMsalService
    {
        Task<string> GetTokenUsingAuthorizationCode(string code);
        Task<string> GetToken(string scope);
    }


    public class MsalService : IMsalService
    {
        private IConfidentialClientApplication client;

        private IConfiguration Configuration { get; }
        private IHttpContextAccessor UserHttpContext { get; }


        string ClientId;
        string Authority;
        string RedirectUri;

        public MsalService(IConfiguration Configuration, IHttpContextAccessor UserHttpContext)
        {
            this.Configuration = Configuration;
            this.UserHttpContext = UserHttpContext;

            ClientId = Configuration["AzureAd:ClientId"];
            Authority = $"{Configuration["AzureAd:Instance"]}{Configuration["AzureAd:TenantId"]}";

            var CallbackPath = Configuration["AzureAd:CallbackPath"];

            RedirectUri = $"{UserHttpContext.HttpContext.Request.Scheme}://{UserHttpContext.HttpContext.Request.Host}{CallbackPath}";

            client = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithAuthority(Authority)
                .WithRedirectUri(RedirectUri)
                .WithClientSecret(Configuration["AzureAd:ClientSecret"])
                .Build();
        }

        public async Task<string> GetTokenUsingAuthorizationCode(string code)
        {
            string[] scopes = { "User.Read" };
            var result = await client.AcquireTokenByAuthorizationCode(scopes, code).ExecuteAsync().ConfigureAwait(false);
            return result.AccessToken;
        }

        public async Task<string> GetToken(string scope)
        {
            var scopes = scope.Split(' ');

            var _claimsPrincipal = UserHttpContext.HttpContext.User;

            var account = await GetAccountByUser(_claimsPrincipal);
            var result = await client.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);

            return result.AccessToken;
        }


        public async Task<IAccount> GetAccountByUser(ClaimsPrincipal claimsPrincipal)
        {
            var account = await client.GetAccountAsync(GetMsalAccountId(claimsPrincipal));

            //This is obsolete
            //var account = accounts.Where(a => a.Username == username).FirstOrDefault();

            return account;
        }


        public string GetMsalAccountId(ClaimsPrincipal claimsPrincipal)
        {
            string userObjectId = GetObjectId(claimsPrincipal);
            string nameIdentifierId = GetNameIdentifierId(claimsPrincipal);
            string tenantId = GetTenantId(claimsPrincipal);
            string userFlowId = GetUserFlowId(claimsPrincipal);

            if (!string.IsNullOrWhiteSpace(nameIdentifierId) &&
                !string.IsNullOrWhiteSpace(tenantId) &&
                !string.IsNullOrWhiteSpace(userFlowId))
            {
                // B2C pattern: {oid}-{userFlow}.{tid}
                return $"{nameIdentifierId}.{tenantId}";
            }
            else if (!string.IsNullOrWhiteSpace(userObjectId) && !string.IsNullOrWhiteSpace(tenantId))
            {
                // AAD pattern: {oid}.{tid}
                return $"{userObjectId}.{tenantId}";
            }

            return null;
        }


        public string GetUsername(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.FindFirst(CustomClaimTypes.UserPrincipalName) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.UserPrincipalName).Value;
            if (claimsPrincipal.FindFirst(CustomClaimTypes.PreferredUserName) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.PreferredUserName).Value;

            return null;
        }


        public string GetObjectId( ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.FindFirst(CustomClaimTypes.ObjectId) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.ObjectId).Value;

            return null;
        }


        public string GetTenantId( ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.FindFirst(CustomClaimTypes.Tid)!=null) return claimsPrincipal.FindFirst(CustomClaimTypes.Tid).Value;
            if (claimsPrincipal.FindFirst(CustomClaimTypes.TenantId) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.TenantId).Value;

            return null;
        }


        public static string GetNameIdentifierId(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.FindFirst(CustomClaimTypes.UniqueObjectIdentifier) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.UniqueObjectIdentifier).Value;

            return null; 
        }


        public static string GetUserFlowId(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.FindFirst(CustomClaimTypes.Tfp) != null) return claimsPrincipal.FindFirst(CustomClaimTypes.Tfp).Value;

            return null;
        }
    }
}
