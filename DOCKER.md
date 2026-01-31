# Docker Setup

This project includes Docker Compose configuration for running the entire Temporal Dashboard stack.

## Prerequisites

- Docker Desktop or Docker Engine with Docker Compose
- At least 2GB of available disk space

## Quick Start

1. **Build and start all services**:
   ```bash
   docker-compose up --build
   ```

2. **Start in detached mode** (runs in background):
   ```bash
   docker-compose up -d --build
   ```

3. **View logs**:
   ```bash
   docker-compose logs -f
   ```

4. **Stop all services**:
   ```bash
   docker-compose down
   ```

5. **Stop and remove volumes** (clears uploaded files):
   ```bash
   docker-compose down -v
   ```

## Services

### API Service
- **Container**: `temporal-dashboard-api`
- **Ports**: 
  - `8001` → API HTTP (8080)
  - `8002` → API HTTPS (8081)
- **URL**: http://localhost:8001
- **Volume**: `uploads-data` (persistent storage for uploaded DLLs)

### Web Service
- **Container**: `temporal-dashboard-web`
- **Ports**:
  - `8000` → Web HTTP (8080)
  - `8003` → Web HTTPS (8081)
- **URL**: http://localhost:8000
- **Depends on**: API service

## Network

Both services run on a custom bridge network (`temporal-dashboard-network`) allowing them to communicate using service names:
- Web service connects to API at: `http://api:8080`

## Volumes

- **uploads-data**: Persistent volume for uploaded DLL files
  - Data persists even when containers are stopped
  - To clear: `docker-compose down -v`

## Development

For local development with hot-reload, you can still run the services directly:

```bash
# Terminal 1 - API
cd src/TemporalDashboard.Api
dotnet run

# Terminal 2 - Web
cd src/TemporalDashboard.Web
dotnet run
```

## Building Individual Images

You can build individual Docker images:

```bash
# Build API image
docker build -f src/TemporalDashboard.Api/Dockerfile -t temporal-dashboard-api:latest ./src

# Build Web image
docker build -f src/TemporalDashboard.Web/Dockerfile -t temporal-dashboard-web:latest ./src
```

## Troubleshooting

### Port Already in Use
If ports 8000, 8001, 8002, or 8003 are already in use, modify the port mappings in `docker-compose.yml`:
```yaml
ports:
  - "9000:8080"  # Change 8000 to 9000
```

### View Container Logs
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api
docker-compose logs web

# Follow logs
docker-compose logs -f api
```

### Access Container Shell
```bash
# API container
docker exec -it temporal-dashboard-api /bin/bash

# Web container
docker exec -it temporal-dashboard-web /bin/bash
```

### Check Service Status
```bash
docker-compose ps
```

### Rebuild After Code Changes
```bash
docker-compose up --build
```

## Production Considerations

For production deployment, consider:

1. **Environment Variables**: Use `.env` file or Docker secrets
2. **HTTPS**: Configure proper SSL certificates
3. **Resource Limits**: Add memory/CPU limits in `docker-compose.yml`
4. **Health Checks**: Add health check endpoints
5. **Logging**: Configure centralized logging
6. **Backup**: Set up backups for the `uploads-data` volume

Example production overrides:
```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 512M
```
