$scriptPath = "D:\origin\zocono.fgs.vnext.host"
$desktopPath = [Environment]::GetFolderPath('Desktop')
$slnPath = Join-Path $scriptPath "zocono.fgs.host.slnx"
$buildPath = [System.IO.Path]::Combine($scriptPath, "build", "Debug", "net10.0");
$backupPath = [System.IO.Path]::Combine($scriptPath, "build", "Debug", "temp_net10.0");
$hostZipPath = Join-Path $desktopPath "host.zip"

function Select-One {
  param(
    $Title,
	[Parameter(Mandatory,ValueFromPipeline)]
	[string[]]$Items
  )
  Write-Host $Title
  $x0 = [Console]::CursorLeft
  $y0 = [Console]::CursorTop
  
  $count = $Items.Length
  $cur = 0;
  do {
	  [Console]::SetCursorPosition($x0,$y0)
	  
	  for ($i = 0; $i -lt $count; $i++){
		if($cur -eq $i) { 
	      Write-Host " -> $($Items[$i])`n" -ForegroundColor Yellow -NoNewline
		}
		else {
		  Write-Host "    $($Items[$i])`n" -NoNewline
	    }
	  }
	  
    $key = [Console]::ReadKey($true) 
	switch ($key.Key){
	  UpArrow {
	    $cur--
		if ( $cur -lt 0 ) { $cur = 0 }
	  }
	  DownArrow {
	    $cur++
		if ( $cur -ge $count ) { $cur = $count - 1 }
	  }
	  Enter {
		return $cur
	  }
	  Escape {
		return -1
	  }
	}
  }while ($true)
}
try{
$result = Select-One -Title 请选择请选择请选择请选择请选择请选择请选择 请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择1, 请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择2,      请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择请选择3
Write-Host 你选择了$result
}catch {
	Write-Host "出错了" -Foreground Red
}