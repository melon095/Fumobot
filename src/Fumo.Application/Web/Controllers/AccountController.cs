using AspNet.Security.OAuth.Twitch;
using Fumo.Application.Web.Service;
using Fumo.Shared.Eventsub;
using Fumo.Shared.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly HttpUserService HttpUserService;

    public AccountController(HttpUserService httpUserService)
    {
        HttpUserService = httpUserService;
    }

    [HttpGet("Login")]
    public IActionResult Login() => Challenge(new AuthenticationProperties { RedirectUri = "/" }, TwitchAuthenticationDefaults.AuthenticationScheme);

    [HttpGet("Logout"), Authorize]
    public IActionResult Logout() => SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);

    [HttpGet("Me")]
    public async ValueTask<IActionResult> Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized();

        var user = await HttpUserService.GetUser();

        return Ok(new
        {
            id = user.TwitchID,
            name = user.TwitchName,
        });
    }

    [HttpGet("Join"), Authorize]
    public async ValueTask<IActionResult> Join(IEventsubManager eventsubManager, CancellationToken ct)
    {
        var user = await HttpUserService.GetUser(ct);

        if (!await eventsubManager.IsUserEligible(user.TwitchID, EventsubType.ChannelChatMessage, ct))
            return RedirectToActionPermanent("Login");

        if (await eventsubManager.CheckSubscribeCooldown(user.TwitchID, EventsubType.ChannelChatMessage))
            return Redirect("/error?code=cooldown");

        await eventsubManager.Subscribe(user.TwitchID, EventsubType.ChannelChatMessage, ct);

        return Redirect("/");
    }
}
