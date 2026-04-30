-----------------------
A continuación, adjunto el log del depurador JIT y el código actual del componente que maneja el Tray Icon. Necesito que identifiques la causa del fallo y apliques la corrección con manejo de excepciones adecuado.

Consulte el final de este mensaje para obtener más detalles sobre cómo invocar a la depuración 
Just-In-Time (JIT) en lugar de a este cuadro de diálogo.

************** Texto de la excepción **************
System.NullReferenceException: Object reference not set to an instance of an object.
   at VirtualWebDisplay.UI.TrayIcon.ConfigurationFormPresenter.ShowConfigurationDialog(IReadOnlyList`1 screenRuntimes) in C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\UI\TrayIcon\ConfigurationFormPresenter.cs:line 93
   at VirtualWebDisplay.UI.TrayIcon.ConfigurationFormPresenter.ShowConfigurationDialog(IReadOnlyList`1 screenRuntimes) in C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\UI\TrayIcon\ConfigurationFormPresenter.cs:line 77
   at VirtualWebDisplay.UI.TrayIcon.VirtualDisplayTrayController.ShowConfigurationDialog() in C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\UI\TrayIcon\VirtualDisplayTrayController.cs:line 152
   at VirtualWebDisplay.UI.TrayIcon.VirtualDisplayTrayController.<RunUiThread>b__18_0(Object _, EventArgs _) in C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\UI\TrayIcon\VirtualDisplayTrayController.cs:line 129
   at System.Windows.Forms.NotifyIcon.WmMouseDown(MouseButtons button, Int32 clicks)
   at System.Windows.Forms.NativeWindow.Callback(HWND hWnd, UInt32 msg, WPARAM wparam, LPARAM lparam)


************** Ensamblados cargados **************
System.Private.CoreLib
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Private.CoreLib.dll
----------------------------------------
VirtualWebDisplay
    Versión del ensamblado: 1.0.0.0
    Ubicación: C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\bin\Debug\net10.0-windows\VirtualWebDisplay.dll
----------------------------------------
System.Runtime
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.dll
----------------------------------------
System.Security.Cryptography
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Security.Cryptography.dll
----------------------------------------
System.Memory
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Memory.dll
----------------------------------------
System.Net.NameResolution
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.NameResolution.dll
----------------------------------------
System.Windows.Forms
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\System.Windows.Forms.dll
----------------------------------------
System.Linq
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Linq.dll
----------------------------------------
System.Collections
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.dll
----------------------------------------
System.Text.Json
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Text.Json.dll
----------------------------------------
System.Threading.Thread
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Threading.Thread.dll
----------------------------------------
System.Threading
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Threading.dll
----------------------------------------
System.Text.Encoding.Extensions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Text.Encoding.Extensions.dll
----------------------------------------
System.Text.Encodings.Web
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Text.Encodings.Web.dll
----------------------------------------
System.Numerics.Vectors
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Numerics.Vectors.dll
----------------------------------------
System.Collections.Concurrent
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.Concurrent.dll
----------------------------------------
System.Private.Uri
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Private.Uri.dll
----------------------------------------
System.Reflection.Emit.Lightweight
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Reflection.Emit.Lightweight.dll
----------------------------------------
System.Reflection.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Reflection.Primitives.dll
----------------------------------------
System.Reflection.Emit.ILGeneration
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Reflection.Emit.ILGeneration.dll
----------------------------------------
System.Runtime.InteropServices
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.InteropServices.dll
----------------------------------------
System.IO.Pipelines
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.IO.Pipelines.dll
----------------------------------------
System.Diagnostics.Process
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Diagnostics.Process.dll
----------------------------------------
System.ComponentModel.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ComponentModel.Primitives.dll
----------------------------------------
System.Net.NetworkInformation
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.NetworkInformation.dll
----------------------------------------
System.Net.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.Primitives.dll
----------------------------------------
Microsoft.Win32.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\Microsoft.Win32.Primitives.dll
----------------------------------------
System.Runtime.Intrinsics
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.Intrinsics.dll
----------------------------------------
System.Diagnostics.Tracing
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Diagnostics.Tracing.dll
----------------------------------------
System.Diagnostics.DiagnosticSource
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Diagnostics.DiagnosticSource.dll
----------------------------------------
System.Windows.Forms.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\System.Windows.Forms.Primitives.dll
----------------------------------------
System.Private.Windows.Core
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\System.Private.Windows.Core.dll
----------------------------------------
System.Private.Windows.GdiPlus
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\System.Private.Windows.GdiPlus.dll
----------------------------------------
System.Drawing.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Drawing.Primitives.dll
----------------------------------------
System.Drawing.Common
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\System.Drawing.Common.dll
----------------------------------------
System.Collections.Specialized
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.Specialized.dll
----------------------------------------
System.ComponentModel.EventBasedAsync
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ComponentModel.EventBasedAsync.dll
----------------------------------------
Accessibility
    Versión del ensamblado: 4.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\Accessibility.dll
