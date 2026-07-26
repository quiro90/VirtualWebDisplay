---
tags: [troubleshooting, red, firewall, puertos]
aliases: [Problemas de Red y Firewall, Network Issues, Port Issues]
type: guia
updated: 2026-07-08
---

# Problemas de Red y Firewall

## Síntomas

- Navegador no carga `http://host:port`.
- Timeout al conectar desde otro dispositivo.
- WebRTC conecta local pero no remoto.

## Causas y soluciones

### Firewall bloquea puertos

- Abrir `HttpPort` (TCP) en Windows Firewall.
- Abrir `HttpPort+1` (HTTPS, ver [[Certificado SSL (HTTPS)]]).
- WebRTC usa puertos UDP dinámicos → abrir rango UDP o usar TURN (no soportado nativamente).

### Binding incorrecto

- Por defecto Kestrel escucha en `http://0.0.0.0:{HttpPort}`.
- Verificar `VirtualScreenConfig` → `HttpPort` (legacy name, sigue funcionando).
- Ver [[Endpoints HTTP]] y [[Resolución de Runtime por Puerto]].

### Acceso desde iPad / móvil

- Usar IP LAN del host, no `localhost`.
- HTTPS con certificado self-signed → aceptar certificado en el dispositivo (ver [[Certificado SSL (HTTPS)]]).

### Keepalive

- `keepalive.js` mantiene sesión ([[Módulos JavaScript]]). Si corta → revisar timeout de red.

## Enlaces

- [[Certificado SSL (HTTPS)]]
- [[Endpoints HTTP]]
- [[Resolución de Runtime por Puerto]]
- [[Guía de Troubleshooting]]

## Continuar con
- [[Endpoints HTTP]]
- [[Certificado SSL (HTTPS)]]
