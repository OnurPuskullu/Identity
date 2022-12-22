using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Abstractions;
using System.Security.Claims;
using Udemy.Identity.Entities;
using Udemy.Identity.Models;

namespace Udemy.Identity.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class HomeController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        public HomeController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult Index()
        {
            return View(new UserCreateModel());
        }
        public IActionResult Create()
        {
            return View(new UserCreateModel());  
        }
        [HttpPost]
        public async Task<IActionResult> Create(UserCreateModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new()
                {
                    Email = model.Email,
                    Gender = model.Gender,
                    UserName = model.Username
                };        
                var identityResult = await _userManager.CreateAsync(user, model.Password);
                if (identityResult.Succeeded)
                {
                    var memberRole = await _roleManager.FindByNameAsync("Member");
                    if (memberRole==null)
                    {
                        await _roleManager.CreateAsync(new()
                        {
                            Name = "Member",
                            CreatedTime = DateTime.Now,
                        });
                    }
                    await _userManager.AddToRoleAsync(user, "Member");
                    return RedirectToAction("Index");
                }
                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }
        public IActionResult SignIn(string returnUrl)
        {
            return View(new UserSignInModel { ReturnUrl = returnUrl });
        }
        [HttpPost]
        public async Task<IActionResult> SignIn(UserSignInModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                var signInResult = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, true);
                if (signInResult.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    var roles=await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("AdminPanel");
                    }
                    else
                    {
                        return RedirectToAction("Panel");
                    }
                    //bu iş başarılı
                }
                else if(signInResult.IsLockedOut)
                {
                    var lockOutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    ModelState.AddModelError("", $"Hesabınız {(lockOutEnd.Value.UtcDateTime-DateTime.UtcNow).Minutes} dk askıya alınmıştır");
                }
                else
                {
                    var message = string.Empty;

                    if (user != null)
                    {
                        var failedCount = await _userManager.GetAccessFailedCountAsync(user);
                        message = $"{(_userManager.Options.Lockout.MaxFailedAccessAttempts - failedCount)} kez daha girerseniz hesabınız geçici olarak kilitlenecektir";
                    }
                    else
                    {
                        message = "Kullanıcı adı veya şifre hatalı";
                    }
                    ModelState.AddModelError("", message);
                }    
            }
            return View(model); 
        }
        [Authorize]
        public IActionResult GetUserInfo()
        {
            var userName = User.Identity.Name;
            var role = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;
            User.IsInRole("Member");
            return View();
        }

        [Authorize(Roles ="Admin")]
        public IActionResult AdminPanel()
        {
            return View();
        }
        [Authorize(Roles ="Member")]
        public IActionResult Panel()
        {
            return View();
        }
        [Authorize(Roles ="Member")]
        public IActionResult MemberPage()
        {
            return View();
        }
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }
    }
}