----------------------------------------
Microsoft.Win32.SystemEvents
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\Microsoft.Win32.SystemEvents.dll
----------------------------------------
System.ComponentModel.TypeConverter
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ComponentModel.TypeConverter.dll
----------------------------------------
VirtualWebDisplay.resources
    Versión del ensamblado: 1.0.0.0
    Ubicación: C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\bin\Debug\net10.0-windows\es\VirtualWebDisplay.resources.dll
----------------------------------------
System.ComponentModel
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ComponentModel.dll
----------------------------------------
System.Runtime.Loader
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.Loader.dll
----------------------------------------
System.Windows.Forms.resources
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.0.6\es\System.Windows.Forms.resources.dll
----------------------------------------
Microsoft.Win32.Registry
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\Microsoft.Win32.Registry.dll
----------------------------------------
System.Collections.NonGeneric
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.NonGeneric.dll
----------------------------------------
System.Formats.Asn1
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Formats.Asn1.dll
----------------------------------------
System.IO.MemoryMappedFiles
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.IO.MemoryMappedFiles.dll
----------------------------------------
Microsoft.AspNetCore
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.dll
----------------------------------------
Microsoft.Extensions.Hosting.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Hosting.Abstractions.dll
----------------------------------------
Microsoft.AspNetCore.Http.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Http.Abstractions.dll
----------------------------------------
Microsoft.AspNetCore.Routing
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Routing.dll
----------------------------------------
Microsoft.Extensions.Features
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Features.dll
----------------------------------------
Microsoft.Extensions.DependencyInjection.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.DependencyInjection.Abstractions.dll
----------------------------------------
Microsoft.Extensions.Logging.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.Abstractions.dll
----------------------------------------
Microsoft.Extensions.Diagnostics.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Diagnostics.Abstractions.dll
----------------------------------------
Microsoft.Extensions.Configuration.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.Abstractions.dll
----------------------------------------
Microsoft.AspNetCore.StaticFiles
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.StaticFiles.dll
----------------------------------------
Microsoft.Extensions.Configuration
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.dll
----------------------------------------
Microsoft.Extensions.Primitives
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Primitives.dll
----------------------------------------
Microsoft.Extensions.Hosting
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Hosting.dll
----------------------------------------
Microsoft.AspNetCore.Hosting
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Hosting.dll
----------------------------------------
Microsoft.AspNetCore.Hosting.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Hosting.Abstractions.dll
----------------------------------------
Microsoft.Extensions.Configuration.EnvironmentVariables
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.EnvironmentVariables.dll
----------------------------------------
Microsoft.Extensions.FileProviders.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.FileProviders.Abstractions.dll
----------------------------------------
Microsoft.Extensions.FileProviders.Physical
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.FileProviders.Physical.dll
----------------------------------------
Microsoft.Extensions.Configuration.FileExtensions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.FileExtensions.dll
----------------------------------------
Microsoft.Extensions.Options
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Options.dll
----------------------------------------
Microsoft.Extensions.Logging
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.dll
----------------------------------------
Microsoft.Extensions.Diagnostics
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Diagnostics.dll
----------------------------------------
Microsoft.Extensions.Configuration.Binder
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.Binder.dll
----------------------------------------
Microsoft.Extensions.Configuration.Json
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Configuration.Json.dll
----------------------------------------
System.IO.FileSystem.Watcher
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.IO.FileSystem.Watcher.dll
----------------------------------------
System.Threading.Overlapped
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Threading.Overlapped.dll
----------------------------------------
Microsoft.Extensions.Logging.EventLog
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.EventLog.dll
----------------------------------------
Microsoft.Extensions.Logging.Configuration
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.Configuration.dll
----------------------------------------
Microsoft.Extensions.Options.ConfigurationExtensions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Options.ConfigurationExtensions.dll
----------------------------------------
Microsoft.Extensions.Logging.Console
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.Console.dll
----------------------------------------
Microsoft.Extensions.Logging.Debug
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.Debug.dll
----------------------------------------
Microsoft.Extensions.Logging.EventSource
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Logging.EventSource.dll
----------------------------------------
Microsoft.Extensions.DependencyInjection
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.DependencyInjection.dll
----------------------------------------
Microsoft.AspNetCore.Server.Kestrel.Core
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.Kestrel.Core.dll
----------------------------------------
Microsoft.AspNetCore.Server.Kestrel
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.Kestrel.dll
----------------------------------------
Microsoft.AspNetCore.Server.Kestrel.Transport.Quic
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.dll
----------------------------------------
Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.dll
----------------------------------------
Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.dll
----------------------------------------
System.Net.Quic
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.Quic.dll
----------------------------------------
System.Net.Sockets
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.Sockets.dll
----------------------------------------
Microsoft.AspNetCore.Server.IIS
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.IIS.dll
----------------------------------------
Microsoft.AspNetCore.Server.IISIntegration
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Server.IISIntegration.dll
----------------------------------------
Microsoft.AspNetCore.Http
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Http.dll
----------------------------------------
Microsoft.AspNetCore.Connections.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Connections.Abstractions.dll
----------------------------------------
Microsoft.AspNetCore.Hosting.Server.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Hosting.Server.Abstractions.dll
----------------------------------------
Microsoft.Extensions.ObjectPool
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.ObjectPool.dll
----------------------------------------
Microsoft.AspNetCore.HostFiltering
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.HostFiltering.dll
----------------------------------------
Microsoft.AspNetCore.HttpOverrides
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.HttpOverrides.dll
----------------------------------------
Microsoft.AspNetCore.Routing.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Routing.Abstractions.dll
----------------------------------------
System.ObjectModel
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ObjectModel.dll
----------------------------------------
System.Console
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Console.dll
----------------------------------------
System.Diagnostics.EventLog
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\System.Diagnostics.EventLog.dll
----------------------------------------
System.Threading.ThreadPool
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Threading.ThreadPool.dll
----------------------------------------
System.IO.Pipes
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.IO.Pipes.dll
----------------------------------------
Microsoft.AspNetCore.Http.Extensions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Http.Extensions.dll
----------------------------------------
Microsoft.AspNetCore.Authentication.Abstractions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Authentication.Abstractions.dll
----------------------------------------
Microsoft.AspNetCore.Authorization
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Authorization.dll
----------------------------------------
Microsoft.AspNetCore.Http.Features
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Http.Features.dll
----------------------------------------
Microsoft.Net.Http.Headers
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Net.Http.Headers.dll
----------------------------------------
System.Net.Security
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.Security.dll
----------------------------------------
System.Security.Claims
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Security.Claims.dll
----------------------------------------
System.Net.WebSockets
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Net.WebSockets.dll
----------------------------------------
Microsoft.Extensions.Validation
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.Extensions.Validation.dll
----------------------------------------
System.Linq.Expressions
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Linq.Expressions.dll
----------------------------------------
System.Runtime.Numerics
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.Numerics.dll
----------------------------------------
Anonymously Hosted DynamicMethods Assembly
    Versión del ensamblado: 0.0.0.0
    Ubicación: C:\Users\Juan Quiroga\Desarrollo\net.core\VirtualWebDisplay\VirtualWebDisplay_Parsec\bin\Debug\net10.0-windows\
----------------------------------------
System.Collections.Immutable
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.Immutable.dll
----------------------------------------
Microsoft.AspNetCore.Metadata
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Metadata.dll
----------------------------------------
Microsoft.AspNetCore.Http.Results
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.Http.Results.dll
----------------------------------------
Microsoft.AspNetCore.WebUtilities
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.6\Microsoft.AspNetCore.WebUtilities.dll
----------------------------------------
System.Diagnostics.StackTrace
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Diagnostics.StackTrace.dll
----------------------------------------
System.Reflection.Metadata
    Versión del ensamblado: 10.0.0.0
    Ubicación: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Reflection.Metadata.dll
----------------------------------------

************** Depuración JIT **************

