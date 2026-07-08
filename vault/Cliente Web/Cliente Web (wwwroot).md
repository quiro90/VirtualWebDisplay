---
tags: [cliente, web, wwwroot]
aliases: [Cliente Web, wwwroot, Static Files]
type: referencia
updated: 2026-07-08
---

# Cliente Web (wwwroot)

**Carpeta**: `VirtualWebDisplay_Parsec/wwwroot/` — archivos estáticos servidos por Kestrel.

## Servir

`app.UseStaticFiles()` en [[ApplicationLifecycleManager]] sirve `wwwroot/` en la raíz (`/`). Ver [[Módulos JavaScript]].

```
wwwroot/
└── js/
    ├── common/
    │   ├── logger.js          (~140 líneas) logging 5 niveles
    │   └── keepalive.js       (~90 líneas)  keep-alive de sesión
    ├── touch/
    │   └── touch-input.js     (~580 líneas) entrada táctil
    ├── webimage/
    │   └── webimage-client.js (~160 líneas) cliente JPEG polling
    └── webrtc/
        └── webrtc-client.js   (~300 líneas) cliente WebRTC
```

## Cache busting

Los `<script>` se sirven con `?v={AppVersion}` (de `TemplateVersionHelper` / `.csproj` `Version=1.0.4`). Ver [[ESLint y Versionado]].

## Logger configurable

`logger.js`: 5 niveles (SILENT/ERROR/WARN/INFO/DEBUG). `DEFAULT_LEVEL = localhost ? 4 : 2`.

## Enlaces

- [[Módulos JavaScript]]
- [[HTML Templates]]
- [[ESLint y Versionado]]