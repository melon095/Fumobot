using Autofac;
using Fumo.Database;
using Fumo.Interfaces;
using Fumo.ThirdParty.ThreeLetterAPI;
using Fumo.ThirdParty.ThreeLetterAPI.Response;
using System.Text.RegularExpressions;

namespace Fumo.Models;

public class UserRepository : IUserRepository
{
    private static readonly Regex UsernameCleanRegex = new("[@#]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public IComponentContext ComponentContext { get; }

    public UserRepository(IComponentContext componentContext)
    {
        ComponentContext = componentContext;
    }

    /// <inheritdoc/>
    public Task<UserDTO> SearchIDAsync(string id, CancellationToken? cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<UserDTO> SearchNameAsync(string username, CancellationToken? cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static string CleanUsername(string username) => UsernameCleanRegex.Replace(username.ToLower(), "");
}
