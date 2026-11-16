param([switch]$Test,[switch]$Package)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$c=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $c)){throw '.NET Framework C# compiler not found. Windows x64 required.'}
$bin=Join-Path $root 'bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$src=Join-Path $root 'src\AdbKeeper.cs'
$refs=@('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll')
& $c /nologo /target:winexe /optimize+ /codepage:65001 ("/out:$bin\AdbKeeper.exe") ("/win32icon:$root\assets\phone.ico") $refs $src (Join-Path $root 'src\AssemblyInfo.cs')
if($LASTEXITCODE -ne 0){throw 'Build failed'}
if($Test){
  & $c /nologo /target:exe /codepage:65001 ("/out:$bin\FakeAdb.exe") (Join-Path $root 'tests\FakeAdb.cs')
  if($LASTEXITCODE -ne 0){throw 'Fake ADB build failed'}
  foreach($testName in @('DeviceChecks','NoticeChecks','LifecycleChecks')){
    & $c /nologo /target:exe /codepage:65001 ("/main:$testName") ("/out:$bin\$testName.exe") $refs $src (Join-Path $root "tests\$testName.cs")
    if($LASTEXITCODE -ne 0){throw "Test build failed: $testName"}
    & "$bin\$testName.exe"
    if($LASTEXITCODE -ne 0){throw "Test failed: $testName"}
  }
}
if($Package){
  $dist=Join-Path $root 'dist';New-Item -ItemType Directory -Force -Path $dist | Out-Null
  $stage=Join-Path $dist 'AdbKeeper-v1.0.0-win-x64'
  New-Item -ItemType Directory -Force -Path $stage | Out-Null
  Copy-Item -LiteralPath "$bin\AdbKeeper.exe" -Destination $stage -Force
  foreach($file in @('README.md','LICENSE','THIRD_PARTY.md','VALIDATION.md','Install-Dependencies.cmd','Install-Dependencies.ps1')){Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage -Force}
  $zip=Join-Path $dist 'AdbKeeper-v1.0.0-win-x64.zip'
  Compress-Archive -LiteralPath $stage -DestinationPath $zip -Force
  $hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
  [IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS.txt'),$hash+'  '+[IO.Path]::GetFileName($zip)+"`n",[Text.UTF8Encoding]::new($false))
}
