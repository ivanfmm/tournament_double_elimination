# Ingress con Traefik

Alternativa al acceso directo por NodePort de la API. Usa la API estándar de
Ingress, el controlador Traefik y ruteo por hostname.

```
http://tournaments.local:8080
  -> 127.0.0.1:8080 (host)
  -> nodePort 30081 (nodo kind)
  -> Traefik, entrypoint "web"
  -> Service tournaments-api:8080
  -> Pods de la API
```

Requisitos: kind, kubectl, Helm 3+. Todo se corre desde la raíz del repo.

## Archivos

| Archivo | Qué es |
|---|---|
| `kind-traefik.yml` | Cluster kind con 8080 -> 30081 (Traefik), 3000 (Grafana), 9090 (Prometheus) |
| `kubernetes/traefik/values.yml` | Valores de Helm para Traefik (**no** se aplica con kubectl) |
| `kubernetes/traefik/api-ingress.yml` | Ingress `tournaments-api` para el host `tournaments.local` |

## 1. Crear el cluster con el mapeo de Ingress

kind no permite agregar puertos a un cluster existente. Los puertos 8080, 3000
y 9090 del host deben estar libres. Si vas a borrar un cluster anterior,
respalda primero los datos de la base.

```powershell
$env:KIND_CLUSTER = "tournaments-ingress"
kind create cluster --name $env:KIND_CLUSTER --config kind-traefik.yml
kubectl config use-context "kind-$env:KIND_CLUSTER"
```

Después sigue los pasos normales del proyecto sobre este cluster (PostgreSQL,
construir y cargar imágenes, Metrics Server, desplegar `kubernetes/*.yml`),
**sin** crear otro cluster con `kind.yml`.

## 2. Instalar Traefik

```powershell
helm repo add traefik https://traefik.github.io/charts
helm repo update
helm search repo traefik/traefik --versions
```

Elige una versión del chart compatible con tu versión de Kubernetes y anótala:

```powershell
$env:TRAEFIK_CHART_VERSION = "<version>"
helm upgrade --install traefik traefik/traefik --version $env:TRAEFIK_CHART_VERSION --namespace traefik --create-namespace --values kubernetes/traefik/values.yml --wait --timeout 5m
```

Verificar:

```powershell
kubectl -n traefik rollout status deployment/traefik --timeout=180s
kubectl get ingressclass traefik
kubectl -n traefik get service traefik
```

El IngressClass `traefik` es explícito (no es el default), el dashboard no se
expone y solo hay HTTP.

## 3. Aplicar la ruta de la aplicación

Con el Service `tournaments-api` ya creado:

```powershell
kubectl apply -f kubernetes/traefik/api-ingress.yml
kubectl -n tournaments get ingress tournaments-api
```

El Service de la API se queda como NodePort para conservar el acceso directo;
Traefik lo alcanza por dentro del cluster. Si quieres dejarlo solo por Ingress,
cambia `kubernetes/api.yml` a `type: ClusterIP` y quita `nodePort`.

## 4. Acceder

Agrega al archivo hosts (`C:\Windows\System32\drivers\etc\hosts` en Windows,
`/etc/hosts` en Linux/macOS, requiere administrador):

```
127.0.0.1 tournaments.local
```

Abre `http://tournaments.local:8080` (la interfaz web) o prueba sin editar hosts:

```powershell
curl.exe --fail --resolve tournaments.local:8080:127.0.0.1 http://tournaments.local:8080/health
curl.exe --fail --resolve tournaments.local:8080:127.0.0.1 http://tournaments.local:8080/teams
```

`localhost:8080` **no** coincide con la regla de host y Traefik responde 404.
Usa `tournaments.local` o manda el header `Host`.

Para el loadtest apunta a Traefik:

```powershell
locust -f loadtest/locustfile.py --host http://tournaments.local:8080
```

El ServiceMonitor de Prometheus usa el Service interno, no cambia. Grafana
(3000) y Prometheus (9090) no pasan por Traefik.

### Cluster existente sin el mapeo

Para probar el ruteo sin recrear el cluster (no es persistente). Detén antes
cualquier port-forward que use el 8080:

```powershell
kubectl -n traefik port-forward service/traefik 8080:80
```

## Problemas comunes

```powershell
kubectl -n traefik logs deployment/traefik --tail=100
kubectl -n tournaments describe ingress tournaments-api
kubectl -n tournaments get endpointslices -l kubernetes.io/service-name=tournaments-api
```

- **Connection refused**: revisar el mapeo de puertos del cluster y que Traefik esté listo.
- **404 de Traefik**: revisar el header Host y el IngressClass.
- **502/503**: revisar readiness de los Pods, selectores del Service y puerto del backend.
- **ADDRESS vacío en el Ingress**: normal con NodePort local; prueba el ruteo.
- **Error de schema en Helm**: `helm show values traefik/traefik --version $env:TRAEFIK_CHART_VERSION`.
  No hagas `kubectl apply` a toda la carpeta `kubernetes/traefik/`.
