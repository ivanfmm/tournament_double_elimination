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
SCHEMA_FILE="database/init/db_script.sql"

# atajo para no repetir --context y -n en cada linea
k() { kubectl --context "$CONTEXT" -n "$NAMESPACE" "$@"; }

if [ ! -d "$OVERLAY_PATH" ]; then
    echo "Kustomize overlay not found: $OVERLAY_PATH" >&2
    exit 1
fi

if [ ! -f "$SCHEMA_FILE" ]; then
    echo "Schema file not found: $SCHEMA_FILE" >&2
    exit 1
fi

if ! kubectl config get-contexts "$CONTEXT" >/dev/null 2>&1; then
    echo "Kubernetes context not found: $CONTEXT" >&2
    exit 1
fi

# 1. El cluster responde
kubectl --context "$CONTEXT" get --raw=/readyz >/dev/null
echo "== $CONTEXT listo, desplegando $IMAGE"

# 2. Namespace y ConfigMap con el esquema (Postgres lo ejecuta al arrancar)
kubectl --context "$CONTEXT" create namespace "$NAMESPACE" --dry-run=client -o yaml \
    | kubectl --context "$CONTEXT" apply -f -
k create configmap db-init \
    --from-file=db_script.sql="$SCHEMA_FILE" \
    --dry-run=client -o yaml | k apply -f -

# 3. Manifiestos del overlay (namespace, secret, postgres, api)
kubectl --context "$CONTEXT" apply -k "$OVERLAY_PATH"

# La API necesita la base (con su esquema) arriba
k rollout status deployment/postgres --timeout=180s

# 4. Poner la imagen nueva en la API y esperar el rollout
k set image deployment/api api="$IMAGE"
k rollout status deployment/api --timeout=180s

# 5. Evidencia: que imagen quedo corriendo
echo "== Imagen desplegada en $CONTEXT:"
k get deployment api -o jsonpath='{.spec.template.spec.containers[0].image}'
echo
