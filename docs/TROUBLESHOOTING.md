# 🐛 Troubleshooting - VirtualWebDisplay

## Tabla de Contenidos

1. [Problemas de Instalación](#problemas-de-instalación)
2. [Problemas de Ejecución](#problemas-de-ejecución)
3. [Problemas de Pantalla Virtual](#problemas-de-pantalla-virtual)
4. [Problemas de Red y Conectividad](#problemas-de-red-y-conectividad)
5. [Problemas de WebRTC](#problemas-de-webrtc)
6. [Problemas de Rendimiento](#problemas-de-rendimiento)
7. [Problemas de Configuración](#problemas-de-configuración)
8. [Errores Comunes y Soluciones](#errores-comunes-y-soluciones)
9. [Recopilación de Logs](#recopilación-de-logs)
10. [FAQ](#faq)

---

## Problemas de Instalación

### Error: ".NET 10 Runtime not found"

**Síntoma**:
```
This application requires .NET 10.0 runtime.
Please install it from https://dotnet.microsoft.com/download
```

**Causa**: .NET 10 SDK/Runtime no está instalado.

**Solución**:

1. Descargar e instalar .NET 10 SDK:
   ```
   https://dotnet.microsoft.com/download/dotnet/10.0
   ```

2. Verificar instalación:
   ```powershell
   dotnet --version
   # Debe mostrar: 10.x.x
   ```

3. Reiniciar aplicación.

---

### Error: "Parsec VDD Driver Not Found"

**Síntoma**:
```
┌──────────────────────────────────────┐
│  Parsec VDD Driver Not Found         │
├──────────────────────────────────────┤
│  This application requires the       │
│  Parsec Virtual Display Driver.      │
│                                      │
│  [Download Driver]  [Cancel]         │
└──────────────────────────────────────┘
```

**Causa**: Driver Parsec VDD no está instalado.

**Solución**:

1. Descargar Parsec VDD (instalador directo):
   ```
   https://builds.parsec.app/vdd/parsec-vdd-0.45.0.0.exe
   ```

2. Ejecutar el instalador **como Administrador**.

3. Verificar instalación en Device Manager:
   ```
   Display adapters > Parsec Virtual Display Adapter
   ```

4. Reiniciar aplicación.

**Alternativa (si el instalador falla)**:

```powershell
# Verificar si driver está presente pero no activo
Get-PnpDevice -Class Display | Where-Object {$_.FriendlyName -like "*Parsec*"}

# Si aparece como "disabled", habilitarlo:
Enable-PnpDevice -InstanceId "<InstanceId del dispositivo>"
```

---

### Error: "Access Denied" al Instalar Driver

**Síntoma**: `installdriver.bat` muestra "Access Denied" o falla silenciosamente.

**Causa**: Permisos insuficientes.

**Solución**:

1. Cerrar todos los procesos de VirtualWebDisplay.

2. Ejecutar PowerShell **como Administrador**:
   ```powershell
   cd "C:\Path\To\ParsecVDD"
   .\installdriver.bat
   ```

3. Si persiste, deshabilitar temporalmente Secure Boot en BIOS (solo en sistemas con firma de driver requerida).

4. Reiniciar Windows después de instalación exitosa.

---

## Problemas de Ejecución

### Error: "Another instance is already running"

**Síntoma**:
```
┌──────────────────────────────────────┐
│  Error                               │
├──────────────────────────────────────┤
│  Another instance of VirtualWeb      │
│  Display is already running.         │
│                                      │
│  [OK]                                │
└──────────────────────────────────────┘
```

**Causa**: Instancia previa no cerró correctamente (mutex no liberado).

**Solución 1 - Terminar Proceso**:

```powershell
# Buscar proceso
tasklist | findstr VirtualWebDisplay

# Terminar proceso
taskkill /F /IM VirtualWebDisplay.exe
```

**Solución 2 - Reiniciar Windows**:

Si persiste después de terminar proceso, reiniciar (libera todos los mutex).

**Prevención**:

- Cerrar aplicación correctamente: Tray Icon → Exit
- No forzar cierre con Task Manager (usa mutex que no se libera)

---

### Error: "Address already in use"

**Síntoma**:
```
Unhandled exception: System.IO.IOException: Address already in use: http://0.0.0.0:5000
```

**Causa**: Puerto HTTP configurado (por ejemplo 8000) está siendo usado por otra aplicación.

**Solución 1 - Identificar Proceso**:

```powershell
# Ver qué proceso usa puerto 8000
netstat -ano | findstr :8000

# Ejemplo output:
# TCP    0.0.0.0:8000    0.0.0.0:0    LISTENING    1234
#                                                   ^^^^
#                                                    PID

# Ver nombre del proceso
tasklist | findstr 1234

# Terminar proceso (si es seguro hacerlo)
taskkill /PID 1234 /F
```

**Solución 2 - Cambiar Puerto**:

Editar configuración:
```json
{
  "Screen1": {
      "Port": 7000  // Cambiar a puerto disponible
  }
}
```

**Puertos Comúnmente en Uso (evitar)**:
- 80, 443 (HTTP/HTTPS)
- 3000 (Node.js apps)
- 5000, 5001 (ASP.NET Core default)
- 8000, 8001 (valores comunes en esta app)
- 8080 (Tomcat, proxies)

**Puertos Alternativos Recomendados**:
- 7000-7999
- 9000-9999

---

### Aplicación Se Cierra Inmediatamente

**Síntoma**: Al ejecutar `.exe`, la aplicación se cierra sin mostrar mensaje de error.

**Causa Posible 1**: Excepción no capturada durante inicio.

**Diagnóstico**:

Ejecutar desde PowerShell para ver output:
```powershell
cd "C:\Path\To\VirtualWebDisplay"
.\VirtualWebDisplay.exe
```

Observar mensajes de error en consola.

**Causa Posible 2**: Archivo de configuración corrupto.

**Solución**:

1. Eliminar configuración:
   ```powershell
   del C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json
   ```

2. Reiniciar aplicación (creará configuración default).

---

## Problemas de Pantalla Virtual

### Pantalla Virtual No Aparece en Windows

**Síntoma**: Aplicación inicia correctamente, pero no se ve monitor extra en Display Settings.

**Diagnóstico**:

1. Verificar Device Manager:
   ```
   Win+X → Device Manager → Display adapters
   ```

   Debería aparecer:
   ```
   Display adapters
   ├── <GPU principal> (ej: NVIDIA GeForce RTX 3060)
   └── Parsec Virtual Display Adapter
   ```

2. Si aparece con `⚠️` (warning icon):
   - Click derecho → Properties → Ver código de error

**Solución Según Código de Error**:

**Code 10** (Device cannot start):
- Reinstalar driver Parsec VDD
- Verificar compatibilidad con Windows 10/11

**Code 12** (Not enough resources):
- Reducir resolución de pantalla virtual
- Deshabilitar otros monitores virtuales

**Code 43** (Device failed):
- Actualizar controladores de GPU principal
- Reinstalar driver Parsec VDD

**Si no aparece en Device Manager**:

```powershell
# Buscar manualmente dispositivos no reconocidos
pnputil /enum-devices /class Display
```

---

### Pantalla Virtual Parpadea o Desaparece

**Síntoma**: Monitor virtual aparece y desaparece intermitentemente.

**Causa**: Keep-alive loop de `VirtualDisplayManager` no está ejecutándose correctamente.

**Diagnóstico**:

El driver Parsec VDD requiere que se llame `Update()` cada 100ms para mantener la pantalla activa.

**Solución 1 - Verificar Código**:

En `VirtualDisplayManager.cs`, verificar que el loop de keep-alive esté ejecutándose:

```csharp
// Debe existir este loop
while (!_cts.Token.IsCancellationRequested)
{
    Update();  // Llamada cada 100ms
    await Task.Delay(100, _cts.Token);
}
```

**Solución 2 - Revisar Logs**:

Si el loop se está interrumpiendo, puede deberse a:
- Excepción no capturada en `Update()`
- Thread bloqueado por otra operación
- Cancellation token activado prematuramente

---

### Pantalla Virtual Muestra Contenido Incorrecto

**Síntoma**: La captura muestra escritorio principal u otro contenido en lugar de la pantalla virtual.

**Causa**: `DxgiCaptureService` está usando bounds incorrectos.

**Diagnóstico**:

Verificar que `VirtualDisplayManager` está reportando bounds correctos:

```csharp
var bounds = _displayManager.Bounds;
Console.WriteLine($"Bounds: X={bounds.X}, Y={bounds.Y}, Width={bounds.Width}, Height={bounds.Height}");
```

**Solución**:

1. Verificar en Display Settings que la pantalla virtual está en la posición configurada.

2. Mover una ventana a la pantalla virtual y verificar que la captura la muestra.

3. Si persiste, recrear pantalla virtual:
   - Tray Icon → Configuration → Apply

---

### Pantalla Virtual No Recuerda su Posición o Resolución

**Síntoma**: Al iniciar el servicio, la pantalla virtual no se ubica donde se dejó la última vez o su resolución vuelve a valores por defecto.

**Causa**: El archivo de estado de hardware (`virtualscreen.display.json`) está corrupto, desincronizado o no se tienen permisos de escritura sobre él.

**Solución**:

1. Detener el servicio desde el Tray Icon.
2. Eliminar el caché de estado del display:
   ```powershell
   del C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.display.json
   ```
3. Iniciar el servicio nuevamente y acomodar la pantalla en la configuración de Windows.

---

## Problemas de Red y Conectividad

### No Puedo Acceder desde Navegador (localhost)

**Síntoma**: Navegando a `https://localhost:5001` muestra "Unable to connect" o "This site can't be reached".

**Diagnóstico 1 - Verificar que Aplicación Está Ejecutándose**:

```powershell
# Verificar proceso
tasklist | findstr VirtualWebDisplay

# Verificar puertos listening
netstat -ano | findstr :5001
# Debe mostrar:
# TCP    0.0.0.0:5001    0.0.0.0:0    LISTENING    <PID>
```

Si no aparece:
- Aplicación no inició correctamente
- Puerto configurado es diferente (verificar `virtualscreen.user.json`)

**Diagnóstico 2 - Verificar Firewall**:

```powershell
# Ver reglas de firewall para puerto 5001
Get-NetFirewallRule | Where-Object {$_.LocalPort -eq 5001}
```

**Solución - Permitir en Firewall**:

```powershell
# Crear regla para puerto HTTPS (5001)
New-NetFirewallRule -DisplayName "VirtualWebDisplay HTTPS" `
                    -Direction Inbound `
                    -LocalPort 5001 `
                    -Protocol TCP `
                    -Action Allow

# Crear regla para puerto HTTP (5000)
New-NetFirewallRule -DisplayName "VirtualWebDisplay HTTP" `
                    -Direction Inbound `
                    -LocalPort 8000 `
                    -Protocol TCP `
                    -Action Allow
```

**Diagnóstico 3 - Verificar Certificado**:

Si navegador muestra "ERR_CERT_AUTHORITY_INVALID":
- Certificado SSL no está instalado o no es confiable
- Ver sección "Problemas de WebRTC" para solución

---

### No Puedo Acceder desde Otro Dispositivo en Red

**Síntoma**: Desde otro PC/tablet en red local, navegando a `https://192.168.1.XXX:5001` no carga.

**Diagnóstico 1 - Verificar IP**:

```powershell
# Obtener IP local
ipconfig | findstr IPv4
# Ejemplo: 192.168.1.100
```

Verificar que la IP usada en navegador es correcta.

**Diagnóstico 2 - Ping desde Dispositivo Remoto**:

En dispositivo remoto:
```
ping 192.168.1.100
```

Si no responde:
- Problema de red (router, switch, WiFi)
- Firewall bloqueando ICMP

**Diagnóstico 3 - Verificar Firewall en PC Host**:

Windows Firewall puede bloquear acceso externo por defecto.

**Solución**:

```powershell
# Permitir acceso desde red local
New-NetFirewallRule -DisplayName "VirtualWebDisplay HTTPS (Remote)" `
                    -Direction Inbound `
                    -LocalPort 5001 `
                    -Protocol TCP `
                    -Action Allow `
                    -Profile Private
```

**Diagnóstico 4 - Verificar Binding de Kestrel**:

En `Program.cs`, verificar que Kestrel está escuchando en `0.0.0.0` (todas las interfaces):

```csharp
options.Listen(IPAddress.Any, port);  // ✅ Correcto
// NO usar:
options.Listen(IPAddress.Loopback, port);  // ❌ Solo localhost
```

---

## Problemas de WebRTC

### Error: "NET::ERR_CERT_AUTHORITY_INVALID"

**Síntoma**: Chrome/Edge muestra advertencia de certificado SSL no confiable.

**Causa**: Certificado SSL autofirmado no está instalado en "Trusted Root Certification Authorities".

**Solución**:

1. Descargar certificado:
   ```
   https://localhost:5001/cert
   ```
   Guarda como: `localhost.cer`

2. Instalar certificado:
   - Doble click en `localhost.cer`
   - Click "Install Certificate..."
   - Store Location: **Local Machine** (requiere admin)
   - Certificate Store: **Trusted Root Certification Authorities**
   - Click "Finish"

3. Reiniciar navegador.

4. Verificar instalación:
   - Chrome: `chrome://settings/certificates` → "Authorities" → buscar "localhost"
   - Windows: `certmgr.msc` → "Trusted Root Certification Authorities" → "Certificates"

**Alternativa (solo para testing, NO recomendado)**:

En Chrome, click en "Advanced" → "Proceed to localhost (unsafe)".

---

### WebRTC No Conecta (Queda en "Connecting...")

**Síntoma**: Página web muestra "Connecting..." indefinidamente, nunca establece conexión WebRTC.

**Diagnóstico 1 - Ver Console de Navegador**:

Abrir DevTools (`F12`) → Console.

Buscar errores relacionados con:
- `RTCPeerConnection`
- `Failed to set remote description`
- `ICE candidate error`

**Error Común 1**: "DOMException: Failed to execute 'setRemoteDescription' on 'RTCPeerConnection'"

**Causa**: SDP offer/answer incompatible.

**Solución**:
- Verificar que navegador soporta WebRTC (Chrome 60+, Edge 79+, Firefox 68+)
- Actualizar navegador a versión más reciente

**Error Común 2**: "SecurityError: Failed to construct 'RTCPeerConnection': Access to RTCPeerConnection is denied"

**Causa**: Navegador requiere HTTPS para WebRTC.

**Solución**:
- Usar `https://` (no `http://`)
- Instalar certificado SSL (ver arriba)

**Diagnóstico 2 - Verificar Endpoint `/webrtc/offer`**:

```powershell
# Probar endpoint manualmente
Invoke-WebRequest -Uri "https://localhost:5001/webrtc/offer" `
                  -Method POST `
                  -Body '{"sdp":"test"}' `
                  -ContentType "application/json"
```

Si retorna error 500:
- Problema en servidor (revisar logs de aplicación)
- `WebRtcStreamService` no está ejecutándose

---

### WebRTC Conecta Pero No Muestra Video

**Síntoma**: Conexión WebRTC se establece (console muestra "Connected"), pero imagen no aparece.

**Diagnóstico 1 - Verificar VideoTrack**:

En DevTools Console, verificar que el video element está recibiendo stream:
```javascript
videoElement.srcObject.getTracks().forEach(track => {
   console.log(`Track: ${track.kind}, State: ${track.readyState}`);
});
// Debe mostrar: "video", "live"
```

Si track está "ended" o no aparece:
- Problema de negociación de VideoTrack
- Firewall bloqueando protocolo
- Servidor no envía H.264 frames correctamente

**Diagnóstico 2 - Verificar Eventos de VideoTrack**:

```javascript
peerConnection.ontrack = (event) => {
   console.log("Track recibido:", event.track.kind);
   videoElement.srcObject = event.streams[0];
};

peerConnection.onconnectionstatechange = () => {
   console.log("Conexión:", peerConnection.connectionState);
};
```

Si `ontrack` nunca se dispara:
- Servidor no está enviando VideoTrack con H.264
- Problema en `WebRtcStreamService` con transmisión de H.264
- Problema en `H264EncoderService` (no genera frames)

**Solución**:

1. Verificar que `DxgiCaptureService` está capturando frames:
   ```
   GET https://localhost:5001/cap/{token}
   ```
   Reemplazar `{token}` con el valor de `CapToken` visible en logs al iniciar la app. Debe retornar imagen JPEG.

2. Si `/cap/{token}` funciona pero WebRTC no:
   - Problema específico de `WebRtcStreamService`
   - Revisar logs de aplicación para excepciones

---

## Problemas de Rendimiento

### Alto Uso de CPU (>50%)

**Síntoma**: Aplicación consume 30-50%+ CPU constantemente.

**Causa**: Captura de pantalla y codificación JPEG son operaciones intensivas.

**Diagnóstico**:

Factores que aumentan uso de CPU:
- ⬆️ Resolución alta (4K vs. 1080p)
- ⬆️ FPS alto (60 FPS vs. 20 FPS)
- ⬆️ Calidad JPEG alta (95 vs. 75)
- ⬆️ Contenido dinámico (gaming vs. dashboard estático)

**Solución 1 - Reducir Resolución**:

```json
{
  "Width": 1280,   // En lugar de 1920
  "Height": 720    // En lugar de 1080
}
```

**Ahorro de CPU**: ~40-50%

**Solución 2 - Reducir FPS**:

```json
{
   "CaptureIntervalSeconds": 0.10  // 10 FPS aprox
}
```

**Ahorro de CPU**: ~50%

**Solución 3 - Reducir Calidad JPEG**:

```json
{
  "JpegQuality": 60  // En lugar de 75-85
}
```

**Ahorro de CPU**: ~20-30%

**Solución 4 - Verificar Detección de Cambios Activa**:

La detección de cambios debería reducir CPU cuando pantalla está estática.

Si no funciona:
- Puede estar deshabilitada en código
- Pantalla tiene contenido que cambia constantemente (ej: animaciones, videos)

---

### Latencia Alta en WebRTC (>100ms)

**Síntoma**: Notas retraso significativo entre mover mouse en pantalla virtual y ver cambio en navegador.

**Diagnóstico**:

Medir latencia:
1. Abrir cronómetro en pantalla virtual
2. Tomar screenshot en navegador cuando muestre un segundo específico
3. Comparar con tiempo real

Latencia >100ms es anómala para WebRTC.

**Causa Posible 1**: Red lenta o congestionada.

**Solución**:
- Usar Ethernet en lugar de WiFi
- Verificar ancho de banda:
  ```powershell
  Test-NetConnection -ComputerName <IP_router>
  ```
- Cerrar otras aplicaciones que usen red (torrents, streaming, etc.)

**Causa Posible 2**: Intervalo de captura alto.

**Solución**:
```json
{
   "CaptureIntervalSeconds": 0.033  // 30 FPS aprox
}
```

**Causa Posible 3**: Encoder H.264 no está generando frames.

**Solución**:

Verificar en logs de aplicación que `H264EncoderService` está activo:
```
[INFO] H264EncoderService: Iniciando encoder...
[INFO] H264EncoderService: Encoder seleccionado: NVENC (NVIDIA)
[INFO] WebRtcStreamService: VideoTrack enviando NAL units...
```

Si no ves estos logs:
- `H264EncoderService` no inicializó correctamente
- GPU no soporta NVENC/AMF (verificar que fallback a libx264 funciona)

---

### Frames Se Saltan o Tartamudean

**Síntoma**: Video no es fluido, frames se saltan o aparecen en ráfagas.

**Causa Posible 1**: Intervalo de captura irregular por alta carga de CPU.

**Solución**:
- Reducir resolución/FPS/calidad (ver "Alto Uso de CPU")
- Cerrar otras aplicaciones que usen CPU

**Causa Posible 2**: Pérdida de paquetes en red.

**Diagnóstico**:
```powershell
ping -t <IP_destino>
# Observar si hay "Request timed out"
```

**Solución**:
- Mejorar conexión de red
- Cambiar a Ethernet
- Acercarse al router WiFi

**Causa Posible 3**: Navegador sobrecargado (muchas tabs, extensiones).

**Solución**:
- Cerrar tabs innecesarias
- Deshabilitar extensiones de navegador temporalmente
- Usar navegador dedicado para VirtualWebDisplay

---

## Problemas de Configuración

### Configuración No Se Guarda

**Síntoma**: Modificar configuración en UI, click "Apply", pero al reiniciar aplicación, configuración vuelve a valores anteriores.

**Causa**: Archivo `virtualscreen.user.json` no tiene permisos de escritura o está corrupto.

**Diagnóstico**:

```powershell
# Verificar que archivo existe y es escribible
Test-Path C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json

# Ver contenido
Get-Content C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json
```

**Solución 1 - Verificar Permisos**:

```powershell
# Dar permisos completos al usuario actual
icacls "C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json" /grant ${env:USERNAME}:F
```

**Solución 2 - Eliminar y Recrear**:

```powershell
del C:\Users\<Usuario>\.virtualwebdisplay\virtualscreen.user.json
# Reiniciar aplicación (creará nuevo virtualscreen.user.json)
```

---

### Error: "Invalid JSON" al Editar Manualmente

**Síntoma**: Después de editar `virtualscreen.user.json`, aplicación muestra error o usa configuración default.

**Causa**: Sintaxis JSON inválida.

**Solución**:

1. Validar JSON en:
   - https://jsonlint.com/
   - https://jsonformatter.org/

2. Errores comunes:
   - ❌ Coma final: `{"Width": 1920,}` → ✅ `{"Width": 1920}`
   - ❌ Comillas simples: `{'Width': 1920}` → ✅ `{"Width": 1920}`
   - ❌ Sin comillas en claves: `{Width: 1920}` → ✅ `{"Width": 1920}`

3. Usar editor con validación JSON (VS Code, Notepad++).

---

## Errores Comunes y Soluciones

### Exception: "Object reference not set to an instance of an object"

**Ubicación**: `VirtualDisplayManager.TryCreate()`

**Causa**: Parsec VDD driver no respondió correctamente.

**Solución**:
- Reinstalar driver Parsec VDD
- Reiniciar Windows
- Verificar que no hay otro software de pantalla virtual conflictivo (DisplayLink, Spacedesk, etc.)

---

### Exception: "Access to the path is denied"

**Ubicación**: `VirtualScreenSettingsStore.SaveSettings()`

**Causa**: Carpeta `.virtualwebdisplay` tiene permisos restrictivos.

**Solución**:

```powershell
# Dar permisos completos a la carpeta
icacls "C:\Users\<Usuario>\.virtualwebdisplay" /grant ${env:USERNAME}:F /T
```

---

### Exception: "The process cannot access the file because it is being used by another process"

**Ubicación**: `LocalCertificateProvider.GetCertificate()`

**Causa**: Archivo `localhost.pfx` está siendo accedido por otro proceso.

**Solución**:

```powershell
# Buscar procesos que usan el archivo
handle64.exe "C:\Users\<Usuario>\.virtualwebdisplay\localhost.pfx"

# O usar Resource Monitor:
# Win+R → resmon → CPU tab → search "localhost.pfx"
```

Terminar proceso que lo está usando o reiniciar Windows.

---

## Recopilación de Logs

### Habilitar Logging Detallado

**Opción 1 - Ejecutar desde PowerShell**:

```powershell
cd "C:\Path\To\VirtualWebDisplay"
.\VirtualWebDisplay_Parsec.exe > logs.txt 2>&1
```

Output se guardará en `logs.txt`.

**Opción 2 - Modificar `appsettings.json`** (si existe):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

### Información Útil para Reportar Issues

Al reportar un problema en GitHub, incluir:

1. **Versión de VirtualWebDisplay**:
   ```
   (Ver en propiedades del archivo .exe)
   ```

2. **Versión de Windows**:
   ```powershell
   winver
   ```

3. **Versión de .NET**:
   ```powershell
   dotnet --version
   ```

4. **Configuración**:
   ```powershell
   Get-Content C:\Users\<Usuario>\.virtualwebdisplay\settings.json
   ```

5. **Logs de error** (si están disponibles).

6. **Pasos para reproducir** el problema.

---

## FAQ

### ¿Puedo conectar mi tablet/celular por cable USB en lugar de WiFi?

**Sí, especialmente en dispositivos Android.** Aunque la aplicación no tiene un botón explícito de "Modo USB" en la interfaz, puedes usar el cable para obtener la mejor latencia y estabilidad (evitando el lag del WiFi):

1. Conecta tu dispositivo Android a la PC mediante el cable USB.
2. En tu Android, ve a **Ajustes > Redes e Internet > Zona Wi-Fi / Compartir conexión** (los nombres pueden variar según la marca).
3. Activa la opción **Anclaje de red por USB** (USB Tethering).
4. Windows detectará tu teléfono como si fuera una conexión de red por cable (asegúrate de que el Firewall de Windows permita conexiones públicas/privadas para la app).
5. Abre el navegador en tu Android e ingresa la URL de la aplicación que se muestra en tu PC. El sistema operativo enrutará automáticamente el tráfico a través del cable USB.

---

### ¿Puedo usar más de 2 pantallas virtuales?

**Actualmente**: No, limitado a 2 pantallas.

**Futuro**: Planeado para v2.0.0.

**Workaround**: Ejecutar múltiples instancias (requiere modificar `SingleInstanceManager` en código).

---

### ¿Funciona con TeamViewer/AnyDesk/Remote Desktop?

**Sí**, pero:
- TeamViewer/AnyDesk: Pueden no mostrar pantallas virtuales por defecto (requiere configuración)
- Remote Desktop: Solo muestra pantallas conectadas físicamente (pantallas virtuales pueden no aparecer)

**Recomendación**: Usar VirtualWebDisplay como alternativa a estos servicios, no en conjunto.

---

### ¿Puedo controlar el mouse/teclado desde el navegador?

**Actualmente**: No, solo visualización.

**Futuro**: Planeado para v1.3.0 (control remoto).

---

### ¿Funciona en Linux/macOS?

**No**, requiere:
- Windows 10/11 para Parsec VDD driver
- WinForms para UI

**Futuro**: Cliente web podría ejecutarse en cualquier OS, pero servidor solo Windows.

---

### ¿Puedo streamear audio también?

**Actualmente**: No.

**Futuro**: Planeado para v1.2.0.

---

### ¿Cuál es el ancho de banda requerido?

Depende de configuración:

| Configuración | Ancho de Banda Estimado |
|---------------|-------------------------|
| 720p, 10 FPS, Quality 60 | ~1-2 Mbps |
| 1080p, 20 FPS, Quality 75 | ~5-10 Mbps |
| 1080p, 30 FPS, Quality 85 | ~10-20 Mbps |
| 4K, 60 FPS, Quality 95 | ~50-100 Mbps |

**Medición Real**:

Usar DevTools → Network tab para ver transferencia en tiempo real.

---

Para más ayuda, visitar:
- **GitHub Issues**: https://github.com/quiro90/VirtualWebDisplay/issues
- **Discussions**: https://github.com/quiro90/VirtualWebDisplay/discussions
