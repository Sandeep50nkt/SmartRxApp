using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmartRx.Domain;
using SmartRx.Web.Helpers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _http;
    public AccountController(IHttpClientFactory http) { _http = http; }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
public async Task<IActionResult> Login(LoginRequest req)
{
    var res = await _http.CreateClient("auth").PostAsJsonAsync("http://localhost:5001/api/auth/login", req);
    if (!res.IsSuccessStatusCode) { ViewBag.Error="Invalid credentials"; return View(req); }

    var login = await res.Content.ReadFromJsonAsync<LoginResponse>();
    HttpContext.Session.SetString(SessionKeys.Jwt, login!.Token);
    HttpContext.Session.SetString(SessionKeys.Role, login.Role);
    HttpContext.Session.SetString(SessionKeys.User, login.Username);

    var claims = new List<Claim> {
        new Claim(ClaimTypes.Name, login.Username),
        new Claim(ClaimTypes.Role, login.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return RedirectToAction("Index", "Drugs");
}

public async Task<IActionResult> Logout()
{
    HttpContext.Session.Clear();
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Login");
}
}
