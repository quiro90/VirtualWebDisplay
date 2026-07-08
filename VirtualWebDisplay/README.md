---
tags: [vault, meta, moc]
aliases: [Documentación Vault, Obsidian Vault]
updated: 2026-07-08
---

# 🗂️ Vault de Documentación — VirtualWebDisplay

Este `vault/` es la documentación del proyecto **reorganizada en notas atómicas** compatible con [Obsidian](https://obsidian.md). Está pensada para ser consumida tanto por **humanos** como por **IA**, con notas cortas, enfocadas y densamente enlazadas.

> [!tip] Cómo abrirlo en Obsidian
> 1. Abre Obsidian → **Open folder as vault**.
> 2. Selecciona la carpeta `vault/` de este repositorio.
> 3. Empieza por [[00 - Inicio (MOC)]] o por [[Índice para IA]].

## Principios de esta documentación

- **Notas atómicas**: cada archivo explica un solo concepto/componente/flujo.
- **Enlaces `[[wikilink]]`**: las relaciones entre conceptos son navegables.
- **Frontmatter YAML**: metadatos (`tags`, `aliases`, `type`, `updated`) para filtrado y búsqueda.
- **Callouts de Obsidian** (`> [!info]`, `> [!warning]`, `> [!danger]`) para destacar contexto.
- **Diagramas Mermaid**: soportados nativamente por Obsidian.
- **Fuente única de verdad**: el código fuente. Esta documentación **describe** el estado actual; no lo sustituye.

## Estructura de carpetas

| Carpeta | Contenido |
|---|---|
| *(raíz)* | Inicio (MOC), visión general, stack, índice para IA |
| `Arquitectura/` | Capas, diagramas, gestores de estado y ciclo de vida |
| `Componentes/` | Notas por clase/servicio clave |
| `Web API/` | Endpoints HTTP y modos de transmisión |
| `Configuración/` | Persistencia, modelos, perfiles, placement |
| `Seguridad/` | Auth, rate limiting, viewers, SSL |
| `Touch Input/` | Entrada táctil, gestos, script JS |
| `Cliente Web/` | wwwroot, módulos JS, templates HTML |
| `Flujos/` | Recorridos de ejecución (arranque, captura, runtime) |
| `Desarrollo/` | Build, AOT, ESLint, testing, convenciones |
| `Troubleshooting/` | Problemas comunes y soluciones |

## Relación con `docs/` existente

La carpeta `docs/` original (y `AGENT.md`, `README*.md`, `NATIVE_AOT_BUILD.md`, `TAREASPENDINETES.md`) **no fue modificada ni eliminada**. Este vault es una **reorganización paralela** que consolida y fragmenta ese contenido. Cuando haya discrepancia, el código fuente y `docs/` son la referencia original.

- [[00 - Inicio (MOC)]]
- [[Índice para IA]]