Write-Host "Valuator 5001..."
Start-Process powershell -ArgumentList '-NoExit -Command "cd E:\RP\DISTRIBUTED-PROGRAMMING\Valuator; dotnet run --urls http://0.0.0.0:5001"'

Write-Host "Valuator 5002..."
Start-Process powershell -ArgumentList '-NoExit -Command "cd E:\RP\DISTRIBUTED-PROGRAMMING\Valuator; dotnet run --urls http://0.0.0.0:5002"'

Start-Sleep 5

Write-Host "Nginx..."
Push-Location "C:\nginx-1.29.5"
& .\nginx.exe -c "E:\RP\DISTRIBUTED-PROGRAMMING\nginx\conf\nginx.conf"
Pop-Location