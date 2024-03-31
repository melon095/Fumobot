using System.Security.Claims;
using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? requestUri)
    {
        string redirect = requestUri switch
        {
            string uri when !string.IsNullOrWhiteSpace(uri) && Url.IsLocalUrl(uri) => uri,
            _ => "/",
        };

        return Challenge(new AuthenticationProperties { RedirectUri = redirect }, TwitchAuthenticationDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("logout")]
    public IActionResult Logout() => SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);

    [Authorize]
    [HttpGet("user")]
    public IActionResult GetUser()
    {
        var name = User.FindFirst(TwitchAuthenticationConstants.Claims.DisplayName)?.Value;
        var picture = User.FindFirst(TwitchAuthenticationConstants.Claims.ProfileImageUrl)?.Value;

        return Ok(new { name, picture });
    }
}
