$dockerUser = Read-Host "Ingresa tu nombre de usuario de Docker Hub (el que ves cuando entras a hub.docker.com)"
if ([string]::IsNullOrWhiteSpace($dockerUser)) {
    Write-Host "Usuario no valido. Saliendo..."
    exit
}

$services = @("tenantidentityservice", "taxcomplianceservice", "catalogpricingservice", "poscartservice")

Write-Host "Renombrando y subiendo imagenes para el usuario: $dockerUser"
foreach ($svc in $services) {
    # Renombrar (tag) la imagen antigua al nuevo usuario
    docker tag "rpedraza/${svc}:latest" "${dockerUser}/${svc}:latest"
    
    # Subir a Docker Hub
    docker push "${dockerUser}/${svc}:latest"
}

# Actualizar el YAML para que Kubernetes descargue las imagenes de TU usuario y no de 'rpedraza'
$yamlPath = ".\k8s\manifests\all-services.yaml"
$yamlContent = Get-Content $yamlPath -Raw
$yamlContent = $yamlContent -replace 'image: rpedraza/', "image: ${dockerUser}/"
Set-Content -Path $yamlPath -Value $yamlContent -Encoding UTF8

Write-Host "¡Listo! Ya puedes ejecutar .\Deploy-To-K8s.ps1"
