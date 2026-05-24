# Update this tag when Microsoft publishes .NET 11 preview container images.
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet restore ../StravaExporter.slnx
RUN dotnet publish src/StravaExporter.Api/StravaExporter.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StravaExporter.Api.dll"]
