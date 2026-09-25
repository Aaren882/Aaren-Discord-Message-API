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
RUN dotnet restore "Arma3WebService/Arma3WebService.csproj"
COPY . .

# Build the image
FROM restore AS publish
WORKDIR "/src/Arma3WebService"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Arma3WebService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID /app/publish .

ENTRYPOINT ["dotnet", "Arma3WebService.dll"]
