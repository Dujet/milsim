# FreeTAKServer Setup (Docker, Windows)

Reference doc for running the FreeTAKServer (FTS) backend this project's CoT networking talks to. Referenced from the main [README](../README.md).

## Prerequisites

- Docker Desktop installed and running
- PowerShell or CMD

## Install

FTS is hosted on GHCR (GitHub Container Registry), not Docker Hub:

```powershell
docker pull ghcr.io/freetakteam/freetakserver:latest
docker pull ghcr.io/freetakteam/ui:latest
```

Download the official example compose file:

```powershell
curl -o compose.yaml https://raw.githubusercontent.com/FreeTAKTeam/FreeTAKHub-Installation/refs/heads/main/containers/example-compose.yaml
```

**The downloaded file has two bugs — fix them before running:**

1. `FTS_IP` in the `freetakserver-ui` service is a hardcoded IP that doesn't exist → change to `'freetakserver'`.
2. `FTS_UI_SQLALCHEMY_DATABASE_URI` points at a `data/` subdirectory that's never created → drop `data/` from the path.
3. (Windows-specific) the `:Z` SELinux flag on the UI volume is irrelevant on Windows and can be dropped.

A corrected `compose.yaml` is below — use this instead of hand-patching the download.

## Run

```powershell
docker compose -f compose.yaml up -d      # start
docker compose -f compose.yaml down       # stop
docker compose -f compose.yaml logs -f    # live logs
```

## Verify

| What | URL / command |
|---|---|
| Web UI | http://localhost:5000 |
| CoT TCP (Unity connects here) | localhost:8087 |
| FTS version | `docker compose -f compose.yaml exec freetakserver pip show FreeTAKServer` |

If `exec` says "No such container", run `docker ps` to find the real container name and use `docker exec <name> pip show FreeTAKServer` instead.

## Ports

| Port | Purpose |
|---|---|
| 8080 | Data Package (HTTP) |
| 8087 | CoT TCP — TAK clients and Unity connect here |
| 8089 | CoT SSL |
| 8443 | Data Package (SSL) |
| 9000 | Federation |
| 5000 | Web UI |

## Notes for Unity integration

- Unity **sends** entity positions by opening a TCP connection to FTS port `8087` and writing CoT XML.
- Unity **receives** operator markers over that same TCP connection.
- FTS broadcasts all received CoT events to every connected client, including back to Unity.
- This project doesn't use the FTS REST API — in testing it was unreliable for driving simulation entities. All entity traffic goes over the CoT TCP connection.

## Corrected `compose.yaml`

```yaml
services:
  freetakserver:
    image: ghcr.io/freetakteam/freetakserver:latest
    hostname: freetakserver
    networks:
        - taknet
    volumes:
      - free-tak-core-db:/opt/fts/
    ports:
      - 8080:8080
      - 8087:8087
      - 8089:8089
      - 8443:8443
      - 9000:9000
      - 19023:19023
    environment:
        FTS_FED_PASSWORD: "defaultpass"
        FTS_CLIENT_CERT_PASSWORD: "supersecret"
        FTS_WEBSOCKET_KEY: "YourWebsocketKey"
        FTS_SECRET_KEY: "vnkdjnfjknfl1232#"
        FTS_CONNECTION_MESSAGE: "Welcome to FreeTAKServer. The Parrot is not dead. It's just resting"
        FTS_COT_PORT: 8087
        FTS_SSLCOT_PORT: 8089
        FTS_API_PORT: 19023
        FTS_FED_PORT: 9000
        FTS_DP_ADDRESS: 'freetakserver'
        FTS_USER_ADDRESS: 'freetakserver'
        FTS_API_ADDRESS: 'freetakserver'
        FTS_ROUTING_PROXY_SUBSCRIBE_PORT: 19030
        FTS_ROUTING_PROXY_SUBSCRIBE_IP: 'freetakserver'
        FTS_ROUTING_PROXY_PUBLISHER_PORT: 19032
        FTS_ROUTING_PROXY_PUBLISHER_IP: 'freetakserver'
        FTS_ROUTING_PROXY_SERVER_PORT: 19031
        FTS_ROUTING_PROXY_SERVER_IP: 'freetakserver'
        FTS_INTEGRATION_MANAGER_PULLER_PORT: 19033
        FTS_INTEGRATION_MANAGER_PULLER_ADDRESS: 'freetakserver'
        FTS_INTEGRATION_MANAGER_PUBLISHER_PORT: 19034
        FTS_INTEGRATION_MANAGER_PUBLISHER_ADDRESS: 'freetakserver'
        FTS_OPTIMIZE_API: True
        FTS_DATA_RECEPTION_BUFFER: 1024
        FTS_MAX_RECEPTION_TIME: 4
        FTS_NUM_ROUTING_WORKERS: 3
        FTS_COT_TO_DB: True
        FTS_MAINLOOP_DELAY: 100
        FTS_EMERGENCY_RADIUS: 0
        FTS_LOG_LEVEL: "info"

  freetakserver-ui:
    image: ghcr.io/freetakteam/ui:latest
    hostname: freetakserver-ui
    networks:
        - taknet
    ports:
      - 5000:5000
    volumes:
      - free-tak-ui-db:/home/freetak/:rw
    environment:
      FTS_IP: 'freetakserver'
      FTS_API_PORT: 19023
      FTS_API_PROTO: 'http'
      FTS_UI_EXPOSED_IP: 'freetakserver-ui'
      FTS_MAP_EXPOSED_IP: '127.0.0.1'
      FTS_MAP_PORT: 8000
      FTS_MAP_PROTO: 'http'
      FTS_UI_PORT: 5000
      FTS_UI_WSKEY: 'YourWebsocketKey'
      FTS_API_KEY: 'Bearer token'
      FTS_UI_SQLALCHEMY_DATABASE_URI: 'sqlite:////home/freetak/FTSServer-UI.db'

volumes:
  free-tak-core-db:
  free-tak-ui-db:

networks:
    taknet:
        driver: bridge
```
