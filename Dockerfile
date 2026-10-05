# Built by .github/workflows/deploy.yml and pushed to Artifact Registry.
#
# The standard two-stage .NET image, as Microsoft's container docs teach it: restore and
# publish on the SDK image, run on the slimmer ASP.NET runtime image.
#   - the csproj is copied and restored first, so the package layer is cached until the
#     project's dependencies change;
#   - Program.cs reads PORT at RUNTIME and listens on 0.0.0.0:$PORT (compose sets it);
#   - runs as the image's built-in non-root `app` user ($APP_UID).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/App/App.csproj src/App/
RUN dotnet restore src/App/App.csproj
COPY src/ src/
RUN dotnet publish src/App/App.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG BUILD_ID=""
WORKDIR /app
# ASPNETCORE_HTTP_PORTS is cleared: the base image sets it to 8080, and Program.cs binds $PORT.
ENV PORT=8080 BUILD_ID=$BUILD_ID DOTNET_CLI_TELEMETRY_OPTOUT=1 ASPNETCORE_HTTP_PORTS=
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "App.dll"]
