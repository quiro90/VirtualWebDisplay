---
tags: [seguridad, ssl, https, certificado]
aliases: [Certificado SSL, HTTPS, LocalCertificateProvider, Self-signed]
type: referencia
updated: 2026-07-08
---

# Certificado SSL (HTTPS)

**Archivo**: `Web/Hosting/LocalCertificateProvider.cs` (generación/obtención).

> [!warning] Experimental / WIP
> Generación de certificado **autofirmado**. Puede requerir setup manual o disparar warnings de seguridad del navegador.

## Generación

- Ubicación: `%USERPROFILE%\.virtualwebdisplay\localca.pfx` (PKCS#12 con clave) + `localca.crt` (cert público).
- Algoritmo: **RSA 2048 bits** · validez **400 días** (límite iOS/Safari amigable).
- **CA**: cert marcado como `certificateAuthority: true` (es una CA local; los navegadores lo instalan en *Trusted Root*).
- **Subject Alternative Names (SANs)**: `localhost`, `hostName` (nombre del equipo), `127.0.0.1` (IPv4 loopback), `::1` (IPv6 loopback), IP local detectada.
- Kestrel: HTTP = `Port`, HTTPS = `Port + 1`.

## Instalación manual

1. Navegar a `https://localhost:<port+1>/cert` → guardar `localca.crt`.
2. Doble click → **Install Certificate**.
3. **Store Location**: Local Machine.
4. **Certificate Store**: Trusted Root Certification Authorities.
5. Reiniciar navegador.

## Regeneración

Eliminar `localca.pfx` (y `localca.crt`) y reiniciar la app (genera uno nuevo).

## Endpoint

`GET /cert` → descarga `localca.crt` (PEM/DER público).

## Enlaces

- [[Endpoints HTTP]]
- [[WebRTC (H.264)]]
- [[Configuración de Usuario]]

## Continuar con
- [[KestrelConfigurator]]
- [[Endpoints HTTP]]
