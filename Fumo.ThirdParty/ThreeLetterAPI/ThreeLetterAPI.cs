using Fumo.ThirdParty.Exceptions;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Fumo.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPI : IThreeLetterAPI, IDisposable
{
    public static readonly string HttpFactoryName = nameof(ThreeLetterAPI);

    public IConfiguration Config { get; }
    private HttpClient HttpClient { get; set; }

    private bool disposed = false;

    private string? Query = null;
    private string? Operation = null;
    private object? Variables = new() { };
    private Extension? Extension = null;

    public ThreeLetterAPI(IConfiguration config, HttpClient? httpClient = null)
    {
        this.Config = config;
        // FIXME: Use a custom IHttpClientFactory implementation
        this.HttpClient = httpClient ?? new HttpClient();

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

    public IThreeLetterAPI AddInstruction(IThreeLetterAPIInstruction instruction)
    {
        this.Query = instruction.Instruction;

        return this;
    }

    public IThreeLetterAPI AddInstruction(string instruction)
    {
        this.Query = instruction;

        return this;
    }

    public IThreeLetterAPI AddOperation(string operation, Extension extension)
    {
        this.Operation = operation;
        this.Extension = extension;

        return this;
    }

    public IThreeLetterAPI AddVariables(object variables)
    {
        this.Variables = variables;

        return this;
    }

    public async Task<TResponse> SendAsync<TResponse>(CancellationToken cancellationToken = default)
    {
        ThreeLetterAPIRequest body = new();

        if (this.Operation is not null)
        {
            body.OperationName = this.Operation;
            body.Extensions = this.Extension;
        }
        else if (this.Query is not null)
        {
            body.Query = this.Query;
        }
        else
        {
            throw new Exception($"Missing {nameof(this.Operation)} and {nameof(this.Query)}");
        }

        if (this.Variables is not null)
        {
            body.Variables = this.Variables;
        }

        var serialized = JsonSerializer.Serialize(body);

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
}
