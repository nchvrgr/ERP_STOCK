FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore servidor/src/ApiWeb/ApiWeb.csproj
RUN dotnet publish servidor/src/ApiWeb/ApiWeb.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://0.0.0.0:${PORT:-8080}} dotnet ApiWeb.dll"]
