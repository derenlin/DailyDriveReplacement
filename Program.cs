using Microsoft.Extensions.Configuration;
using SpotifyDailyDrive;

// ── Configuration ─────────────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var spotifyConfig = configuration
    .GetSection(SpotifyConfig.SectionName)
    .Get<SpotifyConfig>()
    ?? throw new InvalidOperationException("Could not bind Spotify configuration.");

spotifyConfig.Validate();

var dailyDriveConfiguration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("dailydrive-config.json", optional: false, reloadOnChange: false)
    .Build();

var dailyDriveConfig = dailyDriveConfiguration
    .GetSection(DailyDriveConfig.SectionName)
    .Get<DailyDriveConfig>()
    ?? throw new InvalidOperationException("Could not bind Daily Drive configuration.");

dailyDriveConfig.Validate();

// ── Authentication ─────────────────────────────────────────────────────────────
Console.WriteLine("=== Spotify Daily Drive ===\n");

SpotifyAuthService authService = new(spotifyConfig);
var spotifyClient = await authService.AuthenticateAsync();

// ── Playlist Updates ──────────────────────────────────────────────────────────
PlaylistService playlistService = new(spotifyClient, spotifyConfig, dailyDriveConfig);

await playlistService.UpdateDailyDriveAsync();
Console.WriteLine("\nDone! Your Daily Drive playlist is updated.");
