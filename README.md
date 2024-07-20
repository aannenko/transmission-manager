# Transmission Manager
Do more with Transmission!<br>
- Add torrents by their web page
- Download new TV show episodes on a schedule

### Given
Raspberry Pi with LibreELEC 12<br>
Central European Time [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) (change it to your time zone)

### Steps
```bash
# Create a Docker network
docker network create transmission-network

# Create these folders
mkdir /storage/transmission/config
mkdir /storage/transmission/watch

# Run Transmission
docker run -d \
  --name=transmission \
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
  --name=transmission-manager \
  --hostname=transmission-manager \
  --network=transmission-network \
  -e PUID=0 \
  -e PGID=0 \
  -e TZ=Europe/Prague \
  -p 9092:9092 \
  -v /storage/transmission-manager/database:/app/database \
  --restart unless-stopped \
  ghcr.io/aannenko/transmission-manager:latest
```

Now you can send HTTP requests to `http://<docker_host>:9092/api/v1/torrents`

You can, for example, send requests using Visual Studio Code with the REST Client extension - open [TransmissionManager.Api.http](src/TransmissionManager.Api/TransmissionManager.Api.http) in VS Code, change the hostname, request data and start sending requests.