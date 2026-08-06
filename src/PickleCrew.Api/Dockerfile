# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props ./
COPY src/PickleCrew.Api/PickleCrew.Api.csproj src/PickleCrew.Api/
COPY src/PickleCrew.ServiceDefaults/PickleCrew.ServiceDefaults.csproj src/PickleCrew.ServiceDefaults/
RUN dotnet restore src/PickleCrew.Api/PickleCrew.Api.csproj --runtime linux-x64

COPY src/PickleCrew.Api/ src/PickleCrew.Api/
COPY src/PickleCrew.ServiceDefaults/ src/PickleCrew.ServiceDefaults/
RUN dotnet publish src/PickleCrew.Api/PickleCrew.Api.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained false \
    --no-restore \
    --output /app \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
STOPSIGNAL SIGTERM
COPY --from=build --chown=app:app /app/ ./
USER app
ENTRYPOINT ["dotnet", "PickleCrew.Api.dll"]
