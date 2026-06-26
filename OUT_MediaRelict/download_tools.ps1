# MEDIA RELIC v0.1 tool downloader
# Downloads local portable tools into ./tools:
#   mpv.exe
#   ffmpeg.exe
#   ffprobe.exe
#
# Run from the MediaRelic project folder:
#   powershell -ExecutionPolicy Bypass -File .\download_tools.ps1

$ErrorActionPreference = "Stop"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Root  = Split-Path -Parent $MyInvocation.MyCommand.Path
$Tools = Join-Path $Root "tools"
$Temp  = Join-Path $Root "_tool_download_tmp"

New-Item -ItemType Directory -Force -Path $Tools | Out-Null

if (Test-Path $Temp) {
    Remove-Item $Temp -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $Temp | Out-Null

function Say($Text) {
    Write-Host ""
    Write-Host "== $Text" -ForegroundColor Cyan
}

function Download($Url, $OutFile) {
    Write-Host "Downloading:"
    Write-Host "  $Url"
    Invoke-WebRequest `
        -Uri $Url `
        -OutFile $OutFile `
        -UseBasicParsing `
        -Headers @{ "User-Agent" = "MediaRelicToolDownloader/0.1" }
}

function NeedExe($Path, $Name) {
    if (!(Test-Path $Path)) {
        throw "Missing $Name at $Path"
    }
}

try {
    Say "Downloading FFmpeg release essentials"

    $ffmpegZip = Join-Path $Temp "ffmpeg-release-essentials.zip"
    $ffmpegOut = Join-Path $Temp "ffmpeg"

    # Gyan.dev release essentials build contains ffmpeg.exe and ffprobe.exe.
    Download "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" $ffmpegZip

    Expand-Archive -Path $ffmpegZip -DestinationPath $ffmpegOut -Force

    $ffmpegExe = Get-ChildItem $ffmpegOut -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
    $ffprobeExe = Get-ChildItem $ffmpegOut -Recurse -Filter "ffprobe.exe" | Select-Object -First 1

    if ($null -eq $ffmpegExe) { throw "ffmpeg.exe not found in archive" }
    if ($null -eq $ffprobeExe) { throw "ffprobe.exe not found in archive" }

    Copy-Item $ffmpegExe.FullName  (Join-Path $Tools "ffmpeg.exe")  -Force
    Copy-Item $ffprobeExe.FullName (Join-Path $Tools "ffprobe.exe") -Force

    Say "Downloading tiny 7-Zip extractor for mpv archive"

    $sevenZip = Join-Path $Temp "7zr.exe"

    # Official 7-Zip standalone console extractor.
    Download "https://www.7-zip.org/a/7zr.exe" $sevenZip
    NeedExe $sevenZip "7zr.exe"

    Say "Finding latest mpv Windows build"

    # mpv official installation page lists third-party Windows builds.
    # zhongfly tends to publish very fresh GitHub release assets.
    $releaseApi = "https://api.github.com/repos/zhongfly/mpv-winbuild/releases/latest"
    $release = Invoke-RestMethod `
        -Uri $releaseApi `
        -Headers @{
            "User-Agent" = "MediaRelicToolDownloader/0.1"
            "Accept" = "application/vnd.github+json"
        }

    # Safer baseline asset:
    #   mpv-x86_64-YYYYMMDD-git-xxxx.7z
    # Skip debug and v3 builds. v3 is faster, but baseline x86_64 is less picky about old CPUs.
    $mpvAsset = $release.assets |
        Where-Object {
            $_.name -match '^mpv-x86_64-\d{8}-git-.+\.7z$' `
            -and $_.name -notmatch 'debug' `
            -and $_.name -notmatch 'v3'
        } |
        Select-Object -First 1

    if ($null -eq $mpvAsset) {
        throw "Could not find baseline mpv x86_64 asset in latest zhongfly release."
    }

    $mpvArchive = Join-Path $Temp $mpvAsset.name
    Download $mpvAsset.browser_download_url $mpvArchive

    Say "Extracting mpv"

    $mpvOut = Join-Path $Temp "mpv"
    New-Item -ItemType Directory -Force -Path $mpvOut | Out-Null

    & $sevenZip x "-o$mpvOut" -y $mpvArchive | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "7zr failed to extract mpv archive."
    }

    $mpvExe = Get-ChildItem $mpvOut -Recurse -Filter "mpv.exe" | Select-Object -First 1

    if ($null -eq $mpvExe) {
        throw "mpv.exe not found in archive"
    }

    Copy-Item $mpvExe.FullName (Join-Path $Tools "mpv.exe") -Force

    Say "Verifying tools"

    $mpvLocal     = Join-Path $Tools "mpv.exe"
    $ffmpegLocal  = Join-Path $Tools "ffmpeg.exe"
    $ffprobeLocal = Join-Path $Tools "ffprobe.exe"

    NeedExe $mpvLocal "mpv.exe"
    NeedExe $ffmpegLocal "ffmpeg.exe"
    NeedExe $ffprobeLocal "ffprobe.exe"

    Write-Host ""
    Write-Host "Installed into:" -ForegroundColor Green
    Write-Host "  $Tools"
    Write-Host ""

    Write-Host "Versions:" -ForegroundColor Green
    & $mpvLocal --version     | Select-Object -First 1
    & $ffmpegLocal -version   | Select-Object -First 1
    & $ffprobeLocal -version  | Select-Object -First 1

    Write-Host ""
    Write-Host "Done. MEDIA RELIC can now find the tools. Humanity survives another folder." -ForegroundColor Green
}
finally {
    if (Test-Path $Temp) {
        Remove-Item $Temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}
