FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Tournament.Api/*.csproj Tournament.Api/
COPY Tournament.Domain/*.csproj Tournament.Domain/
COPY Tournament.Delegate/*.csproj Tournament.Delegate/
COPY Tournament.Repository/*.csproj Tournament.Repository/
COPY Tournament.Common/*.csproj Tournament.Common/
RUN dotnet restore Tournament.Api/*.csproj

COPY . .
RUN dotnet publish Tournament.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Tournament.Api.dll"]
