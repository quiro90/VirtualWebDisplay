# Script para completar la reorganización de archivos
# Ejecutar desde la raíz del proyecto

Write-Host "🚀 Iniciando reorganización de archivos..." -ForegroundColor Green

# Función para agregar namespace a un archivo
function Add-Namespace {
    param(
        [string]$SourceFile,
        [string]$DestFile,
        [string]$Namespace
    )

    $content = Get-Content $SourceFile -Raw

    # Si ya tiene namespace, no agregar
    if ($content -match "namespace ") {
        Copy-Item $SourceFile $DestFile -Force
        return
    }

    # Agregar namespace al inicio
    $newContent = "namespace $Namespace;`n`n$content"
    Set-Content -Path $DestFile -Value $newContent -Encoding UTF8
}

# Mover archivos de Configuration
Write-Host "📁 Moviendo archivos de Configuration..." -ForegroundColor Cyan
Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualScreenConfig.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\Models\VirtualScreenConfig.cs" `
              -Namespace "VirtualWebDisplay.Configuration.Models"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualWebDisplaySettings.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\Models\VirtualWebDisplaySettings.cs" `
              -Namespace "VirtualWebDisplay.Configuration.Models"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualScreenSettingsStore.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\VirtualScreenSettingsStore.cs" `
              -Namespace "VirtualWebDisplay.Configuration"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualDisplayProfiles.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\VirtualDisplayProfiles.cs" `
              -Namespace "VirtualWebDisplay.Configuration"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\TransmissionModeOptions.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\TransmissionModeOptions.cs" `
              -Namespace "VirtualWebDisplay.Configuration"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualDisplayPlacementOptions.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Configuration\VirtualDisplayPlacementOptions.cs" `
              -Namespace "VirtualWebDisplay.Configuration"

# Mover archivos de Parsec
Write-Host "📁 Moviendo archivos de Parsec..." -ForegroundColor Cyan
Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\VirtualDisplayManager.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Parsec\VirtualDisplayManager.cs" `
              -Namespace "VirtualWebDisplay.Parsec"

# Mover archivos de Streaming
Write-Host "📁 Moviendo archivos de Streaming..." -ForegroundColor Cyan
Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\CaptureService.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Streaming\CaptureService.cs" `
              -Namespace "VirtualWebDisplay.Streaming"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\WebRtcStreamService.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Streaming\WebRtcStreamService.cs" `
              -Namespace "VirtualWebDisplay.Streaming"

# Mover archivos de Infrastructure
Write-Host "📁 Moviendo archivos de Infrastructure..." -ForegroundColor Cyan
Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\ScreenRuntimeContext.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Infrastructure\ScreenRuntimeContext.cs" `
              -Namespace "VirtualWebDisplay.Infrastructure"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\NetworkAddressHelper.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Infrastructure\NetworkAddressHelper.cs" `
              -Namespace "VirtualWebDisplay.Infrastructure"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\LocalCertificateProvider.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Infrastructure\LocalCertificateProvider.cs" `
              -Namespace "VirtualWebDisplay.Infrastructure"

Add-Namespace -SourceFile "VirtualWebDisplay_Parsec\SingleInstanceManager.cs" `
              -DestFile "VirtualWebDisplay_Parsec\Infrastructure\SingleInstanceManager.cs" `
              -Namespace "VirtualWebDisplay.Infrastructure"

Write-Host "✅ Archivos reorganizados exitosamente!" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  NOTA: Ejecutar el script Update-Imports.ps1 para actualizar los using statements" -ForegroundColor Yellow
