# Use SDK image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG TARGETARCH
ARG BINARY_VERSION
WORKDIR /source

# Copy everything and publish the app
COPY TransmissionManager.Api/. TransmissionManager.Api/
RUN (echo "$BINARY_VERSION" | grep -Eq "^[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?$") && \
    dotnet publish TransmissionManager.Api -a $TARGETARCH -o /app -p:Version=$BINARY_VERSION || \
    dotnet publish TransmissionManager.Api -a $TARGETARCH -o /app
RUN mkdir /app/database

# Use runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled
ARG PUID=1000
ARG PGID=1000
USER $PUID:$PGID
COPY --from=build /app/ /app/
WORKDIR /app
ENTRYPOINT ["./TransmissionManager.Api"]
