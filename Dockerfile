FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/SignalWatch.Api/SignalWatch.Api.csproj backend/SignalWatch.Api/
RUN dotnet restore backend/SignalWatch.Api/SignalWatch.Api.csproj

COPY . .
RUN dotnet publish backend/SignalWatch.Api/SignalWatch.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

EXPOSE 8080

ENTRYPOINT ["dotnet", "SignalWatch.Api.dll"]