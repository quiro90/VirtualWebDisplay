# Checklist de Backlog Tecnico

Estado general: **Fase 0-2 completadas ✅ | Fase 3-6 pendientes**
Última actualización: 2026-04-26 (Smoke test Fase 2 exitoso)

## Objetivo
Mejorar estructura y organizacion sin cambiar la logica funcional del sistema.

## Fase 0 - Diagnostico
- [x] Revisar arquitectura y documentacion existente.
- [x] Identificar puntos de concentracion de responsabilidades.
- [x] Detectar duplicidades y residuos de organizacion.

## Fase 1 - Higiene Estructural (bajo riesgo)
- [x] Limpiar using duplicados y ordenarlos por archivo.
- [x] Corregir textos con codificacion dañada en mensajes de usuario.
- [x] Verificar compilacion sin regresiones funcionales.

## Fase 2 - Reorganizacion de Program.cs (sin cambiar logica)
- [x] Extraer templates de seguridad/limite a UI/HtmlTemplates.
- [x] Extraer helpers de acceso/autorizacion a Infrastructure.
- [x] Extraer helpers de limpieza de runtimes a Infrastructure.
- [x] Reducir el tamaño de Program.cs manteniendo mismos endpoints y flujo.
- [x] Verificar compilacion y arranque.

## Fase 3 - Orden de Capa Web
- [ ] Definir estrategia de mapeo de endpoints por modulos (Auth, Stream, Config).
- [ ] Reutilizar carpeta Controllers o crear carpeta Endpoints consistente.
- [ ] Mantener rutas y contratos HTTP existentes.

## Fase 4 - Modularizacion de UI Forms
- [ ] Separar ResolutionConfigurationForm en partials por responsabilidad.
- [ ] Separar ScreenTabControls en partials por responsabilidad.
- [ ] Mantener localizacion y tema sin cambios funcionales.

## Fase 5 - Configuracion y Copias
- [ ] Reducir duplicacion entre Clone y CopyTo de VirtualScreenConfig.
- [ ] Agregar prueba de cobertura de propiedades copiada.

## Fase 6 - Hardening Tecnico
- [ ] Migrar carga de certificados a API recomendada (evitar warning SYSLIB0057).
- [ ] Revisar puntos de polling/sleeps para encapsulado y documentacion.

## Criterios de aceptacion por fase
- [ ] Sin cambios en comportamiento observable para usuario final.
- [x] Compilacion exitosa.
- [x] Sin errores nuevos en la solucion.
- [x] Diff legible, enfocado y facil de revisar.
