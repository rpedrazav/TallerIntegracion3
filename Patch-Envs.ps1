$env:PATH = "$PWD;" + $env:PATH
$env:KUBECONFIG = "C:\Users\kkk\Downloads\estudiantes-rpedraza.kubeconfig"

Write-Host "Forzando modo Development en los microservicios para migrar las bases de datos..." -ForegroundColor Cyan

kubectl set env deployment/tenantidentityservice ASPNETCORE_ENVIRONMENT=Development
kubectl set env deployment/taxcomplianceservice ASPNETCORE_ENVIRONMENT=Development
kubectl set env deployment/catalogpricingservice ASPNETCORE_ENVIRONMENT=Development
kubectl set env deployment/poscartservice ASPNETCORE_ENVIRONMENT=Development

Write-Host "Reiniciando pods para aplicar cambios..." -ForegroundColor Cyan
kubectl rollout restart deployment/tenantidentityservice
kubectl rollout restart deployment/taxcomplianceservice
kubectl rollout restart deployment/catalogpricingservice
kubectl rollout restart deployment/poscartservice

Write-Host "Listo. Espera unos 15 segundos a que los pods inicien y las tablas se creen." -ForegroundColor Green
