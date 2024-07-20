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
RUN mkdir /app/Database

# Use runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled
WORKDIR /app
USER $APP_UID
COPY --from=build --chown=$APP_UID /app/ /app/
ENV ConnectionStrings__AppDb="Data Source=/app/Database/TransmissionManager.db"
EXPOSE 9092
ENTRYPOINT ["./TransmissionManager.Api"]
