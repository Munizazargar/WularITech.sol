FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "WularItech solutions.csproj"
RUN dotnet publish "WularItech solutions.csproj" -c Release -o /app/publish

# DEBUG: show what got published
RUN ls -la /app/publish/wwwroot/css/

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-10000}

ENTRYPOINT ["dotnet", "WularItech solutions.dll"]