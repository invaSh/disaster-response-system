# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/DispatchService/DispatchService.csproj", "src/DispatchService/"]

# Restore dependencies
RUN dotnet restore "src/DispatchService/DispatchService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/DispatchService"
RUN dotnet build "DispatchService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DispatchService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DispatchService.dll"]
