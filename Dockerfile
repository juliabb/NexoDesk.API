FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NexoDesk.API/NexoDesk.API.csproj", "NexoDesk.API/"]
RUN dotnet restore "NexoDesk.API/NexoDesk.API.csproj"

COPY . .
WORKDIR /src/NexoDesk.API
RUN dotnet publish "NexoDesk.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:NoWarn=CS1591

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/storage/anexos && chown -R ${APP_UID}:${APP_UID} /app

USER ${APP_UID}
EXPOSE 10000

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} exec dotnet NexoDesk.API.dll"]
