#!/usr/bin/env bash
set -e

# Este script se corre UNA SOLA VEZ, dentro del Dev Container
# (terminal integrada de VS Code después de "Reopen in Container"),
# parado en la raíz de tu-proyecto/, junto al .devcontainer, docker-compose*, etc.

# 1. Crea la solución vacía
# NOTA: el SDK de .NET 10 genera por defecto el formato nuevo .slnx
# (en vez del .sln clásico). Si tu SDK genera .sln en vez de .slnx,
# cambia SLN_FILE más abajo por "Tournament.sln".
dotnet new sln -n Tournament
SLN_FILE="Tournament.slnx"

# 2. Crea cada proyecto de la capa correspondiente
dotnet new webapi -n Tournament.Api            # capa de entrada (controllers, Program.cs)
dotnet new classlib -n Tournament.Domain       # entidades + interfaces de negocio
dotnet new classlib -n Tournament.Delegate     # orquesta casos de uso / servicios
dotnet new classlib -n Tournament.Repository   # implementación de acceso a datos (EF Core / Dapper)
dotnet new classlib -n Tournament.Common       # DTOs y utilidades compartidas

# 3. Agrega todos los proyectos a la solución
dotnet sln "$SLN_FILE" add \
    Tournament.Api/Tournament.Api.csproj \
    Tournament.Domain/Tournament.Domain.csproj \
    Tournament.Delegate/Tournament.Delegate.csproj \
    Tournament.Repository/Tournament.Repository.csproj \
    Tournament.Common/Tournament.Common.csproj

# 4. Define las referencias entre capas (quién puede usar a quién)
#    Api      -> Delegate, Common
#    Delegate -> Domain, Repository, Common
#    Repository -> Domain, Common
#    Domain   -> (no depende de nadie, es el centro de la arquitectura)

dotnet add Tournament.Api/Tournament.Api.csproj reference \
    Tournament.Delegate/Tournament.Delegate.csproj \
    Tournament.Common/Tournament.Common.csproj

dotnet add Tournament.Delegate/Tournament.Delegate.csproj reference \
    Tournament.Domain/Tournament.Domain.csproj \
    Tournament.Repository/Tournament.Repository.csproj \
    Tournament.Common/Tournament.Common.csproj

dotnet add Tournament.Repository/Tournament.Repository.csproj reference \
    Tournament.Domain/Tournament.Domain.csproj \
    Tournament.Common/Tournament.Common.csproj

# 5. Restaura paquetes para confirmar que todo compila
dotnet restore
dotnet build

echo ""
echo "Listo. Estructura creada:"
find . -maxdepth 1 -name "Tournament.*" -o -name "*.sln" -o -name "*.slnx" | sort
