# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]

# Restore dependencies
RUN dotnet restore "src/NotificationService/NotificationService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
