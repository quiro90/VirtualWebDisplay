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

- Ubicación: `%USERPROFILE%\.virtualwebdisplay\localhost.pfx`
- Algoritmo: **RSA 2048 bits** · validez **10 años**.
- **Subject Alternative Names (SANs)**: `localhost`, IP local, `127.0.0.1` (requerido por navegadores modernos).
- Kestrel: HTTP = `Port`, HTTPS = `Port + 1`.

## Instalación manual

1. Navegar a `https://localhost:<port+1>/cert` → guardar `localhost.cer`.
2. Doble click → **Install Certificate**.
3. **Store Location**: Local Machine.
4. **Certificate Store**: Trusted Root Certification Authorities.
5. Reiniciar navegador.

## Regeneración

Eliminar `localhost.pfx` y reiniciar la app (genera uno nuevo).

## Endpoint

`GET /cert` → descarga `.cer`.

## Enlaces

- [[Endpoints HTTP]]
- [[WebRTC (H.264)]]
- [[Configuración de Usuario]]