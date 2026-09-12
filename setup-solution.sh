#!/usr/bin/env bash
#!/usr/bin/env bash
set -e

# Este script se corre UNA SOLA VEZ, dentro del Dev Container
# (terminal integrada de VS Code después de "Reopen in Container"),
# parado en la raíz de tu-proyecto/, junto al .devcontainer, docker-compose*, etc.
#
# Estructura y nombres alineados 1:1 con el PRD (sección "Project Structure"):
#   src/TournamentServices.Api
#   src/TournamentServices.Domain
#   src/TournamentServices.Delegates      (plural)
#   src/TournamentServices.Repositories   (plural)
#   tests/TournamentServices.Api.Tests
#   tests/TournamentServices.Delegates.Tests
#   tests/TournamentServices.Repositories.Tests
#   tests/TournamentServices.Domain.Tests
#
# NOTA: el PRD no define un proyecto "Common" — los DTOs viven dentro de
# TournamentServices.Api/Dtos/. Si más adelante necesitas compartir algo
# entre capas que no sea un DTO de la Api (excepciones custom, helpers),
# valóralo entonces; no lo creamos de entrada para no desviarnos del PRD.

SLN_NAME="TournamentServices"

# 1. Crea la solución vacía
# NOTA: el SDK de .NET 10 genera por defecto el formato nuevo .slnx
# (en vez del .sln clásico). Si tu SDK genera .sln en vez de .slnx,
# cambia SLN_FILE más abajo por "${SLN_NAME}.sln".
dotnet new sln -n "$SLN_NAME"
SLN_FILE="${SLN_NAME}.slnx"

# 2. Crea cada proyecto de la capa correspondiente (src/)
dotnet new webapi   -n TournamentServices.Api          -o src/TournamentServices.Api
dotnet new classlib -n TournamentServices.Domain       -o src/TournamentServices.Domain
dotnet new classlib -n TournamentServices.Delegates    -o src/TournamentServices.Delegates
dotnet new classlib -n TournamentServices.Repositories -o src/TournamentServices.Repositories

# 3. Crea cada proyecto de tests (tests/), uno por capa, como pide el PRD
dotnet new xunit -n TournamentServices.Domain.Tests       -o tests/TournamentServices.Domain.Tests
dotnet new xunit -n TournamentServices.Delegates.Tests    -o tests/TournamentServices.Delegates.Tests
dotnet new xunit -n TournamentServices.Repositories.Tests -o tests/TournamentServices.Repositories.Tests
dotnet new xunit -n TournamentServices.Api.Tests          -o tests/TournamentServices.Api.Tests

# 4. Agrega todos los proyectos (src + tests) a la solución
dotnet sln "$SLN_FILE" add \
    src/TournamentServices.Api/TournamentServices.Api.csproj \
    src/TournamentServices.Domain/TournamentServices.Domain.csproj \
    src/TournamentServices.Delegates/TournamentServices.Delegates.csproj \
    src/TournamentServices.Repositories/TournamentServices.Repositories.csproj \
    tests/TournamentServices.Domain.Tests/TournamentServices.Domain.Tests.csproj \
    tests/TournamentServices.Delegates.Tests/TournamentServices.Delegates.Tests.csproj \
    tests/TournamentServices.Repositories.Tests/TournamentServices.Repositories.Tests.csproj \
    tests/TournamentServices.Api.Tests/TournamentServices.Api.Tests.csproj

# 5. Define las referencias entre capas de producción (quién puede usar a quién)
#    Api          -> Delegates
#    Delegates    -> Domain, Repositories
#    Repositories -> Domain
#    Domain       -> (no depende de nadie, es el centro de la arquitectura)

dotnet add src/TournamentServices.Api/TournamentServices.Api.csproj reference \
    src/TournamentServices.Delegates/TournamentServices.Delegates.csproj

dotnet add src/TournamentServices.Delegates/TournamentServices.Delegates.csproj reference \
    src/TournamentServices.Domain/TournamentServices.Domain.csproj \
    src/TournamentServices.Repositories/TournamentServices.Repositories.csproj

dotnet add src/TournamentServices.Repositories/TournamentServices.Repositories.csproj reference \
    src/TournamentServices.Domain/TournamentServices.Domain.csproj

# 6. Cada proyecto de tests referencia únicamente la capa que prueba
dotnet add tests/TournamentServices.Domain.Tests/TournamentServices.Domain.Tests.csproj reference \
    src/TournamentServices.Domain/TournamentServices.Domain.csproj

dotnet add tests/TournamentServices.Delegates.Tests/TournamentServices.Delegates.Tests.csproj reference \
    src/TournamentServices.Delegates/TournamentServices.Delegates.csproj

dotnet add tests/TournamentServices.Repositories.Tests/TournamentServices.Repositories.Tests.csproj reference \
    src/TournamentServices.Repositories/TournamentServices.Repositories.csproj

dotnet add tests/TournamentServices.Api.Tests/TournamentServices.Api.Tests.csproj reference \
    src/TournamentServices.Api/TournamentServices.Api.csproj

# 7. Paquetes NuGet que el PRD ya especifica por capa
#    (Moq para Delegates/Repositories tests, WebApplicationFactory para Api tests,
#    FluentValidation para Api, EF Core InMemory para Repositories tests)
dotnet add tests/TournamentServices.Delegates.Tests/TournamentServices.Delegates.Tests.csproj package Moq
dotnet add tests/TournamentServices.Repositories.Tests/TournamentServices.Repositories.Tests.csproj package Moq
dotnet add tests/TournamentServices.Repositories.Tests/TournamentServices.Repositories.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/TournamentServices.Api.Tests/TournamentServices.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add src/TournamentServices.Api/TournamentServices.Api.csproj package FluentValidation

# NOTA: aún no decides EF Core vs Dapper para Repositories (el PRD deja la
# puerta abierta a ambos). Cuando decidas, agrega aquí, por ejemplo:
#   dotnet add src/TournamentServices.Repositories/TournamentServices.Repositories.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
# o el paquete de Dapper + Npgsql si te vas por ese lado.

# 8. Scaffolding de carpetas internas tal como las pide el PRD
#    (git no versiona carpetas vacías, así que dejamos un .gitkeep en cada una)
mkdir -p src/TournamentServices.Api/Routes
mkdir -p src/TournamentServices.Api/Dtos
mkdir -p src/TournamentServices.Api/Validators
mkdir -p src/TournamentServices.Api/Extensions
mkdir -p src/TournamentServices.Domain/Enums

touch src/TournamentServices.Api/Routes/.gitkeep
touch src/TournamentServices.Api/Dtos/.gitkeep
touch src/TournamentServices.Api/Validators/.gitkeep
touch src/TournamentServices.Api/Extensions/.gitkeep
touch src/TournamentServices.Domain/Enums/.gitkeep

# 9. Restaura paquetes y compila para confirmar que todo está en orden
dotnet restore
dotnet build

echo ""
echo "Listo. Estructura creada:"
find src tests -maxdepth 1 -type d | sort
echo ""
find . -maxdepth 1 -name "*.sln" -o -maxdepth 1 -name "*.slnx" | sort