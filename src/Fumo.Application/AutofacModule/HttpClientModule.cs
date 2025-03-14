using Fumo.Shared.Models;

namespace Fumo.Application.AutofacModule;

public static class HttpClientModule
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0";

    private static void Default(HttpClient o)
    {
        o.Timeout = DefaultTimeout;
        o.DefaultRequestHeaders.UserAgent.ParseAdd(DefaultUserAgent);
        o.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    }

    public static void AddHttpClients(this IServiceCollection builder, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SevenTV.Bearer))
            throw new ArgumentException("Missing SevenTV Bearer Token from Configuration");

        var userAgent = settings.UserAgent ?? DefaultUserAgent;

        builder.AddHttpClient("SevenTV", (o) =>
        {
            o.BaseAddress = new Uri("https://7tv.io/v4/gql");
            o.DefaultRequestHeaders.Authorization = new("Bearer", settings.SevenTV.Bearer);

            Default(o);
        });

        builder.AddHttpClient("TLAPI", (o) =>
        {
            o.BaseAddress = new Uri("https://gql.twitch.tv/gql/");
            o.DefaultRequestHeaders.Add("Client-ID", settings.Twitch.ThreeLetterAPI);

            Default(o);
        });

        builder.AddHttpClient("Pajbot", Default);

        builder.AddHttpClient("HelixFactory", Default);
    }
}
