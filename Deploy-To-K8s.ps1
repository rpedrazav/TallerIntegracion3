Write-Host "========================================="
Write-Host "Desplegando en Kubernetes (Estudiantes)"
Write-Host "========================================="

# Añadir directorio actual al PATH para que kubectl encuentre el plugin oidc-login
$env:PATH = "$PWD;" + $env:PATH

if (!(Get-Command "kubectl" -ErrorAction SilentlyContinue)) {
    Write-Host "Error: kubectl no esta instalado."
    exit
}

if (Test-Path "$HOME\Downloads\estudiantes-rpedraza.kubeconfig") {
    $env:KUBECONFIG = "$HOME\Downloads\estudiantes-rpedraza.kubeconfig"
    Write-Host "Usando kubeconfig de Descargas..."
} elseif (Test-Path "$HOME\Descargas\estudiantes-rpedraza.kubeconfig") {
    $env:KUBECONFIG = "$HOME\Descargas\estudiantes-rpedraza.kubeconfig"
    Write-Host "Usando kubeconfig de Descargas..."
} else {
    Write-Host "No se encontro el archivo kubeconfig en Descargas/Downloads."
    exit
}

Write-Host "Iniciando conexion a Kubernetes..."
kubectl apply -f .\k8s\manifests\all-services.yaml

Write-Host "Despliegue finalizado."
Write-Host "Ingresa a http://fortio-rpedraza.dev.censei.cl para agregar carga."
