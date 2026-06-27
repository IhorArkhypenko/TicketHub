# Build context is the repository root (shared Directory.*.props live there).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config* ./
COPY src/BuildingBlocks ./src/BuildingBlocks
COPY src/Contracts ./src/Contracts
COPY src/Services/Payment ./src/Services/Payment

RUN dotnet restore src/Services/Payment/Payment.Api/Payment.Api.csproj
RUN dotnet publish src/Services/Payment/Payment.Api/Payment.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]
