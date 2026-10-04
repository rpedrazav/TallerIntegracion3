Write-Host "Construyendo y subiendo imagenes a Docker Hub..."

$services = @("TenantIdentityService", "TaxComplianceService", "CatalogPricingService", "POSCartService")
$user = "rpedraza"

foreach ($svc in $services) {
    Write-Host "Construyendo $svc..."
    $imgName = $svc.ToLower()
    docker build -t "$user/${imgName}:latest" -f src/$svc/Dockerfile src/$svc
    # docker push "$user/${imgName}:latest"
}
Write-Host "Listo. Descomenta el 'docker push' cuando hayas hecho 'docker login'."
