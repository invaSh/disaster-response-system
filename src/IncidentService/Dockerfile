# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/IncidentService/IncidentService.csproj", "src/IncidentService/"]

# Restore dependencies
RUN dotnet restore "src/IncidentService/IncidentService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/IncidentService"
RUN dotnet build "IncidentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "IncidentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IncidentService.dll"]
