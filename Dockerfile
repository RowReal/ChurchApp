# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy everything from repo
COPY . .

# Restore dependencies
RUN dotnet restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Render uses port 10000
EXPOSE 10000

# IMPORTANT: Replace with your exact dll name if different
ENTRYPOINT ["dotnet", "ChurchApp.dll"]
