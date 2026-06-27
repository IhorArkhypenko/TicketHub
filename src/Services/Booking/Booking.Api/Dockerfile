# Build context is the repository root (shared Directory.*.props live there).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config* ./
COPY src/BuildingBlocks ./src/BuildingBlocks
COPY src/Contracts ./src/Contracts
COPY src/Services/Booking ./src/Services/Booking

RUN dotnet restore src/Services/Booking/Booking.Api/Booking.Api.csproj
RUN dotnet publish src/Services/Booking/Booking.Api/Booking.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Booking.Api.dll"]
