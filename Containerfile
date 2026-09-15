FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj para que el restore quede cacheado
COPY src/TournamentServices.Domain/*.csproj        src/TournamentServices.Domain/
COPY src/TournamentServices.Repositories/*.csproj  src/TournamentServices.Repositories/
COPY src/TournamentServices.Delegates/*.csproj     src/TournamentServices.Delegates/
COPY src/TournamentServices.Api/*.csproj           src/TournamentServices.Api/
RUN dotnet restore src/TournamentServices.Api/TournamentServices.Api.csproj

COPY src/ src/
RUN dotnet publish src/TournamentServices.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TournamentServices.Api.dll"]
