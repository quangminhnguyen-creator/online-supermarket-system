FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props OnlineSupermarket.slnx ./
COPY backend ./backend
RUN dotnet restore OnlineSupermarket.slnx
RUN dotnet publish backend/src/OnlineSupermarket.Api/OnlineSupermarket.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OnlineSupermarket.Api.dll"]
