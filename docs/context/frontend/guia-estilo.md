---
id: guia-estilo
tipo: frontend
titulo: Guía de Estilo Visual — GlobalMart OS
estado: aprobada
fuentes: [docs/GlobalMart OS — Guía de estilo.html]
verificado_contra_codigo: false
ultima_revision: 2026-10-06
depende_de: [estructura, pantallas, 005-electron-frontend]
publica: []
consume: []
reglas: [RNF-06]
---
# Guía de Estilo Visual — GlobalMart OS

> Sistema visual aprobado para la aplicación de escritorio POS. La guía HTML es la referencia normativa para nuevas pantallas y componentes del frontend.

## Dirección visual

- Base cálida de lino, crema y tinta, con **pimentón** como color de marca.
- Tipografía sans para la interfaz (`Inter`) y monoespaciada para códigos, folios y tickets (`IBM Plex Mono`).
- Interfaz sobria para turnos largos: sin animaciones distractoras; las alertas deben ser claras y accionables.
- La guía incluye modo claro y modo oscuro. El botón de tema es parte del prototipo visual.

## Tokens aprobados

Los tokens completos y sus valores están en `docs/GlobalMart OS — Guía de estilo.html`, dentro de la sección **Tokens CSS**.

- Tipografía: `--font-sans`, `--font-mono`.
- Escala de texto: `--text-total`, `--text-price`, `--text-title-xl`, `--text-title`, `--text-body`, `--text-body-sm`, `--text-button`, `--text-label`, `--text-caption`, `--text-code`, `--text-ticket`.
- Color base: `--color-bg`, `--color-surface`, `--color-ink`, `--color-ink-soft`, `--color-line`.
- Marca y estados: `--color-primary`, `--color-secondary`, `--color-success`, `--color-warning`, `--color-danger`, `--color-info`, junto con sus tokens de texto y contraste.
- Personalización por tenant: solo se pueden sobrescribir `--color-primary` y `--color-on-primary`.

## Reglas de interacción y accesibilidad

- Pimentón: marca, navegación y acciones primarias.
- Verde salvia: cobrar, confirmar y estados exitosos.
- Ladrillo: anular, errores y vencimientos próximos.
- Mostaza: advertencias; el texto usa el token de contraste oscuro correspondiente.
- Toda alerta debe incluir ícono, título y detalle; nunca comunicar información solo mediante color.
- Precios, cantidades y totales usan `tabular-nums`; los códigos usan tipografía monoespaciada.
- El texto que el cajero necesita para cobrar no baja de 14 px; el mínimo absoluto es 12 px.
- Los estados deben respetar al menos 4.5:1 de contraste sobre su fondo tintado.
- Las fuentes deben empaquetarse en Electron para que el POS funcione sin internet.

## Patrón POS de referencia

La guía muestra una venta en curso con lectura de código de barras, productos por unidad y peso variable, lote y vencimiento, total destacado, acción **Cobrar** y recibo con IVA y folio. Es una referencia de composición y no constituye integración funcional ni datos de producción.

## Aplicación

Esta guía está aprobada como contexto de diseño. Su adopción en cada pantalla debe hacerse al implementar o modificar componentes, sin cambiar por ello el estado funcional documentado en [[pantallas]].

## Conexiones

- Estructura: [[estructura]]
- Pantallas: [[pantallas]]
- Decisión de frontend: [[005-electron-frontend]]

## Fuente

- `docs/GlobalMart OS — Guía de estilo.html`
