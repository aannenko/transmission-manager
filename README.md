# Transmission Manager
Do more with Transmission!<br>
- Add torrents by their web page
- Download new TV show episodes on a schedule

### Given
Raspberry Pi with LibreELEC 12 and Docker add-on<br>
Central European Time [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) (use your time zone instead)

### Steps
SSH to your LibreELEC and execute the following commands:

```bash
# Create a Docker network
docker network create transmission-network

# Create these folders
mkdir /storage/transmission/config
mkdir /storage/transmission/watch
mkdir /storage/videos/movies
mkdir /storage/transmission-manager/database

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
  -v /storage/transmission-manager/database:/app/database \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager:latest
```

Alternatively, if you already have a working Transmission Docker container, do this instead:

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
mkdir /storage/transmission-manager/database

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
  -v /storage/transmission-manager/database:/app/database \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager:latest
```

### Result
Now you can send HTTP requests to `http://<docker_host>:9092/api/v1/torrents`

You can, for example, send requests using Visual Studio Code with the REST Client extension - open [TransmissionManager.Api.http](src/TransmissionManager.Api/TransmissionManager.Api.http) in VS Code, change the hostname, request data and start sending requests.

### Scenarios
1. You download new episodes of a tv show once a week from the same torrent tracker web page.

   Let Transmission Manager do this for you - add the address of this web page to Transmission Manager along with a schedule of automatic checks in cron format and a location to download the new episodes to. Optionally, also add a regex pattern which will make Transmission Manager correctly find magnet links on that web page.
