# GlobalMart OS

## Requisitos
- **Windows:** instalar [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Linux:** instalar [Docker Engine](https://docs.docker.com/engine/install/)
- **Ambos:** instalar [Node.js 22+](https://nodejs.org/) (solo para correr la app)

---

## Setup (solo la primera vez)

Desde la carpeta `Docker/`:
```bash
docker compose build frontend
docker compose up -d frontend
docker compose run --rm frontend npm install
```

## Correr la app

Desde la carpeta `globalmart-frontend/`:
```bash
npm start
```

## Instalar nuevos paquetes

Desde la carpeta `Docker/`:
```bash
docker compose run --rm frontend npm install <paquete>
```
