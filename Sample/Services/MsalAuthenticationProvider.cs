using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleApp.Services
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        readonly IMsalService _msalService;
        readonly IHttpContextAccessor _httpContext;
        readonly string _instance;

        public MsalAuthenticationProvider(IMsalService msalService, IHttpContextAccessor httpContext, string instance)
        {
            _msalService = msalService;
            _httpContext = httpContext;
            _instance = instance;
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            string AccessToken = String.Empty;

            //AccessToken = await _msalService.GetToken($"{_instance}/.default").ConfigureAwait(false);

            
            try
            {
                AccessToken = await _msalService.GetToken($"{_instance}/.default").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // If user interaction is requred or token cache does not contain user was previsouly signed-in
                await _httpContext.HttpContext.ChallengeAsync(AzureADDefaults.OpenIdScheme, new AuthenticationProperties());
            }
            

            request.Headers.Add("Authorization", AccessToken);
        }
    }
}
