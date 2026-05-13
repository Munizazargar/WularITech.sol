# 1. Use the .NET 8 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore dependencies
COPY . .
RUN dotnet restore

# Build and publish the release
RUN dotnet publish -c Release -o /app/publish

# 2. Use the .NET 8 ASP.NET runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway uses the PORT environment variable automatically
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "WularItech solutions.dll"]