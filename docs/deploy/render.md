# Deploy InventoryX to Render + Supabase

Portfolio demo deployment: ASP.NET Core API on Render (free tier) with PostgreSQL on Supabase (free tier).

## Deployment pipeline overview

| Step | Method | Status |
|------|--------|--------|
| Service definition | [`render.yaml`](../render.yaml) at repo root | Ready in repo |
| Container build | Root [`Dockerfile`](../Dockerfile) | Ready in repo |
| Auto-deploy on push | Render Git integration (`autoDeployTrigger: commit`) | **Manual setup** — apply Blueprint once |
| Optional CI trigger | [`.github/workflows/render_deploy.yml`](../../.github/workflows/render_deploy.yml) | Ready — needs `RENDER_DEPLOY_HOOK` secret |
| Database | Supabase Postgres (external) | **Manual** — set connection string in Render |
| CORS origin | `Frontend__AllowedOrigins__0` | **Manual** — set after first deploy URL is known |

### Expected service URL

After Blueprint apply, the API will be available at:

**`https://inventoryx-api.onrender.com`**

(Render assigns this from the `name: inventoryx-api` field in `render.yaml`.)

---

## 0. Render MCP / CLI (optional automation)

To create or manage services from Cursor, ensure the **Render MCP** plugin is connected (OAuth or API key in MCP settings), then restart Cursor.

CLI alternative:

```bash
export RENDER_API_KEY=rnd_...   # Dashboard → Account Settings → API Keys
render workspace set
render services -o json
cd /path/to/InventoryX && render blueprints validate
```

---

## 1. Supabase (database)

1. Create a new Supabase project.
2. Go to **Project Settings → Database**.
3. Copy the **Session mode** connection string (port `5432`).
4. Ensure it includes SSL: `SSL Mode=Require;Trust Server Certificate=true`

Example format (see also [`env.example`](env.example)):

```
Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

## 2. Render (API) — one-time Blueprint setup

### Prerequisites

- Git repo on **GitHub** or **GitLab** connected to Render
- `render.yaml` and `Dockerfile` committed and pushed to the deploy branch (`main`)
- Supabase connection string ready (step 1)

### Apply Blueprint (recommended)

1. Push `render.yaml` to `main` on your connected remote.
2. Open the Blueprint wizard (replace with your repo URL):

   **GitLab (current `origin`):**
   ```
   https://dashboard.render.com/blueprint/new?repo=https://gitlab.com/little-guy-labs-group/little-guy-labs-project
   ```

   **GitHub mirror (`old-origin`):**
   ```
   https://dashboard.render.com/blueprint/new?repo=https://github.com/maestro-00/InventoryX
   ```

3. Authorize Render to access your Git provider if prompted.
4. Review the `inventoryx-api` web service (Docker, Frankfurt, free tier).
5. When prompted for `sync: false` secrets, paste values (see section 3).
6. Click **Apply** to create the service and start the first deploy.

### Deploy trigger methods

| Method | When it runs | Setup |
|--------|----------------|-------|
| **Git auto-deploy** (default) | Every push to `main` | Enabled by `autoDeployTrigger: commit` in `render.yaml` |
| **Manual** | On demand | Render Dashboard → inventoryx-api → **Manual Deploy** |
| **Deploy hook** | POST to hook URL | Dashboard → Settings → **Deploy Hook** → add URL as `RENDER_DEPLOY_HOOK` in GitHub |
| **Render CLI** | On demand | `render deploys create inventoryx-api --wait` |
| **GitHub Actions** | Push to `main` or `workflow_dispatch` | Set `RENDER_DEPLOY_HOOK` secret; see `render_deploy.yml` |

### Option B: Manual web service (without Blueprint)

1. **New → Web Service** → connect repo.
2. **Runtime**: Docker
3. **Dockerfile path**: `./Dockerfile`
4. **Plan**: Free
5. **Health check path**: `/health/ready`

## 3. Environment variables

Configured automatically by Blueprint (`render.yaml`) on first apply:

| Variable | Blueprint value | Notes |
|----------|-----------------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set in `render.yaml` |
| `DEMO_MODE` | `true` | Pre-seeds Accra Mini Mart demo |
| `Jwt__Issuer` | `InventoryX` | Set in `render.yaml` |
| `Jwt__Audience` | `InventoryX.Api` | Set in `render.yaml` |
| `Jwt__SigningKey` | Auto-generated | `generateValue: true` in `render.yaml` |
| `SWAGGER_ENABLED` | `true` | Enables `/swagger` in Production |

**You must set manually** in Render Dashboard → inventoryx-api → **Environment**:

| Variable | Required | Value |
|----------|----------|-------|
| `ConnectionStrings__DefaultConnection` | **Yes** | Supabase session connection string (step 1) |
| `Frontend__AllowedOrigins__0` | **Yes** | `https://inventoryx-api.onrender.com` (or your custom domain) |

