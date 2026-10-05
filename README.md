# ASP.NET Core template

Provisioned from [`Qode-Fleet-Control/fleet-template-v1`](https://github.com/Qode-Fleet-Control/fleet-template-v1) — the fleet
lifecycle contract (`bin/`, `fleet.conf`, `compose.yaml`, deploy workflows) with the stock
ASP.NET Core Web API (minimal APIs, .NET 10) laid on top.

    src/App/            the web API (Program.cs, appsettings*.json, App.http)
    Dockerfile          SDK build stage -> aspnet:10.0 runtime, non-root
    compose.yaml        the fleet's docker runtime (service `app`)
    fleet.conf          the app manifest every bin/ script reads

Routes: `GET /` (a tiny JSON status), `GET /health` (ASP.NET Core health checks — the
fleet's `HEALTH_PATH`), `GET /weatherforecast` (the generator's sample), and `/openapi/v1.json`
in Development.

## Origin

Generated 2026-10-05 with the official template, inside the official SDK image (.NET SDK 10.0.401):

    docker run --rm -u $(id -u):$(id -g) -e HOME=/tmp -v "$PWD":/w -w /w \
      mcr.microsoft.com/dotnet/sdk:10.0 \
      dotnet new webapi -n App -o src/App --framework net10.0

## Running it

**On the fleet** — nothing to do: the fleet clones the repo, injects `PORT` / `DATABASE_URL`, and
runs `bin/run`, which (docker runtime) does `docker compose build` then
`docker compose up --remove-orphans` in the foreground. The app listens on `0.0.0.0:$PORT` and
is served at the root of its own hostname (`https://<hash>.<FLEET_APP_DOMAIN>/`).

**With docker**

    PORT=8080 bin/run                 # or: docker compose up --build
    curl http://localhost:8080/health

**Without docker** (needs the .NET 10 SDK on PATH)

    FLEET_RUNTIME=process PORT=8080 bin/run
    # = dotnet restore src/App/App.csproj
    #   dotnet publish src/App/App.csproj -c Release --no-restore -o .out
    #   env PORT=8080 dotnet .out/App.dll

or, for development with the generator's launch profile: `dotnet run --project src/App`
(http://localhost:5046, `ASPNETCORE_ENVIRONMENT=Development`).

| step | process runtime | docker runtime |
|---|---|---|
| install | `dotnet restore src/App/App.csproj` | — |
| build | `dotnet publish … -o .out` | `docker compose build` |
| start | `env PORT="$PORT" dotnet .out/App.dll` | `docker compose up --remove-orphans` |

## Deviations from the stock generator output, and why

- **Project under `src/App/`, not the repo root.** .NET writes build output to the project's
  `bin/` and `obj/`; at the root that would collide with the fleet's `bin/` lifecycle scripts.
- **`Program.cs` binds `http://0.0.0.0:$PORT` when `PORT` is set**, read at runtime. The fleet
  injects `PORT`; ASP.NET Core does not read that variable on its own. Without `PORT`, Kestrel
  keeps its usual defaults (launchSettings / `ASPNETCORE_URLS`).
- **`AddHealthChecks()` + `MapHealthChecks("/health")`** and a `GET /` route: the fleet's
  health probe, and a root that answers instead of 404.
- **`UseHttpsRedirection()` moved into the Development block.** On the fleet the edge
  terminates TLS; the container speaks plain HTTP, so there is no https port to redirect to
  (the stock line only logged "Failed to determine the https port" on every request).
- **Dockerfile clears `ASPNETCORE_HTTP_PORTS`** (the aspnet image sets it to 8080), so Kestrel
  does not warn that `UseUrls` overrides it.
- Added: `Dockerfile`, `compose.yaml`, `.dockerignore`, a compact `.gitignore` (the stock
  `dotnet new gitignore` ignores every `bin/` — including the fleet's), `.env.example`,
  `fleet.conf`, `bin/`, `.github/workflows/`, `docs/fleet-lifecycle.md`.
- No NuGet lock file: the generator does not create one (`RestorePackagesWithLockFile` is off).

## Verified

On 2026-10-05, docker 29.8.2:

- `migrate.py audit` → `READY`.
- `verify.sh <repo> 46201` → `run=200 restart=200 containers_after_stop=0` (bin/run, probe
  `/health`, bin/restart, probe again, bin/stop).
- Process runtime, inside `mcr.microsoft.com/dotnet/sdk:10.0`:
  `FLEET_RUNTIME=process PORT=46202 bin/run` → `/health` 200, `/` and `/weatherforecast` 200.

See `docs/fleet-lifecycle.md` for the lifecycle contract.
