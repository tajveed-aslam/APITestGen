# Build: docker build -t apitestgen-api ./backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ApiTestGen.Api/ApiTestGen.Api.csproj ApiTestGen.Api/
RUN dotnet restore ApiTestGen.Api/ApiTestGen.Api.csproj
COPY ApiTestGen.Api/ ApiTestGen.Api/
RUN dotnet publish ApiTestGen.Api/ApiTestGen.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    Database__MigrateOnStartup=true
# Render (and most PaaS hosts) inject PORT; default to 8080 elsewhere.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet ApiTestGen.Api.dll"]
