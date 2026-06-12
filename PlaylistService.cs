using SpotifyAPI.Web;
using System.Text.Json;

namespace SpotifyDailyDrive;

public interface IMixedTrackList
{
    string Uri { get; set; }
}

public struct State()
{
    public string PreviousSongList { get; } = string.Empty;
    public string PreviousEpisodesList { get; } = string.Empty;
    public DateTime? LastUpdated { get; }
}

public struct Episode() : IMixedTrackList
{
    required public string Uri { get; set; }
    required public string Name { get; set; }
    required public string Show { get; set; }
    public string Type { get; set; } = "episode";
    public string? Position { get; set; }
}

public struct MusicTrack() : IMixedTrackList
{
    required public string Uri { get; set; }
    public string? Position { get; set; }
}

/// <summary>
/// Creates or updates the Daily Drive playlist for the authenticated user.
/// </summary>
public class PlaylistService(SpotifyClient client, SpotifyConfig config, DailyDriveConfig dailyDriveConfig)
{
    private readonly SpotifyClient _client = client;
    private readonly SpotifyConfig _config = config;

    private readonly DailyDriveConfig _dailyDriveConfig = dailyDriveConfig;

    public static async Task SaveStateToFile(State state)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true, // Required for public fields like 'Age'
            WriteIndented = true  // Optional: makes the output pretty
        };

        string stateJsonString = JsonSerializer.Serialize(state, options);
        File.WriteAllText("/state.json", stateJsonString);
    }

    public static async Task<State?> LoadPreviousState()
    {
        if (File.Exists("/state.json"))
        {
            try
            {
                string saveStateString = await File.ReadAllTextAsync("state.json");
                return JsonSerializer.Deserialize<State>(saveStateString);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: The file was not found. Details: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Permission Error: Cannot access file. Details: {ex.Message}");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O Error: File is locked or corrupted. Details: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Unexpected error: {ex.Message}");
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public async Task<List<Episode>> GetPodcastEpisodes(List<Podcast> podcasts)
    {
        List<Episode> episodes = [];

        foreach (Podcast podcast in podcasts)
        {
            try
            {
                var show = await _client.Shows.Get(podcast.Id);
                var episode = show?.Episodes?.Items?[0];

                if (show is not null && episode is not null)
                {
                    episodes.Add(new Episode
                    {
                        Uri = episode.Uri,
                        Name = episode.Name,
                        Show = show.Name,
                        Type = "episode",
                        Position = podcast.Position // "first" = pinned to top of playlist
                    });
                }
            }
            catch (APIException ex)
            {
                Console.Error.WriteLine($"Spotify API error: {ex.Response} {ex.Message}");
            }
        }

        return episodes;
    }

    public async Task<List<MusicTrack>> fetchTopMusicTracks()
    {
        List<MusicTrack> musicTracks = [];
        if (_dailyDriveConfig.MusicOptions.TopTracks.Enabled)
        {
            var request = new PersonalizationTopRequest
            {
                Limit = 20,
                Offset = 0,
                TimeRangeParam = PersonalizationTopRequest.TimeRange.MediumTerm
            };

            try
            {
                var topTracks = await _client.Personalization.GetTopTracks(request);

                if (topTracks.Items is not null)
                {
                    foreach (FullTrack fullTrack in topTracks.Items)
                    {
                        musicTracks.Add(new MusicTrack
                        {
                            Uri = fullTrack.Uri
                        });
                    }
                }
            }
            catch (APIException ex)
            {
                Console.Error.WriteLine($"Spotify API error: {ex.Response} {ex.Message}");
            }
        }

        return musicTracks;
    }

    public static List<IMixedTrackList> MixContent(List<Episode> episodes, List<MusicTrack> tracks)
    {
        List<IMixedTrackList> mixed = [];
        // E = Episode, T = Track
        const string mixingPattern = "ETTTT";
        int episodeIndex = 0;
        int trackIndex = 0;
        int patternIndex = 0;

        // Walk through the pattern, placing content in the appropriate slots
        while (episodeIndex < episodes.Count || trackIndex < tracks.Count)
        {
            // Which slot are we on? The pattern repeats using modulo (%)
            char slot = mixingPattern[patternIndex % mixingPattern.Length];

            if (slot == 'E')
            {
                // Podcast slot — place next episode if available
                if (episodeIndex < episodes.Count)
                {
                    mixed.Add(episodes[episodeIndex++]);
                }
            }
            else
            {
                // Music slot (M) — place next track if available
                if (trackIndex < tracks.Count)
                {
                    mixed.Add(tracks[trackIndex++]);
                }
            }

            patternIndex++;

            // Safety valve: if one type is exhausted, dump all remaining items of the other
            // This prevents an infinite loop when the pattern asks for content we don't have
            if (episodeIndex >= episodes.Count && trackIndex < tracks.Count)
            {
                while (trackIndex < tracks.Count)
                {
                    mixed.Add(tracks[trackIndex++]);
                }
                break;
            }
            if (trackIndex >= tracks.Count && episodeIndex < episodes.Count)
            {
                while (episodeIndex < episodes.Count)
                {
                    mixed.Add(episodes[episodeIndex++]);
                }
                break;
            }
        }

        return mixed;
    }
    public async Task UpdatePlaylist(List<IMixedTrackList> mixedList)
    {
        string[] mixedListUris = [.. mixedList.Select(listItem => listItem.Uri)];
        try
        {
            // Replace clears the playlist and sets the first batch (max 100)
            var firstBatch = mixedListUris.Take(100).ToList();
            await _client.Playlists.ReplacePlaylistItems(_dailyDriveConfig.PlaylistId, new PlaylistReplaceItemsRequest(firstBatch));

            // Append any remaining URIs in subsequent batches of 100
            var remaining = mixedListUris.Skip(100).Chunk(100);
            foreach (var batch in remaining)
            {
                await _client.Playlists.AddPlaylistItems(_dailyDriveConfig.PlaylistId, new PlaylistAddItemsRequest([.. batch]));
            }

            Console.WriteLine($"Saved {mixedListUris.Length} item(s) to playlist.");
        }
        catch (APIException ex)
        {
            Console.Error.WriteLine($"Failed to save URIs to playlist ({ex.Response?.StatusCode}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates Daily Drive Playlist
    /// </summary>
    public async Task UpdateDailyDriveAsync()
    {
        // Step 1: Get latest podcast episodes
        List<Episode> podcastEpisodes = await GetPodcastEpisodes(_dailyDriveConfig.Podcasts);

        // Step 2: Load previous state and check if podcasts are the same.
        // If they are, there is no need to refresh Daily Drive.
        State? states = await LoadPreviousState();
        var currentEpisodes = podcastEpisodes.Select(ep => ep.Uri).ToArray();
        string currentEpisodesString = string.Join(',', currentEpisodes);
        string? previousEpisodesString = states?.PreviousEpisodesList;

        if (previousEpisodesString is not null && currentEpisodesString == previousEpisodesString)
        {
            Console.WriteLine("No updates needed");
            Environment.Exit(0);
        }

        // Step 3: Get music tracks
        List<MusicTrack> musicTracks = await fetchTopMusicTracks();

        // Step 4: Layer music in between podcast episodes
        // position "first" episodes are pinned to the top of list
        List<Episode> pinned = [];
        List<Episode> mixable = [];

        foreach (Episode episode in podcastEpisodes)
        {
            if (episode.Position == "first")
            {
                pinned.Add(episode);
            }
            else
            {
                mixable.Add(episode);
            }
        }

        List<IMixedTrackList> mixed = [.. pinned, .. MixContent(mixable, musicTracks)];

        await UpdatePlaylist(mixed);

        // SaveStateToFile()
    }
}
