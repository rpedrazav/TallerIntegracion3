FROM node:20-bullseye-slim

# Dependencias del sistema para compilar módulos nativos de Electron
# (no para abrir la ventana — eso se hace desde el host)
RUN apt-get update && apt-get install -y \
    python3 make g++ \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copiamos primero solo el manifest para aprovechar la cache de Docker:
# si package.json no cambia, no vuelve a correr npm install en cada build
COPY package*.json ./
RUN npm install

# El resto del código
COPY . .

# El contenedor se queda vivo para recibir comandos arbitrarios:
#   docker compose run --rm frontend npm install <paquete>
#   docker compose run --rm frontend npm run lint
# La ventana gráfica (npm start) se corre desde el HOST, no desde aquí
CMD ["sleep", "infinity"]

 