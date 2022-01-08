using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Identity.Client;
using Microsoft.Net.Http.Headers;
using SampleApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Services
{
    public class AppAuthenticate : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            HttpContext httpContext = context.HttpContext;

            if(context.Exception.GetType() == typeof(MsalException))
            {
                context.Result = new ChallengeResult(AzureADDefaults.AuthenticationScheme, new AuthenticationProperties());
            }
        }
    }
}