---
name: simplificar-flujo
description: Simplifica código existente de GlobalMart OS sin cambiar su comportamiento. Úsala al tocar o revisar un flujo (login, caja, tarjeta, anulación, Admin) para quitar vueltas, código muerto y duplicados, o al hacer la pasada final de simplificación.
---

# Simplificar un flujo

Objetivo: que un flujo se pueda explicar en voz alta desde el botón hasta la base de datos en unos 5 saltos, sin pasar por archivos que no aportan. El comportamiento observable no cambia.

## 1. Recorre el flujo

Empieza en el botón y sigue la cadena real, anotando archivo y función en cada salto:

1. Página o componente `.tsx` que tiene el botón.
2. Handler que se ejecuta al hacer clic.
3. Función de `globalmart-frontend/src/api/client.ts` que hace la llamada.
4. URL resultante (`config/api.ts`, modo `remote` o `local`).
5. Controller del microservicio que recibe la ruta.
6. Service con la lógica.
7. Repository o `DbContext` que lee o escribe.

Cada salto que solo reenvía, transforma sin necesidad o elige entre caminos que ya no se usan es una **vuelta**.

## 2. Qué eliminar

- **Código muerto:** métodos, rutas, ramas, props y estados sin uso. Confírmalo con grep antes de borrar.
- **Duplicados:** dos repositorios, dos adaptadores o dos funciones que hacen lo mismo. Deja uno y migra las llamadas.
- **Configuración sobrante:** modos, overrides o normalizaciones que corrigen problemas que ya no existen.
- **Alias de rutas** que ningún cliente usa (verifica frontend, ingress y pruebas).
- **Componentes gigantes:** parte en piezas con una sola responsabilidad (`Pos.tsx`, `CerrarTurnoPage.tsx`, `ProductosPage.tsx`). Mueve estilos inline a CSS con los tokens de la guía de estilo.
- **Archivos sueltos** (`fix*.js`, `test*.json`, scripts temporales): solo si la tarea lo indica; la limpieza general es del scrum master.

## 3. Qué no tocar

- La arquitectura: microservicios, Kong, Kafka/outbox, base de datos por servicio y capas Controller → Service → Repository.
- Interfaces que usan las pruebas con Moq, salvo que también actualices esas pruebas.
- Validaciones, manejo de errores, aislamiento por tenant (RN-01), pasarela (RN-02), turno abierto (RN-06) y JWT (RN-07).
- Contratos HTTP que consume el frontend o el despliegue, salvo que la tarea lo pida y actualices ambos lados.

## 4. Verifica

1. Antes de cambiar, ejecuta las pruebas del área (`dotnet test` del servicio, `npm run typecheck`, E2E relacionados) y anota fallos previos.
2. Después de cambiar, repítelas: deben dar el mismo resultado.
3. Cuenta los saltos del flujo antes y después. Menciónalo en el commit o en la tarea, por ejemplo: «cierre de turno: 9 → 6 saltos».
4. Si cambió un endpoint, evento o regla, actualiza el grafo con la skill `grafo-contexto`.
