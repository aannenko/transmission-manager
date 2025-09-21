[![Build and Test](https://github.com/aannenko/transmission-manager/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/dotnetcore.yml) [![Api Docker Image](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish-api.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish-api.yml) [![Web Docker Image](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish-web.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/docker-publish-web.yml) [![CodeQL](https://github.com/aannenko/transmission-manager/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/aannenko/transmission-manager/actions/workflows/codeql-analysis.yml)

# Transmission Manager

This repository contains two projects:

#### [Transmission Manager API](src/TransmissionManager.Api)
A Web API for managing torrents in [Transmission](https://transmissionbt.com/) (a popular open-source torrent client).
Use it to automatically download torrents on a schedule as updates become available (e.g., when new TV show episodes are released).

See how to set it up and use it in the dedicated [README.md](src/TransmissionManager.Api/README.md) file.

#### [Transmission Manager Web](src/TransmissionManager.Web)
A web UI for Transmission Manager API that lets you conveniently control the API from a web interface.
Using it assumes you have already set up Transmission Manager API and can access it.

See how to set it up and use it in the dedicated [README.md](src/TransmissionManager.Web/README.md) file.

## Usage scenarios

### You manually download new episodes of a TV show once a week from the same torrent tracker web page.
Let Transmission Manager do this for you: add the address of this web page along with a cron schedule for when to check for new episodes, and specify a download location. Optionally, add a regex pattern to help Transmission Manager correctly find magnet links on that web page.
