# Cómo correr y probar TournamentServices

Todo vive ahora en **un solo proyecto**: `TournamentServices.slnx`.
La carpeta `tournament_routes_postgres/` ya no se usa y se puede borrar.

---

## 1. Levantar Postgres

```bash
docker compose up -d tournament_db      # o: podman compose up -d tournament_db
```

Deja la base `tournament` escuchando en `localhost:5432` con
usuario `postgres` / contraseña `password`.

> Nota: `database/init/db_script.sql` crea *otra* base (`tournament_db`) con
> tablas tipo documento (JSONB). La API **no** usa esas tablas: EF Core crea
> su propio esquema relacional (`Teams`, `Groups`, `Matches`, `Tournaments`)
> dentro de la base `tournament` la primera vez que arranca
> (`EnsureCreated()` en `Program.cs`). Si algún día cambian el modelo de
> dominio, hay que borrar la base o pasar a migraciones de EF — `EnsureCreated`
> no altera tablas que ya existen.

---

## 2. Correr la API

```bash
dotnet run --project src/TournamentServices.Api
```

- Con `ASPNETCORE_ENVIRONMENT=Development` (lo que hace `dotnet run` por
  default gracias a `launchSettings.json`) lee la connection string de
  `appsettings.Development.json` → **Postgres**.
- Si no hay connection string, cae a **InMemory** y la API arranca igual.
  Útil para probar rutas sin tener Postgres encendido.

Para probar a mano: `src/TournamentServices.Api/TournamentServices.Api.http`
ya trae el ABC completo de equipos, torneos y grupos, encadenando los ids
entre requests.

---

## 3. Correr las pruebas

### 3.1 Todo lo que no necesita Postgres

```bash
dotnet test
```

Corre Domain, Repositories, Delegates y las rutas con EF Core **InMemory**.
Las pruebas de `Api.Tests/Postgres/` aparecen como **Skipped**.

### 3.2 Incluyendo las pruebas contra Postgres real

Define `TEST_POSTGRES` y vuelve a correr:

**PowerShell (Windows)**
```powershell
$env:TEST_POSTGRES = "Host=localhost;Port=5432;Username=postgres;Password=password"
dotnet test
```

**bash**
```bash
export TEST_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=password"
dotnet test
```

Cada test crea su propia base `tournament_test_xxxxxxxxxxxx`, corre contra
ella y la borra al terminar. Nunca tocan la base `tournament` de desarrollo,
y por eso pueden correr en paralelo.

Para correr solo el ABC de Postgres:

```bash
dotnet test --filter "FullyQualifiedName~TournamentServices.Api.Tests.Postgres"
```

---

## 4. Qué cubre cada archivo de prueba

| Archivo | Qué prueba |
|---|---|
| `Postgres/PostgresTestBase.cs` | Infraestructura: base temporal por test, `[PostgresFact]`, `[PostgresTheory]`, helpers `PostAsync`/`GetAsync` |
| `Postgres/PostgresTeamCrudTests.cs` | ABC de **equipos**: alta, nombre duplicado (400), nombre vacío (400), cambio de nombre, 404 en cambio/baja, formato de id inválido (400) |
| `Postgres/PostgresTournamentCrudTests.cs` | ABC de **torneos**: alta con los 3 tipos de formato, PUT completo, PATCH parcial (solo nombre / solo un campo del formato), baja con cascada a grupos, 404s |
| `Postgres/PostgresGroupCrudTests.cs` | ABC de **grupos**: alta, nombre repetido sin importar mayúsculas (422), tope de `numberOfGroups` (422), renombrar, baja, y asignación de equipos (tope de `maxTeamsPerGroup`, equipo inexistente, equipo ya asignado, ids repetidos, lista vacía) |
| `Postgres/PostgresFlowTests.cs` | Un flujo end-to-end que recorre equipos → torneo → grupos → asignación → borrado en cascada |
| `Routes/*RoutesTests.cs` | Las mismas rutas pero con InMemory (rápidas, corren siempre) |

---

## 5. Códigos de estado (referencia rápida)

| Código | Cuándo |
|---|---|
| `400` | Formato de id inválido, JSON mal armado, error de FluentValidation, nombre de equipo duplicado (así lo pide el PRD para `/teams`) |
| `404` | `NotFoundException` — el torneo, grupo o equipo no existe |
| `422` | `DomainValidationException` — regla de negocio (nombre de grupo repetido, se excede `maxTeamsPerGroup` o `numberOfGroups`, equipo ya asignado…) |
| `500` | Cualquier otra excepción |

El mapeo vive en `src/TournamentServices.Api/Middleware/ErrorHandlingMiddleware.cs`.