Optional integrations:

| Variable | Notes |
|----------|-------|
| `Paystack__SecretKey` | Paystack test key for billing demos |
| `SendGridKey` | Email delivery; empty skips outbound email |
| `Authentication__Google__ClientId` | Google OAuth |
| `Authentication__Google__ClientSecret` | Google OAuth |

`PORT` is set automatically by Render. After setting `Frontend__AllowedOrigins__0`, trigger a redeploy so CORS picks up the new value.

### Generate Jwt__SigningKey locally (if not using Blueprint auto-gen)

```bash
openssl rand -base64 48
```

Paste the output as `Jwt__SigningKey` only if you created the service manually without Blueprint.

## 4. First deploy

On first boot the API will:

1. Run EF Core migrations (`InitialPostgres`) against Supabase
2. Seed roles, plans, Ghana tax treatments, adjustment reasons
3. If `DEMO_MODE=true`, seed **Accra Mini Mart** demo data

### Demo login (when DEMO_MODE=true)

| Field | Value |
|-------|-------|
| Email | `demo@inventoryx.dev` |
| Password | `Demo123!` |

Use `POST /api/v1/auth/login` or Swagger at `/swagger`.

## 5. Verify deployment

Replace `inventoryx-api.onrender.com` with your service hostname if different.

```bash
# Liveness (no DB required)
curl https://inventoryx-api.onrender.com/health/live

# Readiness (includes Postgres — Render uses this for health checks)
curl https://inventoryx-api.onrender.com/health/ready

# Swagger UI
open https://inventoryx-api.onrender.com/swagger
```

If `/health/ready` returns 503, check that `ConnectionStrings__DefaultConnection` is set and Supabase is awake.

### Post-deploy checklist

- [ ] `ConnectionStrings__DefaultConnection` set (Supabase session string with SSL)
- [ ] `Frontend__AllowedOrigins__0` set to `https://inventoryx-api.onrender.com`
- [ ] Latest deploy status is **Live** in Render Dashboard
- [ ] `/health/ready` returns 200
- [ ] Login works: `demo@inventoryx.dev` / `Demo123!` (when `DEMO_MODE=true`)

### Quickstart scenario A (manual)

1. Login as demo user (or `POST /api/v1/auth/register` for a new tenant)
2. `GET /api/v1/products` — should list 10 sample products when demo mode is on
3. `GET /api/v1/stock` — verify quantities
4. `POST /api/v1/sales` — complete a sale on the open shift

## Free-tier caveats

- **Cold starts**: Render free tier spins down after ~15 minutes idle. First request may take 30–60 seconds.
- **Background workers** (email outbox, billing, reports) pause while the service is spun down.
- **Ephemeral disk**: Do not store files on local disk; exports are stored in Postgres `bytea` columns.
- **Supabase pause**: Free projects pause after inactivity; wake via dashboard before demos.

## Local development with Postgres

```bash
docker run -d --name inventoryx-pg \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=InventoryX \
  -p 5433:5432 postgres:16

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=InventoryX;Username=postgres;Password=postgres"
export DEMO_MODE=true

dotnet ef database update \
  --project InventoryX.Infrastructure \
  --startup-project InventoryX.Presentation

dotnet run --project InventoryX.Presentation
```

Swagger: `http://localhost:8080/swagger` (or the port shown in console if `PORT` is unset).

### Future EF migrations

Always pass `--output-dir Data/Migrations` so migrations land beside the existing `InitialPostgres` migration:

```bash
dotnet ef migrations add YourMigrationName \
  --project InventoryX.Infrastructure \
  --startup-project InventoryX.Presentation \
  --output-dir Data/Migrations
```
