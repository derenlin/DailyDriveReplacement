# Spotify Daily Drive Generator

A C# console app that authenticates with Spotify and creates (or refreshes) a **Daily Drive** playlist in your account.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A [Spotify Developer account](https://developer.spotify.com/dashboard)

---

## 1. Create a Spotify App

1. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) and create a new app.
2. Under **Redirect URIs**, add `http://localhost:5000/callback`.
3. Copy your **Client ID** and **Client Secret**.

---

## 2. Configure credentials

Open `appsettings.json` and fill in your credentials:

```json
{
  "Spotify": {
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
    "RedirectUri": "YOUR_REDIRECT_URI_HERE",
    "PlaylistName": "Daily Drive",
    "PlaylistDescription": "Your personalized Daily Drive playlist."
  }
}
```

## 3. Configure daily drive settings

Open `dailydrive-config.json` and fill in your options

```json
{
  "DailyDrive": {
    "PlaylistId": "Your Daily Drive Playlist ID",
        "Podcasts": [
          {
            "id": "Podcast show ID",
            "position": "first"
          },
        ],
        "Music": {}
    }
}
```

---

## 4. Run the app

```bash
dotnet restore
dotnet run
```

The app will open your browser for the Spotify login/authorization. After you approve, it will:

1. Find or create a playlist named **Daily Drive** in your account.
2. Populate it with the tracks defined in `Program.cs`.
