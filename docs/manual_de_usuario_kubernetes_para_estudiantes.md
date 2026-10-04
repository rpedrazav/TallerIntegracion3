# Manual de Usuario
## Plataforma Kubernetes para Estudiantes
**Datacenter**  
**Universidad Católica de Temuco**

---

### 1. Introducción
La plataforma Kubernetes para estudiantes permite desplegar, administrar y probar aplicaciones utilizando un entorno personal.

Cada estudiante accede utilizando su cuenta institucional y dispone de un espacio de trabajo propio.

El acceso se realiza mediante Internet y no requiere el uso de VPN.

---

### 2. Requisitos
Para utilizar la plataforma se debe contar con:
- Una cuenta institucional válida.
- Acceso a Internet.
- kubectl instalado.
- El complemento kubectl oidc-login instalado.

Puede comprobar la instalación mediante:
```bash
kubectl version --client
kubectl oidc-login --version
```

---

### 3. Acceso a la plataforma
Ingrese desde un navegador web a:  
https://estudiantes.dev.censei.cl

Presione:  
**Iniciar sesión**

Luego autentíquese utilizando su cuenta institucional.

La primera vez que ingrese, la plataforma preparará automáticamente su entorno personal.

---

### 4. Descargar el kubeconfig
Una vez iniciado sesión, descargue el archivo kubeconfig desde el portal.

Este archivo permite que kubectl se conecte a su entorno Kubernetes.

**Importante:**
- El kubeconfig es personal.
- No debe compartirse con otros usuarios.
- No debe subirse a repositorios Git.

---

### 5. Configurar kubectl
En Linux utilizando Bash o Zsh:
```bash
export KUBECONFIG=~/Descargas/estudiantes-<usuario>.kubeconfig
```

Si utiliza Fish:
```bash
set -gx KUBECONFIG ~/Descargas/estudiantes-<usuario>.kubeconfig
```

También puede utilizar el archivo directamente:
```bash
kubectl --kubeconfig estudiantes-<usuario>.kubeconfig get pods
```

---

### 6. Comprobar la conexión
Para verificar el contexto actual:
```bash
kubectl config current-context
```

Para comprobar su identidad:
```bash
kubectl auth whoami
```

Finalmente:
```bash
kubectl get pods
```

Si todavía no ha desplegado aplicaciones, puede aparecer:
```text
No resources found in student-<usuario> namespace.
```
Esto es normal e indica que la conexión funciona correctamente.

---

### 7. Entorno personal
Cada estudiante trabaja dentro de un espacio personal denominado:
```text
student-<usuario>
```
El kubeconfig descargado selecciona este espacio automáticamente.

Dentro de él puede crear recursos como:
- Pods.
- Deployments.
- Services.
- ConfigMaps.
- Secrets.
- Ingress.

Por seguridad, un estudiante no puede administrar recursos generales del clúster ni acceder a los espacios de otros usuarios.

Por ejemplo, los siguientes comandos normalmente serán rechazados:
```bash
kubectl get nodes
kubectl get namespaces
kubectl get pods -n kube-system
```
Un mensaje Forbidden en estos casos es esperado.

---

### 8. Desplegar una aplicación
Como ejemplo se desplegará un servidor Nginx.

#### 8.1. Crear el Deployment
Ejecute:
```bash
kubectl create deployment mi-web \
  --image=nginx:alpine
```

Compruebe el estado:
```bash
kubectl get deployments
kubectl get pods
```

Después de algunos segundos el Pod debería aparecer en estado:
```text
Running
```

También puede esperar el despliegue mediante:
```bash
kubectl rollout status deployment/mi-web
```

---

### 9. Crear un Service
Para permitir el acceso a la aplicación dentro de Kubernetes:
```bash
kubectl expose deployment mi-web \
  --name=mi-web \
  --port=80 \
  --target-port=80
```

Compruebe:
```bash
kubectl get services
```

---

### 10. Probar la aplicación
Puede realizar una prueba desde otro Pod:
```bash
kubectl run prueba-curl \
  --image curlimages/curl:latest \
  --restart=Never \
  --rm -it -- \
  curl -s http://mi-web
```

Si la aplicación está funcionando correctamente debería aparecer la página HTML de bienvenida de Nginx.

---

### 11. Publicar una aplicación
Para acceder a una aplicación desde Internet se utiliza un recurso Ingress.

Los dominios utilizados deben terminar en:
```text
.dev.censei.cl
```

Se recomienda incluir el nombre del proyecto y el usuario para evitar conflictos.

Ejemplos:
- `miweb-<usuario>.dev.censei.cl`
- `api-<usuario>.dev.censei.cl`
- `tarea1-<usuario>.dev.censei.cl`

#### 11.1. Crear el Ingress
Actualmente se debe utilizar el siguiente formato:
```bash
kubectl create ingress mi-web \
  --class=nginx \
  --rule="miweb-<usuario>.dev.censei.cl/*=mi-web:80" \
  --annotation="external-dns.alpha.kubernetes.io/target=proxy.inf.uct.cl"
```

Reemplace:
```text
<usuario>
```
por su identificador institucional.

La anotación mostrada en el ejemplo es requerida actualmente por la plataforma y no debe modificarse.

