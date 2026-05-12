$redisExe = "E:\RP\Redis-x64-3.0.504\redis-server.exe"

Start-Process $redisExe -ArgumentList "--port","6000","--maxmemory","256mb","--dir","E:\RP\DISTRIBUTED-PROGRAMMING\redis-main" -WorkingDirectory "E:\RP\DISTRIBUTED-PROGRAMMING\redis-main"
Start-Process $redisExe -ArgumentList "--port","6001","--maxmemory","256mb","--dir","E:\RP\DISTRIBUTED-PROGRAMMING\redis-ru" -WorkingDirectory "E:\RP\DISTRIBUTED-PROGRAMMING\redis-ru"
Start-Process $redisExe -ArgumentList "--port","6002","--maxmemory","256mb","--dir","E:\RP\DISTRIBUTED-PROGRAMMING\redis-eu" -WorkingDirectory "E:\RP\DISTRIBUTED-PROGRAMMING\redis-eu"
Start-Process $redisExe -ArgumentList "--port","6003","--maxmemory","256mb","--dir","E:\RP\DISTRIBUTED-PROGRAMMING\redis-asia" -WorkingDirectory "E:\RP\DISTRIBUTED-PROGRAMMING\redis-asia"

$env:DB_MAIN = "localhost:6000"
$env:DB_RU = "localhost:6001" 
$env:DB_EU = "localhost:6002"
$env:DB_ASIA = "localhost:6003"

Write-Host "Valuator 5001..."
Start-Process powershell -ArgumentList '-NoExit -Command "cd E:\RP\DISTRIBUTED-PROGRAMMING\Valuator; dotnet run --urls http://0.0.0.0:5001"'

Write-Host "RankCalculator #1..."
Start-Process powershell -ArgumentList '-NoExit -Command "cd E:\RP\DISTRIBUTED-PROGRAMMING\RankCalculator; dotnet run"'

Write-Host "EventsLogger #1..."
Start-Process powershell -ArgumentList '-NoExit -Command "cd E:\RP\DISTRIBUTED-PROGRAMMING\EventsLogger; dotnet run"'

Start-Sleep 5

Write-Host "Nginx..."
Push-Location "C:\nginx-1.29.5"
& .\nginx.exe -c "E:\RP\DISTRIBUTED-PROGRAMMING\nginx\conf\nginx.conf"
Pop-Location