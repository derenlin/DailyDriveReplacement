namespace SpotifyDailyDrive;

public readonly struct Podcast
{
    required public string Id { init; get; }
    public string? Position { get; }
}

public class TopTracks
{
    public bool Enabled { get; set; }
    public string TimeRange { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class Music
{
    public int TotalSongs { get; set; }
    public List<string> Genres { get; set; } = [];
    public bool Shuffle { get; set; }
    public TopTracks TopTracks { get; set; } = new TopTracks();
}

public class DailyDriveConfig
{
    public const string SectionName = "DailyDrive";

    public string PlaylistId { get; set; } = string.Empty;
    public List<Podcast> Podcasts { get; set; } = [];
    public Music MusicOptions { get; set; } = new Music();


    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PlaylistId))
        {
            throw new InvalidOperationException("A Spotify:PlaylistId is not configured in dailydrive-config.json");
        }

        if (Podcasts.Count < 1)
        {
            throw new InvalidOperationException("At least one podcast is required in dailydrive-config.json");
        }
    }
}

public class SpotifyConfig
{
    public const string SectionName = "Spotify";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5000/callback";
    public string PlaylistName { get; set; } = "Daily Drive";
    public string PlaylistDescription { get; set; } = string.Empty;


    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Spotify:ClientId is not configured in appsettings.json.");

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("Spotify:ClientSecret is not configured in appsettings.json.");
    }
}
