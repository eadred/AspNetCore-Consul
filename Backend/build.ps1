$OldLoc = Get-Location
Set-Location ($MyInvocation.MyCommand.Path | Split-Path)

dotnet restore
dotnet build
#dotnet run --server.urls http://0.0.0.0:5100

Set-Location $OldLoc
