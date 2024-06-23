using AspNet.Security.OAuth.Twitch;
using Fumo.Application.Web.Service;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Models;
using Fumo.Shared.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[Route("[controller]"), ApiController, Authorize]
public class AccountController : ControllerBase
{
    private readonly HttpUserService HttpUserService;
    private readonly string BotId;

    public AccountController(HttpUserService httpUserService, AppSettings settings)
    {
        HttpUserService = httpUserService;
        BotId = settings.Twitch.UserID;
    }

    [HttpGet("Login"), AllowAnonymous]
    public IActionResult Login(string returnUrl = "/")
        => Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, TwitchAuthenticationDefaults.AuthenticationScheme);

    [HttpGet("Logout")]
    public IActionResult Logout()
        => SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);

    [HttpGet("__BotLogin")]
    public async ValueTask<IActionResult> BotLogin(CancellationToken ct)
    {
        var user = await HttpUserService.GetUser(ct);

        if (user.TwitchID != BotId)
            return Unauthorized();

        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OAuthProviderName.TwitchBot);
    }

    [HttpGet("Me")]
    public async ValueTask<IActionResult> Me()
    {
        var user = await HttpUserService.GetUser();

        return Ok(new
        {
            id = user.TwitchID,
            name = user.TwitchName,
        });
    }

    [HttpGet("Join")]
    public async ValueTask<IActionResult> Join(IEventsubManager eventsubManager, CancellationToken ct)
    {
        var user = await HttpUserService.GetUser(ct);

        if (!await eventsubManager.IsUserEligible(user.TwitchID, EventsubType.ChannelChatMessage, ct))
            return RedirectToAction(nameof(Login));

        if (await eventsubManager.CheckSubscribeCooldown(user.TwitchID, EventsubType.ChannelChatMessage))
            return Redirect("/error?code=cooldown");

        if (await eventsubManager.IsSubscribed(EventsubType.ChannelChatMessage, user.TwitchID, ct) is true)
            return Redirect("/error?code=already_joined");

        var request = new EventsubSubscriptionRequest<EventsubBasicCondition>(
            user.TwitchID,
            EventsubType.ChannelChatMessage,
            new(user.TwitchID, BotId)
        );

        await eventsubManager.Subscribe(request, ct);

        return Redirect("/");
    }
}
