---
tags: [overview, vision]
aliases: [Visión General, Qué es VirtualWebDisplay]
type: overview
updated: 2026-07-08
---

# 01 — Visión General

## ¿Qué es?

**VirtualWebDisplay** es una aplicación Windows (.NET 10) que crea **pantallas virtuales** usando el driver **Parsec VDD** y las retransmite por **HTTP/HTTPS** local usando **WebRTC** o **Web Image** (JPEG polling). Permite usar tablets, e-readers o teléfonos como monitores adicionales.

## Objetivo del sistema

> [!important] Suposición operativa
> El programa **no busca "streaming multimedia genérico"**, sino **usar navegadores de dispositivos secundarios como extensión de pantallas virtuales Windows**. Ese objetivo guía cualquier cambio futuro.

## Flujo base

1. Crear 1–2 monitores virtuales en Windows con [[VirtualDisplayManager|Parsec VDD]].
2. Capturar su contenido con [[DxgiCaptureService]].
3. Retransmitir por HTTP local (ver [[Modos de Transmisión]]).
4. Abrir el contenido desde tablets/e-readers en la red local (Wi-Fi o USB tethering).

## Características principales

- ✨ Hasta **2 pantallas virtuales** simultáneas con config independiente.
- 🚀 **WebRTC** ultra-baja latencia (~30–50ms) **o** **JPEG polling** compatible con cualquier navegador.
- ⚙️ Resolución configurable (420p → 5K), intervalo de captura (1–300ms), calidad JPEG (1–100).
- 🖐️ **Entrada táctil** remota con gestos (tap, hold-to-drag, scroll, pinch-zoom) — ver [[Entrada Táctil]].
- 🔐 Seguridad por pantalla (clave de 6 caracteres) + rate limiting + límite de viewers.
- 🌐 Acceso remoto en red local con detección automática de IP.
- 💾 Configuración persistente en `%USERPROFILE%\.virtualwebdisplay\`.

## Modos de uso típicos

1. **Monitor extra**: iPad/Android/Kindle como segunda pantalla táctil.
2. **Stream de una app**: mover una ventana al display virtual (`Win+Shift+→`) y verla en el navegador.
3. **Dashboard/monitoring**: modo Web Image con intervalo alto para contenido estático.

## Enlaces clave

- [[02 - Stack Tecnológico]]
- [[Arquitectura por Capas]]
- [[Modos de Transmisión]]
- [[Endpoints HTTP]]