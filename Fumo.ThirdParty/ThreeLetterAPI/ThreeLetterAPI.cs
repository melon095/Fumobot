using Fumo.ThirdParty.Exceptions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Fumo.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPI : IThreeLetterAPI, IDisposable
{
    public static readonly string HttpFactoryName = nameof(ThreeLetterAPI);

    public IConfiguration Config { get; }
    private HttpClient HttpClient { get; set; }

    private bool disposed = false;

    public ThreeLetterAPI(IConfiguration config)
    {
        this.Config = config;
        this.HttpClient = new HttpClient();

        this.HttpClient.DefaultRequestHeaders.Add("Client-ID", Config["Twitch:ThreeLetterAPI"]);
        this.HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");

        this.HttpClient.BaseAddress = new("https://gql.twitch.tv/gql");
    }

    public void Dispose()
    {
        if (disposed) return;

        GC.SuppressFinalize(this);
        this.HttpClient.Dispose();

        disposed = true;
    }

    public async Task<TResponse> SendAsync<TResponse>(IThreeLetterAPIInstruction instructions, CancellationToken cancellationToken = default)
    {
        var request = instructions.Create();

        var serialized = JsonSerializer.Serialize(request);

        var response = await this.HttpClient.PostAsync(string.Empty, new StringContent(serialized, Encoding.UTF8, "application/json"), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"{nameof(ThreeLetterAPI)} service down");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var json = JsonSerializer.Deserialize<RawThreeLetterResponse<TResponse>>(responseBody);

        if (json!.Data is null && json.Errors is not null)
        {
            var message = json.Errors[0].Message;

            throw new ThreeLetterAPIException(message);
        }

        return json.Data;
    }

    public async Task<List<TResponse>> PaginatedQueryAsync<TResponse>(Func<TResponse?, IThreeLetterAPIInstruction?> prepare, CancellationToken cancellationToken = default)
    {
        List<TResponse> responses = new();

        /*
            This could be fixed by passing in a seperate instruction as a first parameter, but this should work. 
        */
        var instruction = prepare(default!);

        ArgumentNullException.ThrowIfNull(instruction, nameof(instruction));

        do
        {
            // TODO: Add exponential backoff maybe

            var response = await this.SendAsync<TResponse>(instruction, cancellationToken);
            responses.Add(response);
            instruction = prepare(response);
        } while (instruction is not null);

        return responses;
    }
}
