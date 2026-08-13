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
| `WebPage` (default) | An HTML page, e.g. a tracker's topic page | The page is scanned for a magnet link with a regular expression - either `magnetRegexPattern` for this torrent, or the global one from the [configuration](./appsettings.json). |
| `JsonPointer` | A JSON document, e.g. a tracker's API endpoint, with an [RFC 6901](https://datatracker.ietf.org/doc/html/rfc6901) JSON Pointer in the URI **fragment** | The pointer selects a 40-character info hash inside the document, and the magnet link is built from it. `magnetRegexPattern` is not used. |

`JsonPointer` is useful when a tracker offers an API, or when its web pages are not reliably readable - for example when they are served behind an anti-bot challenge.

In the `sourceUri` below, `https://api.example.com/v1/topics/f/1106` is fetched and `/result/6880555/7` selects the info hash within the response - here the item at index `7` of the array under the key `6880555`, itself under `result`:

```
https://api.example.com/v1/topics/f/1106#/result/6880555/7
```

The pointer depends entirely on the shape of your API's response, so fetch the endpoint once and look at where the 40-character info hash actually sits before adding the torrent. The hash may be upper- or lower-case; Transmission Manager normalises it.

Because the pointer lives in the fragment, one endpoint can serve many torrents, each with its own pointer.

## Send requests
Now that you have set up Transmission Manager API, try sending HTTP requests to it from PowerShell 7.</br>
Here are some examples (replace `<docker_host>` with the hostname or IP address of your docker host):
```powershell
# See the first 10 torrents registered in Transmission Manager API (use "take=<larger_number>" to see more torrents)
(iwr http://<docker_host>:9092/api/v1/torrents?take=10 | ConvertFrom-Json).torrents

# Register a new torrent in Transmission Manager API, send it to Transmission for download and check for torrent updates every day at 11:00 and 17:00
iwr http://<docker_host>:9092/api/v1/torrents -Method Post -ContentType application/json -Body '{"sourceUri":"https://exampletracker.com/forum/viewtopic.php?t=1712711","downloadDir":"/tvshows","cron":"0 11,17 * * *"}'

# Register a new torrent whose magnet link comes from a JSON API instead of a web page
# (the part after "#" is a JSON Pointer telling Transmission Manager where the info hash sits in the response)
iwr http://<docker_host>:9092/api/v1/torrents -Method Post -ContentType application/json -Body '{"sourceUri":"https://api.example.com/v1/topics/f/1106#/result/6880555/7","sourceKind":"JsonPointer","downloadDir":"/tvshows","cron":"0 11,17 * * *"}'

# Can't wait for Transmission Manager API to refresh your torrent #3 at the scheduled time? Force-refresh it yourself!
iwr http://<docker_host>:9092/api/v1/torrents/3 -Method Post -ContentType application/json

# Force-refresh all torrents which are still known to Transmission
(iwr http://<docker_host>:9092/api/v1/torrents | ConvertFrom-Json).torrents | % { iwr "http://<docker_host>:9092/api/v1/torrents/$($_.id)" -Method Post -ContentType application/json }

# Unregister torrent #5 from Transmission Manager API but do not touch it in Transmission
# (the version query parameter is required and must match the torrent's current Version)
iwr http://<docker_host>:9092/api/v1/torrents/5?version=1 -Method Delete
```

Alternatively, send requests using [Visual Studio Code](https://code.visualstudio.com/) with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension installed - open the file [Torrents.http](Actions/Torrents/Torrents.http) in VS Code, change the host address, the request data and start sending requests.

Using the API, you can also request information from Transmission Manager API about itself via [AppVersion.http](Actions/AppVersion/AppVersion.http).

## Optimistic concurrency control
Each torrent has a `Version` field (returned in every torrent JSON response). `PATCH` and `DELETE` both require a `version` query parameter that must match the torrent's current `Version`.

- `204 No Content` — operation succeeded; the torrent's `Version` is now `version + 1` (only on `PATCH`).
- `400 Bad Request` — the request was malformed. For `PATCH`, this includes a body with every field set to `null`; at least one field must be provided.
- `404 Not Found` — no torrent with the given id exists.
- `409 Conflict` — the torrent was modified by someone else; the response's `currentVersion` extension carries the latest known `Version` so the client can refetch and retry.

A successful `PATCH` advances `Version` by exactly one. The current `Version` is included in every torrent JSON response, so clients can capture it and pass it back on the next `PATCH` or `DELETE`.

## Setup a Web UI (optional)
If you prefer a web interface to manage your torrents, see the [Transmission Manager Web readme](../TransmissionManager.Web/README.md).