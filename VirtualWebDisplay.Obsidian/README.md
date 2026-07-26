---
tags: [vault, meta, moc]
aliases: [Documentación Vault, Obsidian Vault]
updated: 2026-07-26
---

# 🗂️ Vault de Documentación — VirtualWebDisplay

Este `vault/` es la documentación del proyecto **reorganizada en notas atómicas** compatible con [Obsidian](https://obsidian.md). Está pensada para ser consumida tanto por **humanos** como por **IA**, con notas cortas, enfocadas y densamente enlazadas.

> [!tip] Cómo abrirlo en Obsidian
> 1. Abre Obsidian → **Open folder as vault**.
> 2. Selecciona la carpeta `VirtualWebDisplay.Obsidian/` de este repositorio.
> 3. Empieza por [[00 - Inicio (MOC)]] — índice único con visión general de todos los temas.

## Principios de esta documentación

- **Notas atómicas**: cada archivo explica un solo concepto/componente/flujo.
- **Enlaces `[[wikilink]]`**: las relaciones entre conceptos son navegables.
- **Frontmatter YAML**: metadatos (`tags`, `aliases`, `type`, `updated`) para filtrado y búsqueda.
- **Callouts de Obsidian** (`> [!info]`, `> [!warning]`, `> [!danger]`) para destacar contexto.
- **Diagramas Mermaid**: soportados nativamente por Obsidian.
- **Fuente única de verdad**: el código fuente. Esta documentación **describe** el estado actual; no lo sustituye.

## Estructura de carpetas

Las carpetas agrupan temas por afinidad. El orden en el índice (`00 - Inicio (MOC)`) refleja relevancia: arquitectura y lógica central primero, detalles y troubleshooting al fondo. No imponen un orden de lectura obligatorio.

| Carpeta | Contenido |
|---|---|
| *(raíz)* | Índice (MOC), visión general, stack |
| `Arquitectura/` | Capas, diagramas, gestores de estado y ciclo de vida |
| `Componentes/` | Notas por clase/servicio clave |
| `Web API/` | Endpoints HTTP y modos de transmisión |
| `Flujos/` | Recorridos de ejecución (arranque, captura, runtime) |
| `Configuración/` | Persistencia, modelos, perfiles, placement |
| `Seguridad/` | Auth, rate limiting, viewers, SSL |
| `Touch Input/` | Entrada táctil, gestos, script JS |
| `Cliente Web/` | wwwroot, módulos JS, templates HTML |
| `Desarrollo/` | Build, AOT, ESLint, testing, convenciones |
| `Troubleshooting/` | Problemas comunes y soluciones |

## Fuente de verdad

El **código fuente** es la referencia original; este vault lo describe en notas atómicas. Cuando haya discrepancia, gana el código. Para planificación, tareas y trabajo pendiente, usar **OpenSpec** (no el vault).

- [[00 - Inicio (MOC)]]