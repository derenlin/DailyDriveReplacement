using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyDailyDrive;

public class SpotifyAuthService
{
    private readonly SpotifyConfig _config;
    private EmbedIOAuthServer? _server;

    public SpotifyAuthService(SpotifyConfig config)
    {
        _config = config;
    }

    public async Task<SpotifyClient> AuthenticateAsync()
    {
        var callbackUri = new Uri(_config.RedirectUri);
        var port = callbackUri.Port;

        var tcs = new TaskCompletionSource<SpotifyClient>();

        _server = new EmbedIOAuthServer(callbackUri, port);
        await _server.Start();

        _server.AuthorizationCodeReceived += async (_, response) =>
        {
            await _server.Stop();

            var tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    _config.ClientId,
                    _config.ClientSecret,
                    response.Code,
                    callbackUri));

            var client = new SpotifyClient(tokenResponse.AccessToken);
            tcs.SetResult(client);
        };

        _server.ErrorReceived += (_, error, _) =>
        {
            tcs.SetException(new Exception($"Spotify auth error: {error}"));
            return Task.CompletedTask;
        };

        var loginRequest = new LoginRequest(callbackUri, _config.ClientId, LoginRequest.ResponseType.Code)
        {
            Scope =
            [
                Scopes.PlaylistModifyPublic,
                Scopes.PlaylistModifyPrivate,
                Scopes.UserReadPrivate,
                Scopes.UserTopRead
            ]
        };

        var authUrl = loginRequest.ToUri();
        Console.WriteLine($"Opening browser for Spotify login...");
        Console.WriteLine($"If the browser does not open automatically, go to:\n  {authUrl}\n");
        BrowserUtil.Open(authUrl);

        return await tcs.Task;
    }
}
