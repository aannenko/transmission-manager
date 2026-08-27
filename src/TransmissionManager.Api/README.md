# Transmission Manager API
This application lets you manage your torrents in [Transmission](https://transmissionbt.com/) in a unique way:
1. Add torrents by the address of a tracker web page, or of a JSON API endpoint that exposes the torrent's info hash.
2. Schedule periodic magnet link refreshes (e.g., when a new TV show episode is released) using [cron](https://crontab.guru) syntax. Updated magnets are sent to Transmission automatically, replacing existing torrents while preserving the downloaded files.

## Important setup notes
The steps outlined below assume you have a Raspberry Pi with [LibreELEC](https://libreelec.tv/) and a Docker add-on installed.

By following these steps, you will set up both Transmission and Transmission Manager API to run in Docker on your Raspberry Pi. Transmission will then save files directly to the Raspberry Pi's storage (an SSD is recommended for optimal performance).

Alternatively, you can adapt the setup to your environment; the steps below serve as a general guideline. At a minimum, you need:
- A Docker host capable of running `linux/amd64` or `linux/arm64` images.
- A Transmission instance reachable from the Docker host and the Transmission Manager API container over the network.

## Pre-requisites
- Raspberry Pi with LibreELEC 12 and Docker add-on
- Central European Time [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) (use your own time zone instead)

## Setup
There are two ways to set things up:
- [set up Transmission and Transmission Manager API from scratch](#option-1-set-up-transmission-and-transmission-manager-api-from-scratch)
- [connect Transmission Manager API to a running Transmission container](#option-2-connect-transmission-manager-api-to-a-running-transmission-container)

### Option 1: set up Transmission and Transmission Manager API from scratch
SSH to your LibreELEC and execute the following commands:
```bash
# Create a Docker network
docker network create transmission-network

# Create these folders
mkdir -p /storage/transmission/config
mkdir -p /storage/transmission/watch
mkdir -p /storage/videos/movies
mkdir -p /storage/transmission-manager/data/db

# Run Transmission
docker run -d \
  --name transmission \
  --hostname transmission \
  --network transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -p 9091:9091 \
  -p 51413:51413 \
  -p 51413:51413/udp \
  -v /storage/transmission/config:/config \
  -v /storage/transmission/watch:/watch \
  -v /storage/downloads:/downloads \
  -v /storage/tvshows:/tvshows \
  -v /storage/videos/movies:/movies \
  --restart unless-stopped \
  lscr.io/linuxserver/transmission:latest

# Run Transmission Manager API
docker run -d \
  --name transmission-manager-api \
  --hostname transmission-manager-api \
  --network transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -p 9092:9092 \
  -v /storage/transmission-manager/data:/app/data \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager-api:latest
```

### Option 2: connect Transmission Manager API to a running Transmission container
SSH to your LibreELEC and execute the following commands:
```bash
# Create a Docker network
docker network create transmission-network

# Find the ID of your Transmission container
# (should look similar to 228b4333c2cd)
docker ps

# Add your Transmission container to this network
# (replace 228b4333c2cd with the ID of your Transmission container)
docker network connect transmission-network 228b4333c2cd

# Find the Transmission's IP address within transmission-network
# (replace 228b4333c2cd with the ID of your Transmission container,
# look for the node called "transmission-network" and within it - "IPAddress",
# the IP address should look similar to 172.18.0.2)
docker inspect 228b4333c2cd

# Create a folder for TransmissionManager.db
mkdir -p /storage/transmission-manager/data/db

# Run Transmission Manager API, pointing it to the Transmission container's IP address
# (replace 172.18.0.2 with the IP address of your Transmission container)
docker run -d \
  --name transmission-manager-api \
  --hostname transmission-manager-api \
  --network transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -e Transmission__BaseAddress="http://172.18.0.2:9091" \
  -p 9092:9092 \
  -v /storage/transmission-manager/data:/app/data \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager-api:latest
```

## Torrent sources
Every torrent has a `sourceUri` that Transmission Manager re-reads on each refresh, and a `sourceKind` that decides how it is read. `sourceKind` is optional when adding a torrent and defaults to `WebPage`.

| `sourceKind` | `sourceUri` points to | How the magnet link is obtained |
| --- | --- | --- |
| `WebPage` (default) | An HTML page, e.g. a tracker's topic page | The page is scanned for a magnet link with a regular expression - either `magnetRegexPattern` for this torrent, or `TorrentSources:WebPage:DefaultMagnetRegexPattern` from the [configuration](./appsettings.json). The regular expression needs to contain `magnet:\?`, and its whole match has to be the magnet link itself. |
| `JsonPointer` | A JSON document, e.g. a tracker's API endpoint, with an [RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901) JSON Pointer in the URI **fragment** | The pointer selects a string, which `magnetRegexPattern` and `jsonValueFormat` turn into a magnet link - see [From a JSON value to a magnet link](#from-a-json-value-to-a-magnet-link). |

Whichever kind reads it, `magnetRegexPattern` has to be a valid .NET regular expression of at most 512 characters, and is built with `RegexOptions.ExplicitCapture` - a plain `(…)` only groups, so name a group to capture or backreference it.

`JsonPointer` is useful when a tracker offers an API, or when its web pages are not reliably readable - for example when they are served behind an anti-bot challenge.

In the `sourceUri` below, `https://api.example.com/v1/topics/f/1106` is fetched and `/result/6880555/7` selects a value within the response - here the item at index `7` of the array under the key `6880555`, itself under `result`:

```
https://api.example.com/v1/topics/f/1106#/result/6880555/7
```

The pointer depends entirely on the shape of your API's response, so fetch the endpoint once and look at where the value you need actually sits before adding the torrent. It has to address a string; a pointer that addresses nothing, or something that is not a string, is refused with a message saying which.

Because the pointer lives in the fragment, one endpoint can serve many torrents, each with its own pointer.

### From a JSON value to a magnet link
The string a pointer addresses is rarely a magnet link already - most often it is a bare info hash. Two optional, independent steps bridge the gap:

1. `magnetRegexPattern` extracts the value out of the addressed string. Its **whole match** is taken, so a pattern that needs surrounding context to find the right place excludes that context with a zero-width lookaround, as in `(?<=btih:)[a-fA-F0-9]{40}`. Unlike a `WebPage` pattern, it does not have to look for a magnet link.
2. `jsonValueFormat` builds the magnet link out of that value, which its only placeholder, `{0}`, stands for - as in `magnet:?xt=urn:btih:{0}`. No other placeholder and no other brace is allowed.

Use neither, either or both; with neither, the addressed string is used as it is. Whatever comes out has to be an absolute `magnet:` URI, or the request is refused.

Left out or empty, each of the two falls back to a default from the configuration - and **both shipped defaults are empty**, because no pair of them is right for every API. Wrapping an already-complete magnet link in `magnet:?xt=urn:btih:{0}` would drop its `&dn=` and its passkey-bearing `&tr=`, and would turn a [BitTorrent v2](https://www.bittorrent.org/beps/bep_0052.html) `btmh` magnet into a valid-looking `btih` one carrying a meaningless hash.

So either set these fields per torrent, or - if all your JSON sources share a shape - configure the defaults for all torrents in Transmission Manager by adding these to the `docker run` command:

```bash
  -e TorrentSources__JsonPointer__DefaultJsonValueRegexPattern='[a-fA-F0-9]{40}' \
  -e TorrentSources__JsonPointer__DefaultJsonValueFormat='magnet:?xt=urn:btih:{0}' \
```

## Send requests
Now that you have set up Transmission Manager API, try sending HTTP requests to it from PowerShell 7.</br>
Here are some examples (replace `<docker_host>` with the hostname or IP address of your docker host):
```powershell
# See the first 10 torrents registered in Transmission Manager API (use "take=<larger_number>" to see more torrents)
(iwr http://<docker_host>:9092/api/v1/torrents?take=10 | ConvertFrom-Json).torrents

# Register a new torrent in Transmission Manager API, send it to Transmission for download and check for torrent updates every day at 11:00 and 17:00
iwr http://<docker_host>:9092/api/v1/torrents -Method Post -ContentType application/json -Body '{"sourceUri":"https://exampletracker.com/forum/viewtopic.php?t=1712711","downloadDir":"/tvshows","cron":"0 11,17 * * *"}'

# Register a new torrent whose magnet link comes from a JSON API instead of a web page
# (the part after "#" is a JSON Pointer telling Transmission Manager where the value sits in the response,
# magnetRegexPattern picks the info hash out of that value and jsonValueFormat builds the magnet link;
# leave both out if you have configured deployment-wide defaults for them)
iwr http://<docker_host>:9092/api/v1/torrents -Method Post -ContentType application/json -Body '{"sourceUri":"https://api.example.com/v1/topics/f/1106#/result/6880555/7","sourceKind":"JsonPointer","magnetRegexPattern":"[a-fA-F0-9]{40}","jsonValueFormat":"magnet:?xt=urn:btih:{0}","downloadDir":"/tvshows","cron":"0 11,17 * * *"}'

# Can't wait for Transmission Manager API to refresh your torrent #3 at the scheduled time? Force-refresh it yourself!
iwr http://<docker_host>:9092/api/v1/torrents/3 -Method Post -ContentType application/json

# Force-refresh all torrents which are still known to Transmission
(iwr http://<docker_host>:9092/api/v1/torrents | ConvertFrom-Json).torrents | % { iwr "http://<docker_host>:9092/api/v1/torrents/$($_.id)" -Method Post -ContentType application/json }

# Change torrent #3's schedule and the format its magnet link is built with, leaving its other fields alone
# (the version query parameter is required and must match the torrent's current Version;
# send magnetRegexPattern or jsonValueFormat as "" to clear it and fall back to the configured default,
# or cron as "" to stop refreshing the torrent on a schedule)
iwr http://<docker_host>:9092/api/v1/torrents/3?version=1 -Method Patch -ContentType application/json -Body '{"jsonValueFormat":"magnet:?xt=urn:btih:{0}","cron":"0 9,20 * * *"}'

# Unregister torrent #5 from Transmission Manager API but do not touch it in Transmission
# (the version query parameter is required and must match the torrent's current Version)
iwr http://<docker_host>:9092/api/v1/torrents/5?version=1 -Method Delete
```

Alternatively, send requests using [Visual Studio Code](https://code.visualstudio.com/) with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension installed - open the file [Torrents.http](Actions/Torrents/Torrents.http) in VS Code, change the host address, the request data and start sending requests.

Using the API, you can also request information from Transmission Manager API about itself via [AppVersion.http](Actions/AppVersion/AppVersion.http).

## Conflicting changes
Each torrent has a `version` field, returned in every torrent JSON response. When a new torrent is added to Transmission Manager, it starts with `version = 1`, and goes up by 1 whenever its stored data changes - when you update it, when a refresh finds a newer torrent to download, and when Transmission reports a name for a torrent that did not have one yet. A refresh that finds the torrent you already have changes nothing, `version` included.
To prevent from overwriting someone's changes, `PATCH` (update) and `DELETE` require a `version` query parameter that matches the torrent's current `version`.
In case of such conflict, the request is rejected with a `409 Conflict` response, carrying the torrent's `currentVersion` in the response extensions.
In such cases it is recommended to refetch the torrent, check that your change still makes sense against its new data, and potentially resubmit against that version.

## Setup a Web UI (optional)
If you prefer a web interface to manage your torrents, see the [Transmission Manager Web readme](../TransmissionManager.Web/README.md).