#### 11.2. Comprobar el Ingress
Ejecute:
```bash
kubectl get ingress
```

Debería aparecer el dominio configurado, por ejemplo:
```text
miweb-<usuario>.dev.censei.cl
```

Después de unos segundos podrá acceder a la aplicación desde Internet.

También puede comprobar el registro DNS mediante:
```bash
nslookup miweb-<usuario>.dev.censei.cl
```
o:
```bash
dig miweb-<usuario>.dev.censei.cl
```

---

### 12. HTTPS
La publicación de aplicaciones mediante Ingress se encuentra operativa.

Durante la etapa actual de implementación, algunos proyectos pueden mostrar una advertencia relacionada con el certificado HTTPS.

Esta situación no significa que el Deployment, Service o Ingress estén configurados incorrectamente.

La automatización de certificados HTTPS para los proyectos será incorporada posteriormente.

---

### 13. Comandos útiles

#### 13.1. Consultar recursos
```bash
kubectl get pods
kubectl get deployments
kubectl get services
kubectl get ingress
```

También pueden consultarse varios recursos al mismo tiempo:
```bash
kubectl get deployment,pods,service,ingress
```

#### 13.2. Información detallada
```bash
kubectl describe pod <nombre>
kubectl describe deployment <nombre>
kubectl describe service <nombre>
kubectl describe ingress <nombre>
```

#### 13.3. Ver logs
Para consultar los logs de una aplicación:
```bash
kubectl logs <nombre-del-pod>
```

Para seguirlos en tiempo real:
```bash
kubectl logs -f <nombre-del-pod>
```

#### 13.4. Ingresar a un Pod
Cuando la imagen utilizada dispone de un shell:
```bash
kubectl exec -it <nombre-del-pod> -- sh
```

---

### 14. Actualizar una aplicación
Para cambiar la imagen utilizada por un Deployment:
```bash
kubectl set image deployment/mi-web nginx=nginx:latest
```

Posteriormente:
```bash
kubectl rollout status deployment/mi-web
```

---

### 15. Eliminar una aplicación
Cuando una aplicación ya no sea necesaria, se recomienda eliminar sus recursos.

Para el ejemplo anterior:
```bash
kubectl delete ingress mi-web
kubectl delete service mi-web
kubectl delete deployment mi-web
```

Compruebe:
```bash
kubectl get deployment,service,ingress,pods
```

Si no existen otros recursos debería aparecer:
```text
No resources found in student-<usuario> namespace.
```

---

### 16. Límites de recursos
Cada estudiante dispone de una cantidad limitada de CPU, memoria y recursos.

Si se supera alguno de estos límites, Kubernetes rechazará la operación y mostrará un mensaje indicando el motivo.

Estos límites permiten compartir de forma adecuada los recursos disponibles entre los estudiantes.

---

### 17. Problemas comunes

#### 17.1. Error Forbidden
Un mensaje como:
```text
Error from server (Forbidden)
```
normalmente indica que se intentó realizar una operación fuera de los permisos asignados al estudiante.

Por ejemplo:
```bash
kubectl get nodes
kubectl get namespaces
```
son operaciones restringidas.

#### 17.2. El Pod no inicia
Primero ejecute:
```bash
kubectl get pods
```

Luego:
```bash
kubectl describe pod <nombre-del-pod>
```

Revise especialmente la sección:
```text
Events
```

#### 17.3. La aplicación no responde
Compruebe los recursos en orden:
```bash
kubectl get pods
kubectl get services
kubectl get ingress
```

El Pod debería encontrarse en estado:
```text
Running
```

---

### 18. Buenas prácticas
- No compartir el kubeconfig.
- No subir credenciales o Secrets a repositorios Git.
- Utilizar nombres descriptivos para los recursos.
- Incluir el usuario en los nombres DNS públicos para evitar conflictos.
- Eliminar recursos que ya no se utilicen.
- Revisar los logs y eventos cuando una aplicación presente problemas.

---

### 19. Flujo de trabajo recomendado
El flujo habitual para trabajar con la plataforma es:

```text
Ingresar al portal
       ↓
 Iniciar sesión
       ↓
Descargar kubeconfig
       ↓
Configurar kubectl
       ↓
 Crear Deployment
       ↓
  Crear Service
       ↓
  Crear Ingress
       ↓
Probar la aplicación
       ↓
Eliminar recursos al finalizar
```

---

### 20. Direcciones importantes

| Servicio | Dirección |
| :--- | :--- |
| Portal de estudiantes | `https://estudiantes.dev.censei.cl` |
| API Kubernetes | `https://k8s-estudiantes.dev.censei.cl` |

---

### 21. Resumen
Para utilizar la plataforma, el estudiante debe:
1. Ingresar al portal con su cuenta institucional.
2. Descargar su kubeconfig.
3. Configurar kubectl.
4. Trabajar dentro de su entorno personal.
5. Crear Deployments y Services.
6. Utilizar Ingress cuando necesite publicar una aplicación.
7. Eliminar los recursos que ya no utilice.

No es necesario utilizar VPN para acceder a la plataforma.

---
**Plataforma Kubernetes para Estudiantes**  
**Datacenter Universidad Católica de Temuco**