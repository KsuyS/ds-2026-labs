Get-Process "VBCSCompiler","dotnet","nginx" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Остановлено!"
