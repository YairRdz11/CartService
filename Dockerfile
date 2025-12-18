# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy custom dependencies
COPY nuget-local/ ./nuget-local/
RUN dotnet nuget remove source nuget-local || true
RUN dotnet nuget add source "/src/nuget-local" --name nuget-local

# Copy reposoritory
COPY . .

# Restore solution
RUN dotnet restore ./CartService.sln


# Publish in release without app host
WORKDIR /src/src/CartService.API
RUN dotnet publish "CartService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar publish artefact
COPY --from=build /app/publish .

# Expose port
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Development


# Run API
ENTRYPOINT ["dotnet", "CartService.API.dll"]
