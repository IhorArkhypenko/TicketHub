# Build context is the repository root (shared Directory.*.props live there).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config* ./
COPY src/BuildingBlocks ./src/BuildingBlocks
COPY src/Services/Gateway ./src/Services/Gateway

RUN dotnet restore src/Services/Gateway/Gateway/Gateway.csproj
RUN dotnet publish src/Services/Gateway/Gateway/Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
