[![Build and Test](https://github.com/aannenko/transmission-manager/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/dotnetcore.yml) [![Docker](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish.yml) [![CodeQL](https://github.com/aannenko/transmission-manager/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/codeql-analysis.yml)

# Transmission Manager
Do more with [Transmission](https://transmissionbt.com/)!<br>
- Add torrents by their web page address
- Automatically download new TV show episodes on a schedule

## How-to
The steps outlined below assume you have a Raspberry Pi with LibreELEC and a Docker add-on installed.

By following these steps, you will set up both Transmission and Transmission Manager to run in Docker on your Raspberry Pi. Transmission will then save files directly to the Raspberry Pi's storage (an SSD is recommended for optimal performance).

Alternatively, you can customize the setup to your preferences, using the steps below as a general guideline. The essential components are a Docker host that supports the `linux/amd64` or `linux/arm64` architecture and Transmission, which should be accessible by the Docker host over the network.

### Given
- Raspberry Pi with LibreELEC 12 and Docker add-on<br>
- Central European Time [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) (use your own time zone instead)

### Setup
There are two ways to set things up:
- [set up Transmission and Transmission Manager from scratch](#set-up-transmission-and-transmission-manager-from-scratch)
- [connect Transmission Manager to a running Transmission container](#connect-transmission-manager-to-a-running-transmission-container)

#### Set up Transmission and Transmission Manager from scratch
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

# Run Transmission Manager
docker run -d \
  --name transmission-manager \
  --hostname transmission-manager \
  --network transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -p 9092:9092 \
  -v /storage/transmission-manager/data:/app/data \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager:latest
```

#### Connect Transmission Manager to a running Transmission container
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

# Run Transmission Manager
# (replace 172.18.0.2 with the IP address of your Transmission container)
docker run -d \
  --name transmission-manager \
  --hostname transmission-manager \
  --network transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -e Transmission__BaseAddress="http://172.18.0.2:9091" \
  -p 9092:9092 \
  -v /storage/transmission-manager/data:/app/data \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager:latest
```

### Send requests
Now that you have set up Transmission Manager, try sending HTTP requests to it from PowerShell 7.</br>
Here are some examples (replace `<docker_host>` with the hostname or IP address of your docker host):
```powershell
# See the first 10 torrents registered in Transmission Manager (use "take=<larger_number>" to see more torrents)
(iwr http://<docker_host>:9092/api/v1/torrents?take=10 | ConvertFrom-Json).torrents

# Register a new torrent in Transmission Manager, send it to Transmission for download and check for torrent updates every day at 11:00 and 17:00
iwr http://<docker_host>:9092/api/v1/torrents -Method Post -ContentType application/json -Body '{"webPageUri":"https://nnmclub.to/forum/viewtopic.php?t=1712711","downloadDir":"/tvshows","cron":"0 11,17 * * *"}'

# Can't wait for Transmission Manager to refresh your torrent #3 at the scheduled time? Force-refresh it yourself!
iwr http://<docker_host>:9092/api/v1/torrents/3 -Method Post -ContentType application/json

# Force-refresh all torrents which are still known to Transmission
(iwr http://<docker_host>:9092/api/v1/torrents | ConvertFrom-Json).torrents | % { iwr "http://<docker_host>:9092/api/v1/torrents/$($_.id)" -Method Post -ContentType application/json }

# Unregister torrent #5 from Transmission Manager but do not touch it in Transmission
iwr http://<docker_host>:9092/api/v1/torrents/5 -Method Delete
```

Alternatively, send requests using [Visual Studio Code](https://code.visualstudio.com/) with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension installed - open the file [TransmissionManager.Api.http](src/TransmissionManager.Api/TransmissionManager.Api.http) in VS Code, change the host address, the request data and start sending requests.

## Usage scenarios

### You manually download new episodes of a TV show once a week from the same torrent tracker web page.
Let Transmission Manager do this for you - add the address of this web page to Transmission Manager along with a schedule in cron format that says when to check for new episodes and a location to download the new episodes to. Optionally, also add a regex pattern which will make Transmission Manager correctly find magnet links on that web page.
