# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: tenant-config.spec.ts >> TenantConfig convierte porcentajeIva a number antes de actualizar
- Location: e2e/tenant-config.spec.ts:3:5

# Error details

```
Test timeout of 20000ms exceeded.
```

# Page snapshot

```yaml
- generic [ref=e3]:
  - generic [ref=e4]:
    - heading "Configuración Regional y Fiscal del Tenant" [level=3] [ref=e5]
    - paragraph [ref=e6]: Gestiona los parámetros fiscales de facturación e impuestos (IVA) y localización del negocio.
    - generic [ref=e7]: "Tenant ID: tenant-a"
  - note [ref=e8]: "Modo lectura: Tu usuario actual no tiene rol de Administrador. Puedes consultar los parámetros, pero para guardar modificaciones se requiere un usuario con rol ADMIN."
  - generic [ref=e9]:
    - generic [ref=e10]:
      - generic [ref=e11]:
        - generic [ref=e12]: País (código ISO)
        - textbox "País (código ISO)" [disabled] [ref=e13]:
          - /placeholder: CL
          - text: CL
        - text: "Ej: CL, AR, US"
      - generic [ref=e14]:
        - generic [ref=e15]: Moneda Base (ISO)
        - textbox "Moneda Base (ISO)" [disabled] [ref=e16]:
          - /placeholder: CLP
          - text: CLP
        - text: "Ej: CLP, ARS, USD"
    - generic [ref=e17]:
      - generic [ref=e18]:
        - generic [ref=e19]: Idioma
        - textbox "Idioma" [disabled] [ref=e20]:
          - /placeholder: es
          - text: es
        - text: "Ej: es, en"
      - generic [ref=e21]:
        - generic [ref=e22]: Porcentaje IVA (%)
        - spinbutton "Porcentaje IVA (%)" [disabled] [ref=e23]: "19"
        - text: "Ej: 19 para 19%"
    - generic [ref=e24]:
      - generic [ref=e25]: Zona Horaria (IANA)
      - textbox "Zona Horaria (IANA)" [disabled] [ref=e26]:
        - /placeholder: America/Santiago
        - text: America/Santiago
      - text: "Identificador IANA oficial (ej: America/Santiago, America/Argentina/Buenos_Aires)"
    - generic [ref=e27]:
      - button "Recargar" [ref=e28] [cursor=pointer]
      - button "Solo Administradores" [disabled] [ref=e29]
```