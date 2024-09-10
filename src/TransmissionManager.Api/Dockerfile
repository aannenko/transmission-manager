# Use SDK image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG TARGETARCH
ARG BINARY_VERSION
WORKDIR /source

# Copy everything and publish the app
COPY Directory.Packages.props .
COPY TransmissionManager.Database/. TransmissionManager.Database/
COPY TransmissionManager.TorrentTrackerClient/. TransmissionManager.TorrentTrackerClient/
COPY TransmissionManager.TransmissionClient/. TransmissionManager.TransmissionClient/
COPY TransmissionManager.Api/. TransmissionManager.Api/
RUN (echo "$BINARY_VERSION" | grep -Eq "^[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?$") && \
    dotnet publish TransmissionManager.Api -a $TARGETARCH -o /app -p:Version=$BINARY_VERSION || \
    dotnet publish TransmissionManager.Api -a $TARGETARCH -o /app
RUN mkdir /app/database

# Use runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled-extra
ARG PUID
ARG PGID
WORKDIR /app
USER $PUID:$PGID
COPY --from=build /app/ /app/
ENTRYPOINT ["./TransmissionManager.Api"]
