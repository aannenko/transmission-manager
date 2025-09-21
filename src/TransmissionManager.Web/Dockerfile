# Use SDK image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BINARY_VERSION
WORKDIR /source

# Copy everything and publish the app
COPY Directory.Packages.props .
COPY TransmissionManager.Api.Common/. TransmissionManager.Api.Common/
COPY TransmissionManager.Web/. TransmissionManager.Web/
RUN apt update \
    && apt install -y python3 \
    && dotnet workload install wasm-tools
RUN (echo "$BINARY_VERSION" | grep -Eq "^[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?$") && \
    dotnet publish TransmissionManager.Web -o /app -p:Version=$BINARY_VERSION || \
    dotnet publish TransmissionManager.Web -o /app

# Use runtime image
FROM nginx:alpine
COPY --from=build /app/wwwroot/ /usr/share/nginx/html/
COPY TransmissionManager.Web/nginx.conf /etc/nginx/nginx.conf
