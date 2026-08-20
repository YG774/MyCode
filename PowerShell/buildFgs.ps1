param(
  [string]$configuration = "Debug"
)

$scriptPath = $PSScriptRoot
if ( $configuration -eq "Debug" ) {
  $buildPath = [System.IO.Path]::Combine($scriptPath, "build", "Debug", "net10.0");
  $backupPath = [System.IO.Path]::Combine($scriptPath, "build", "Debug", "temp_net10.0");
}
elseif ( $configuration -eq "Release" ) {
  $buildPath = [System.IO.Path]::Combine($scriptPath, "build", "Release", "net10.0");
  $backupPath = [System.IO.Path]::Combine($scriptPath, "build", "Release", "temp_net10.0");
}
else {
  Write-Host 未知参数 $configuration
  return
}

$desktopPath = [Environment]::GetFolderPath('Desktop')
$slnPath = Join-Path $scriptPath "zocono.fgs.host.slnx"
$zipPath = Join-Path $desktopPath "host.zip"

function Ren-Back {
  if (Test-Path $buildPath) {
    rm -Path $buildPath -Recurse
  }	  
  if (Test-Path $backupPath) {
    ren $backupPath $buildPath
  }
} 

if (!(Test-Path $slnPath)) {
  Write-Host ${slnPath}解决方案不存在
  Read-Host -Prompt "按 Enter 键继续..."
  return
}

if (Test-Path $buildPath) {
  ren $buildPath $backupPath
  if (!$?) {
    Write-Host 备份当前的目录，重命名文件夹失败
	Read-Host -Prompt "按 Enter 键继续..."
    return
  }
}
try {
  dotnet clean $slnPath -c $configuration
  dotnet build $slnPath -c $configuration -v d
  if ($LASTEXITCODE -ne 0) {
    Write-Host 生成解决方案失败
    return
  }
  Compress-Archive -Path $buildPath -DestinationPath $zipPath -Force
}
finally {
  Ren-Back
}
Read-Host -Prompt "按 Enter 键继续..."
return
