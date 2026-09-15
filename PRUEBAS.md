# Cómo correr y probar TournamentServices

Todo vive ahora en **un solo proyecto**: `TournamentServices.slnx`.
La carpeta `tournament_routes_postgres/` ya no se usa y se puede borrar.

---

## 0. Dónde estás parado importa

Hay **dos formas** de correr esto, y la connection string cambia según cuál uses:

| Dónde corres `dotnet` | Host de Postgres | Puerto de la API |
|---|---|---|
| Dentro del dev container (`tournament_api_dev`) — lo normal | `tournament_db` | `8080` dentro, `8081` desde Fedora |
| En Fedora/Windows directo, sin contenedor | `localhost` | `5075` |

Dentro del dev container **no tienes que configurar nada**: el
`docker-compose.dev.yml` ya define la variable de entorno
`ConnectionStrings__Default: Host=tournament_db;...`, y esa variable
**gana** sobre `appsettings.Development.json`. Por eso ese archivo dice
`localhost` y aun así funciona dentro del contenedor.

---

## 1. Fedora con Podman — levantar todo

```bash
cd ~/tu-proyecto/tournament_double_elimination
git pull

podman compose -f docker-compose.dev.yml up -d
podman ps        # deben aparecer tournament_db y tournament_api_dev
```

Si `podman compose` no existe:

```bash
sudo dnf install -y podman-compose
podman-compose -f docker-compose.dev.yml up -d
```

El `:Z` de los volúmenes ya está puesto en el compose, así que SELinux no
te va a bloquear el bind mount.

**Si el puerto 5432 está ocupado** (Postgres instalado en Fedora):

```bash
sudo ss -tlnp | grep 5432
sudo systemctl stop postgresql     # o cambia el puerto en el compose
```

---

## 2. Trabajar dentro del contenedor

En VS Code: `Ctrl+Shift+P` → *Dev Containers: Reopen in Container*.

O sin VS Code:

```bash
podman exec -it tournament_api_dev bash
cd /workspace
```

Ya adentro:

```bash
dotnet restore     # necesario: se agregó el paquete Npgsql a Api.Tests
dotnet build
```

Levantar la API:

```bash
dotnet watch run --project src/TournamentServices.Api
```

Y desde Fedora, en otra terminal:

```bash
curl http://localhost:8081/health          # -> "Services running"
```

> El código está bind-mounteado (`.:/workspace`), así que **editar un
> `.cs` no requiere reconstruir la imagen**. Solo `dotnet restore` cuando
> cambie un `.csproj`.

---

## 3. Comprobar que Postgres realmente está conectado

```bash
podman exec -it tournament_db psql -U postgres -d tournament -c "\dt"
```

Deberías ver `Teams`, `Groups`, `Matches`, `Tournaments`. Esas tablas las
crea EF Core con `EnsureCreated()` la primera vez que arranca la API.

> `database/init/db_script.sql` crea *otra* base (`tournament_db`) con
> tablas tipo documento (JSONB). La API **no** usa esas tablas. Son dos
> esquemas distintos que no chocan porque viven en bases separadas.

**Si cambian el modelo de dominio**, `EnsureCreated` no altera tablas que
ya existen. Hay que borrar el volumen:

```bash
podman compose -f docker-compose.dev.yml down
podman volume rm tournament_pgdata
podman compose -f docker-compose.dev.yml up -d
```

---

## 4. Correr las pruebas

### 4.1 Todo lo que no necesita Postgres

Dentro del contenedor:

```bash
dotnet test
```

Corre Domain, Repositories, Delegates y las rutas con EF Core **InMemory**.
Las pruebas de `Api.Tests/Postgres/` aparecen como **Skipped**.

### 4.2 Incluyendo las pruebas contra Postgres real

Define `TEST_POSTGRES`. **Ojo con el host** — dentro del contenedor es
`tournament_db`, no `localhost`:

```bash
export TEST_POSTGRES="Host=tournament_db;Port=5432;Username=postgres;Password=password"
dotnet test
```

Si corres `dotnet` fuera del contenedor (en Fedora directo):

```bash
export TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
```

Solo el ABC contra Postgres:

```bash
dotnet test --filter "FullyQualifiedName~TournamentServices.Api.Tests.Postgres"
```

Cada test crea su propia base `tournament_test_xxxxxxxxxxxx`, corre contra
ella y la borra al terminar. Nunca tocan la base `tournament` de
desarrollo, y por eso pueden correr en paralelo.

---

## 5. Qué cubre cada archivo de prueba

| Archivo | Qué prueba |
|---|---|
| `Postgres/PostgresTestBase.cs` | Infraestructura: base temporal por test, `[PostgresFact]`, `[PostgresTheory]`, helpers `PostAsync`/`GetAsync` |
| `Postgres/PostgresTeamCrudTests.cs` | ABC de **equipos**: alta, nombre duplicado (400), nombre vacío (400), cambio de nombre, 404 en cambio/baja, formato de id inválido (400) |
| `Postgres/PostgresTournamentCrudTests.cs` | ABC de **torneos**: alta con los 3 tipos de formato, PUT completo, PATCH parcial (solo nombre / solo un campo del formato), baja con cascada a grupos, 404s |
| `Postgres/PostgresGroupCrudTests.cs` | ABC de **grupos**: alta, nombre repetido sin importar mayúsculas (422), tope de `numberOfGroups` (422), renombrar, baja, y asignación de equipos (tope de `maxTeamsPerGroup`, equipo inexistente, equipo ya asignado, ids repetidos, lista vacía) |
| `Postgres/PostgresFlowTests.cs` | Un flujo end-to-end que recorre equipos → torneo → grupos → asignación → borrado en cascada |
| `Routes/*RoutesTests.cs` | Las mismas rutas pero con InMemory (rápidas, corren siempre) |

---

## 6. Probar a mano

`src/TournamentServices.Api/TournamentServices.Api.http` trae el ABC
completo encadenando ids entre requests. Cambia la primera línea según
dónde corras:

```
@host = http://localhost:8081      # API en el dev container, visto desde Fedora
@host = http://localhost:8080      # desde adentro del dev container
@host = http://localhost:5075      # dotnet run fuera de contenedor
```

---

## 7. Códigos de estado (referencia rápida)

| Código | Cuándo |
|---|---|
| `400` | Formato de id inválido, JSON mal armado, error de FluentValidation, nombre de equipo duplicado (así lo pide el PRD para `/teams`) |
| `404` | `NotFoundException` — el torneo, grupo o equipo no existe |
| `422` | `DomainValidationException` — regla de negocio (nombre de grupo repetido, se excede `maxTeamsPerGroup` o `numberOfGroups`, equipo ya asignado…) |
| `500` | Cualquier otra excepción |

El mapeo vive en `src/TournamentServices.Api/Middleware/ErrorHandlingMiddleware.cs`.
