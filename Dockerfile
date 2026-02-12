# 1️⃣ Build stage: use .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
COPY ChurchApp/*.csproj ./ChurchApp/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . ./

# Publish the app in Release mode
RUN dotnet publish -c Release -o /app/out

# 2️⃣ Runtime stage: smaller image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copy the published output from build stage
COPY --from=build /app/out ./

# Expose port 10000 (Render will use this)
EXPOSE 10000

# Start the app
ENTRYPOINT ["dotnet", "ChurchApp.dll"]
