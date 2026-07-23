FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5048

# Restore dependency
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Arma3WebService/Arma3WebService.csproj", "Arma3WebService/"]
COPY ["Components/Components.csproj", "Components/"]
#COPY ["Arma3WebService.Migrations.MySQL/Arma3WebService.Migrations.MySQL.csproj", "Arma3WebService.Migrations.MySQL/"]
#COPY ["Arma3WebService.DBContext/Arma3WebService.DBContext.csproj", "Arma3WebService.DBContext/"]
#COPY ["Arma3WebService.Migrations.NpgSQL/Arma3WebService.Migrations.NpgSQL.csproj", "Arma3WebService.Migrations.NpgSQL/"]
#COPY ["Arma3WebService.Migrations.SQLite/Arma3WebService.Migrations.SQLite.csproj", "Arma3WebService.Migrations.SQLite/"]
RUN dotnet restore "Arma3WebService/Arma3WebService.csproj"
COPY . .

# Build the image
FROM restore AS publish
WORKDIR "/src/Arma3WebService"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Arma3WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arma3WebService.dll"]
