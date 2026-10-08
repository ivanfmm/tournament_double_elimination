#!/usr/bin/env bash
# Despliega una imagen ya publicada en un cluster de Kubernetes.
# Uso: ./scripts/deploy.sh <context> <overlay> <image>
# Ej:  ./scripts/deploy.sh kind-dev dev localhost:5000/tournament-api:git-a1b2c3d

set -euo pipefail

if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <context> <overlay> <image>" >&2
    exit 2
fi

CONTEXT="$1"
OVERLAY="$2"
IMAGE="$3"
NAMESPACE="tournament"
OVERLAY_PATH="k8s/overlays/$OVERLAY"
MIGRATION_JOB="migration"

# atajo para no repetir --context y -n en cada linea
k() { kubectl --context "$CONTEXT" -n "$NAMESPACE" "$@"; }

if [ ! -d "$OVERLAY_PATH" ]; then
    echo "Kustomize overlay not found: $OVERLAY_PATH" >&2
    exit 1
fi

if ! kubectl config get-contexts "$CONTEXT" >/dev/null 2>&1; then
    echo "Kubernetes context not found: $CONTEXT" >&2
    exit 1
fi

# 1. El cluster responde
kubectl --context "$CONTEXT" get --raw=/readyz >/dev/null
echo "== $CONTEXT listo, desplegando $IMAGE"

# 2. Manifiestos del overlay (namespace, secret, postgres, api)
kubectl --context "$CONTEXT" apply -k "$OVERLAY_PATH"

# La migracion necesita la base arriba
k rollout status deployment/postgres --timeout=180s

# 3. Migracion ANTES de cambiar la imagen de la API: el codigo nuevo
# encuentra el esquema listo. Se recrea el Job en cada despliegue.
# No se usa 'kubectl create job' porque no permite pasar variables de
# entorno, y la imagen necesita ConnectionStrings__Default.
k delete job "$MIGRATION_JOB" --ignore-not-found --wait=true
k apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: $MIGRATION_JOB
spec:
  backoffLimit: 3
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: migrate
          image: $IMAGE
          # args (no command): se agrega al entrypoint de la imagen
          args: ["migrate"]
          env:
            - name: ConnectionStrings__Default
              valueFrom:
                secretKeyRef:
                  name: db
                  key: ConnectionStrings__Default
EOF

# 4. Esperar a que la migracion termine bien
if ! k wait --for=condition=complete --timeout=180s "job/$MIGRATION_JOB"; then
    k logs "job/$MIGRATION_JOB" --all-containers=true || true
    exit 1
fi
k logs "job/$MIGRATION_JOB" --all-containers=true

# 5. Poner la imagen nueva en la API y esperar el rollout
k set image deployment/api api="$IMAGE"
k rollout status deployment/api --timeout=180s

# 6. Evidencia: que imagen quedo corriendo
echo "== Imagen desplegada en $CONTEXT:"
k get deployment api -o jsonpath='{.spec.template.spec.containers[0].image}'
echo
