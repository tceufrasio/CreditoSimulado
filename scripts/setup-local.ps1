$ErrorActionPreference = 'Stop'
docker compose up -d
if ($LASTEXITCODE -ne 0) { throw 'Falha ao iniciar PostgreSQL.' }
Write-Host 'Aguardando PostgreSQL ficar pronto...'
$ready = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    docker exec credito-postgres pg_isready -U credito -d credito *> $null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $ready) { throw 'PostgreSQL não ficou disponível. Veja docker logs credito-postgres.' }
Write-Host 'PostgreSQL pronto. O schema é aplicado automaticamente na primeira criação do volume.'
Write-Host "Connection string: Host=localhost;Port=5432;Database=credito;Username=credito;Password=credito_local"
