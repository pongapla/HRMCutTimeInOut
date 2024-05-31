using Microsoft.AspNetCore.Mvc;
using HRMCutTimeInOut.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMCutTimeInOut.Controllers
{
    
    public class LoginController : Controller
    {
        private readonly ActiveDirectoryService _adService;

        public LoginController(ActiveDirectoryService adService)
        {
            _adService = adService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            bool isValid = _adService.IsUserValid(model.Username, model.Password);

            if (isValid)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.Username)
        };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    // Optionally set properties
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "User");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View("Index", model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

    }
}
