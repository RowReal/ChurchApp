# 1️⃣ Build stage: use .NET SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
COPY ChurchApp/*.csproj ./ChurchApp/

# 1️⃣ Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy the csproj file and restore dependencies
COPY churchapp/churchapp/churchapp/*.csproj ./churchapp/
RUN dotnet restore ./churchapp/*.csproj

# Copy everything else
COPY . ./

# Publish the app
RUN dotnet publish ./churchapp/churchapp/churchapp/churchapp.csproj -c Release -o /app/out

# 2️⃣ Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Copy from build stage
COPY --from=build /app/out ./

# Expose port 10000
EXPOSE 10000

# Run the app
ENTRYPOINT ["dotnet", "churchapp.dll"]
