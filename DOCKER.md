# GlobalMart OS

## Requisitos
- **Windows:** instalar [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- **Linux:** instalar [Docker Engine](https://docs.docker.com/engine/install/)
- **Ambos:** instalar [Node.js 22+](https://nodejs.org/) (solo para correr la app)

---

## Setup (solo la primera vez)

**1.** Construir la imagen — desde `Docker/`:
```bash
docker compose build frontend
docker compose up -d frontend
```

**2.** Instalar dependencias — desde `globalmart-frontend/`:
```bash
npm ci --ignore-scripts
node node_modules/electron/install.js
```

> `npm ci` verifica la integridad de cada paquete con los hashes del `package-lock.json`.
> `--ignore-scripts` bloquea scripts de instalacion de paquetes (mas seguro que `npm install`).
> El segundo comando descarga el binario de Electron para tu sistema operativo.

---

## Correr la app

Desde `globalmart-frontend/`:
```bash
npm start
```

---

## Instalar nuevos paquetes

Desde `Docker/` (nunca usar npm install directo en el host):
```bash
docker compose run --rm frontend npm install <paquete>
```

Luego, desde `globalmart-frontend/`, sincronizar el host:
```bash
npm ci --ignore-scripts
```
