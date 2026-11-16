param([string]$InstallDirectory=$PSScriptRoot)
$ErrorActionPreference='Stop'
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$destination=[IO.Path]::GetFullPath($InstallDirectory)
if(-not (Test-Path -LiteralPath $destination -PathType Container)){throw 'Extract the app to an existing writable folder first.'}
$target=Join-Path $destination 'scrcpy'
if(Test-Path -LiteralPath $target){throw 'scrcpy already exists. Keep it, or rename it before preparing a fresh official copy.'}
$work=Join-Path $destination ('.dependencies-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$zip=Join-Path $work 'scrcpy.zip'
$url='https://github.com/Genymobile/scrcpy/releases/download/v5.0.1/scrcpy-win64-v5.0.1.zip'
Write-Host 'Downloading unmodified official scrcpy 5.0.1 (including ADB)...'
Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $zip
if((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5'){throw 'SHA-256 mismatch; dependency was not installed.'}
Expand-Archive -LiteralPath $zip -DestinationPath $work
$source=Join-Path $work 'scrcpy-win64-v5.0.1'
foreach($file in @('scrcpy.exe','scrcpy-server','adb.exe','LICENSE.txt')){if(-not (Test-Path -LiteralPath (Join-Path $source $file) -PathType Leaf)){throw "Missing official component: $file"}}
Move-Item -LiteralPath $source -Destination $target
Remove-Item -LiteralPath $zip
Remove-Item -LiteralPath $work
Write-Host "Ready: $target"
