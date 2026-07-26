---
tags: [troubleshooting, guia]
aliases: [Guía de Troubleshooting, Troubleshooting Guide, Diagnóstico]
type: guia
updated: 2026-07-08
---

# Guía de Troubleshooting

Punto de partida para diagnóstico. Deriva a notas específicas según el síntoma.

## Por síntoma

| Síntoma | Ver |
|---|---|
| No instala / no detecta VDD | [[Problemas de Instalación y Driver]] |
| No conecta / timeout de red | [[Problemas de Red y Firewall]] |
| Pantalla negra / no video | [[Flujo de Captura y Streaming]], [[DxgiCaptureService]] |
| WebRTC no inicia / SDP falla | [[Problemas de WebRTC y SSL]], [[WebRtcStreamService]] |
| Certificado SSL no confiable | [[Certificado SSL (HTTPS)]], [[Problemas de WebRTC y SSL]] |
| Touch no funciona | [[Entrada Táctil]], [[InputHandler (Touch)]] |
| 403 / 401 en endpoints | [[Seguridad por Pantalla]], [[Rate Limiting y Brute Force]] |
| 429 Too Many Requests | [[Rate Limiting y Brute Force]] |
| Viewer rechazado | [[Límite de Viewers]] |

## Logs

- Consola del tray / `dotnet run` output.
- `window.Logger` en cliente (ver [[Módulos JavaScript]]).

## Fuente

`docs/TROUBLESHOOTING.md` — troubleshooting original.

## Enlaces

- [[Problemas de Instalación y Driver]]
- [[Problemas de Red y Firewall]]
- [[Problemas de WebRTC y SSL]]

## Continuar con
- [[Problemas de Instalación y Driver]]
- [[Problemas de Red y Firewall]]
- [[Problemas de WebRTC y SSL]]
