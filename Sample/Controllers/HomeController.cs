using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Sample.Models;
using Sample.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        readonly GraphServiceClient _msgraphClient;
        readonly IHttpContextAccessor _httpContext;


        public HomeController(GraphServiceClient msgraphClient, IHttpContextAccessor httpContext)
        {
            _msgraphClient = msgraphClient;
            _httpContext = httpContext;
        }

        public async Task<IActionResult> Index()
        {

            var me = await _msgraphClient.Me.Request().GetAsync();
            var displayName = me.DisplayName;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
