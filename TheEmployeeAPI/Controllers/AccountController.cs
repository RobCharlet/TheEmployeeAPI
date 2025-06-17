using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TheEmployeeAPI.Users;


namespace TheEmployeeAPI.Controllers;

public class AccountController : Controller {
  private readonly UserManager<User> _userManager;
  private readonly SignInManager<User> _signInManager;
  private readonly ILogger<AccountController> _logger;

  public AccountController (
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<AccountController> logger
  ) {
    _userManager = userManager;
    _signInManager = signInManager;
    _logger = logger;
  }

  [HttpGet]
  public IActionResult Login(string? returnUrl = null)
  {
    if (User.Identity?.IsAuthenticated == true)
    {
      return RedirectToAction("Index", "Employees");
    }

    ViewData["ReturnUrl"] = returnUrl;
    return View(new LoginRequest());
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
  {
    ViewData["ReturnUrl"] = returnUrl;

    if (!ModelState.IsValid) {
      return View(model);
    }

    var user = await _userManager.FindByEmailAsync(model.Email!);
    if (user == null)
    {
      TempData["Error"] = "Email ou mot de passe invalide.";
      return View(model);
    }

    if (!user.IsActive)
    {
      TempData["Error"] = "Compte désactivé. Contactez le support.";
      return View(model);
    }

    var result = await _signInManager.PasswordSignInAsync(
      model.Email!,
      model.Password!,
      model.RememberMe,
      lockoutOnFailure: true
    );

    if (result.Succeeded)
    {
      user.LastLoginDate = DateTime.UtcNow;
      await _userManager.UpdateAsync(user);

      TempData["Success"] = $"Bienvenue {user.DisplayName}";

      if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
      {
        return Redirect(returnUrl);
      }

      return RedirectToAction("Index", "Employees");
    }

    if (result.IsLockedOut)
    {
      TempData["Error"] = "Compte verrouillé. Réessayez plus tard.";
      return View(model);
    }

    TempData["Error"] = "Email ou mot de passe invalide.";
    return View(model);
  }

  [HttpPost]
  [Authorize]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Logout()
  {
    await _signInManager.SignOutAsync();
    TempData["Success"] = "Vous avez été déconnecté avec succès.";
    return RedirectToAction("Login");
  }

  [HttpGet]
  public IActionResult AccessDenied()
  {
    return View();
  }
}