# Transmission Manager Web
This web app lets you control your Transmission Manager using a convenient web interface.
- Connect to different Transmission Manager instances
- Manage all the torrents on the connected instance, refresh or delete them on one page
- Add torrents by their web page address, specify their refresh schedule using [cron](https://crontab.guru)

## Pre-requisites
- A running instance of [Transmission Manager API](../TransmissionManager.Api/README.md) that you can access at `http://<api-host>:9092/api/v1/torrents`.
- A Docker host that supports the `linux/amd64` or `linux/arm64` architecture and can reach the Transmission Manager API instance over the network. Can be the same machine that hosts Transmission Manager API.

## Setup
After you have finished setting up Transmission and Transmission Manager API, SSH to the machine where you want Transmission Manager Web to be hosted (usually the same machine where Transmission Manager API runs) and execute the following command:
```bash
docker run -d \
	--name transmission-manager-web \
	-p 9093:80 \
	--restart unless-stopped \
	ghcr.io/aannenko/transmission-manager-web:latest
```

Then open the UI at `http://<docker-host>:9093/`.

## Connecting to the API
- By default, the web app connects to `http://<docker-host>:9092` (same host, port 9092). This can be changed from the UI to point to any reachable Transmission Manager API instance.
- If your API runs on a different host, ensure it is reachable from the browser.