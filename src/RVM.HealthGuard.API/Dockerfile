FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/RVM.HealthGuard.API/RVM.HealthGuard.API.csproj", "src/RVM.HealthGuard.API/"]
COPY ["src/RVM.HealthGuard.Domain/RVM.HealthGuard.Domain.csproj", "src/RVM.HealthGuard.Domain/"]
COPY ["src/RVM.HealthGuard.Infrastructure/RVM.HealthGuard.Infrastructure.csproj", "src/RVM.HealthGuard.Infrastructure/"]
RUN dotnet restore "./src/RVM.HealthGuard.API/RVM.HealthGuard.API.csproj"
COPY . .
WORKDIR "/src/src/RVM.HealthGuard.API"
RUN dotnet build "./RVM.HealthGuard.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RVM.HealthGuard.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RVM.HealthGuard.API.dll"]
