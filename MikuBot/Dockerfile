FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MikuBot.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY Miku.gif .
VOLUME /app/downloads
ENV ASPNETCORE_URLS=http://+:8080;
EXPOSE 8080

ENTRYPOINT ["dotnet", "MikuBot.dll"]