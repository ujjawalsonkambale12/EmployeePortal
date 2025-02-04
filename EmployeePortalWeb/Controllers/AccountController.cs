
using EmployeePortalWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmployeePortalWeb.Models.LoginViewModel;
using Microsoft.AspNetCore.Authorization;
namespace EmployeePortalWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Login GET
        public IActionResult Login()
        {
            return View();
        }

        // Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Index", "Employee");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Employee"))
                        {
                            return RedirectToAction("Details", "Employee", new { id = user.Id });
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "User does not have a valid role.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                }
            }
            return View(model);
        }



        // Register GET
        public IActionResult Register()
        {
            return View();
        }

        // Register POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (true)
            {
                // Create a new ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    FullName = model.FirstName + " " + model.LastName,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                string isAdmnin = string.Empty;

                if (result.Succeeded)
                {
                    if (user.UserName == "Admin")
                        isAdmnin = "Admin";
                    else
                        isAdmnin = "Employee";

                    var roleResult = await _userManager.AddToRoleAsync(user, isAdmnin);
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            var userId = _userManager.GetUserId(User);

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["EmployeeId"] = userId; 

            return View();
        }

    }
}
