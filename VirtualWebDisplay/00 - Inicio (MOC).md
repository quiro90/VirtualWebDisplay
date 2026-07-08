---
tags: [moc, vault, index]
aliases: [Map of Content, Índice, Inicio]
type: moc
updated: 2026-07-08
---

# 🗺️ Map of Content — VirtualWebDisplay

**VirtualWebDisplay** crea hasta **2 pantallas virtuales** en Windows (driver Parsec VDD) y las transmite por web local (Wi-Fi o USB tethering) usando **WebRTC** o **Web Image** (JPEG polling), con seguridad opcional, límite de viewers y entrada táctil remota.

> [!summary] Idea mental rápida
> App = `ASP.NET Core Minimal API` + `Windows Forms` (tray) + interop Win32.
> `Program.cs` bootstrappea y delega el ciclo en [[ApplicationLifecycleManager]].
> [[ServiceStateManager]] gestiona el estado (Stopped/Starting/Started/Stopping).
> Cada pantalla virtual = un [[ScreenRuntimeContext]].

## 👉 Por dónde empezar

- [[01 - Visión General]] — qué es y para qué sirve
- [[02 - Stack Tecnológico]] — tecnologías y dependencias
- [[Índice para IA]] — referencia rápida optimizada para asistentes

## 🏗️ Arquitectura

- [[Arquitectura por Capas]]
- [[Diagramas del Sistema]]
- [[ServiceStateManager]] ⭐ (estado centralizado)
- [[ScreenRuntimeContext]] (unidad por pantalla)
- [[ApplicationLifecycleManager]] (bucle de servicio)
- [[RuntimeStartupHelper]] (arranque de runtimes por pantalla)
- [[IDriverVerifier (Abstracción)]]

## 🧩 Componentes clave

- [[Program (Entry Point)]]
- [[VirtualDisplayManager]] — crear/destruir monitor virtual
- [[DxgiCaptureService]] — captura DXGI/GDI
- [[H264EncoderService]] — codificación H.264
- [[WebRtcStreamService]] — WebRTC VideoTrack
- [[InputHandler (Touch)]] — touch remoto
- [[VirtualDisplayTrayController]] — UI de bandeja
- [[VirtualScreenSettingsStore]] — persistencia JSON
- [[UpdateCheckService]] — check de versiones GitHub

## 🌐 Web API y transmisión

- [[Endpoints HTTP]]
- [[Modos de Transmisión]]
- [[WebImage (JPEG Polling)]]
- [[WebRTC (H.264)]]
- [[Resolución de Runtime por Puerto]]

## ⚙️ Configuración

- [[Configuración de Usuario]]
- [[VirtualScreenConfig (Campos)]]
- [[Perfiles de Resolución]]
- [[Placement y Posición]]
- [[Resoluciones Personalizadas VDD]]

## 🔒 Seguridad

- [[Seguridad por Pantalla]]
- [[Rate Limiting y Brute Force]]
- [[Límite de Viewers]]
- [[Certificado SSL (HTTPS)]]

## 👆 Touch y cliente web

- [[Entrada Táctil]] · [[Gestos Táctiles]] · [[touch-input.js]]
- [[Cliente Web (wwwroot)]] · [[Módulos JavaScript]] · [[HTML Templates]]

## 🔄 Flujos

- [[Arranque del Sistema]]
- [[Flujo de Captura y Streaming]]
- [[Creación de Pantalla Virtual]]
- [[Cambio de Configuración en Runtime]]

## 🛠️ Desarrollo y ops

- [[Guía de Desarrollo]] · [[Build y Compilación]] · [[Native AOT]]
- [[ESLint y Versionado]] · [[Testing]] · [[Convenciones de Código]]

## 🐛 Troubleshooting

- [[Guía de Troubleshooting]] · [[Problemas de Instalación y Driver]]
- [[Problemas de Red y Firewall]] · [[Problemas de WebRTC y SSL]]