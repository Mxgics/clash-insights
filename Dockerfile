FROM node:24.19-alpine AS web-build
WORKDIR /source/web
COPY web/package.json web/package-lock.json ./
RUN npm ci --no-audit
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /source
COPY Directory.Build.props dotnet-tools.json ./
COPY src/ClashInsights.Api/ClashInsights.Api.csproj src/ClashInsights.Api/packages.lock.json src/ClashInsights.Api/
RUN dotnet restore src/ClashInsights.Api/ClashInsights.Api.csproj --locked-mode
COPY src/ClashInsights.Api/ src/ClashInsights.Api/
COPY --from=web-build /source/web/dist/web/browser/ src/ClashInsights.Api/wwwroot/
RUN dotnet publish src/ClashInsights.Api/ClashInsights.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ClashInsights.Api.dll"]
