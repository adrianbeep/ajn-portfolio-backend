# Runbook de Operaciones — Portfolio Backend

> Guía de operaciones para desplegar, reiniciar y resolver incidentes del Portfolio Backend.

---

## 1. Environments y URLs

| Entorno | URL API | URL Frontend | Database |
|---------|---------|--------------|----------|
| Desarrollo local | `http://localhost:5000` | `http://localhost:4321` | SQLite (`portfolio.db`) |
| Staging | `https://staging-api.portfolio.com` | `https://staging.portfolio.com` | PostgreSQL (Render) |
| Producción | `https://api.portfolio.com` | `https://portfolio.com` | PostgreSQL (Turso) |

---

## 2. Stack de Tecnologías

| Componente | Tecnología | Ubicación |
|------------|------------|-----------|
| Backend API | .NET 8 | `src/Api/` |
| Frontend | Astro SSR | `../portfolio-frontend/` |
| Database dev | SQLite | `src/Api/portfolio.db` |
| Database prod | PostgreSQL 15 | Turso (cloud) |
| CMS | Decap CMS (embebido) | Frontend |
| Deploy API | Render | — |
| Deploy frontend | Vercel | — |

---

## 3. Despliegue

### 3.1 Deploy del Backend (Render)

El deploy en Render es **automático** ante cada push a `main`.

```
Push a main
  → GitHub webhook → Render detecta push
  → Render ejecuta build (dotnet publish)
  → Render ejecuta migraciones (dotnet ef database update)
  → Render reemplaza el container
```

**Para desplegar manualmente:**

```bash
# 1. Verificar que main está actualizado
git checkout main
git pull origin main

# 2. Trigger manual en Render (dashboard)
# Ir a: Render Dashboard → Portfolio API → Deploys → "Deploy manually"

# 3. Verificar que el deploy terminó bien
# Ir a: Render Dashboard → Portfolio API → Logs
# Buscar: "Application started successfully"
```

### 3.2 Deploy del Frontend (Vercel)

同样 automático ante push a `main`.

```bash
# 1. Push a main del frontend
git checkout main
git push origin main

# 2. Vercel detecta el push y despliega automáticamente
# URL: https://portfolio-frontend.vercel.app
```

### 3.3 Migraciones de Base de Datos

**ATENCIÓN: Las migraciones corren automáticamente durante el deploy en Render.**

Para ejecutar migraciones manualmente:

```bash
# En entorno local
cd src/Api
dotnet ef database update

# Para crear una nueva migración después de cambiar el modelo
dotnet ef migrations add NombreDeLaMigracion

# Para revertir una migración (en caso de emergencia)
dotnet ef database update PreviousMigrationName
```

---

## 4. Reiniciar Servicios

### 4.1 Reiniciar la API (Render)

```bash
# Desde el dashboard de Render
# Render Dashboard → Portfolio API → Overview → "Restart"

# O vía API (si tenés el API key de Render)
curl -X POST "https://api.render.com/v1/services/{service-id}/restart" \
  -H "Authorization: Bearer {RENDER_API_KEY}"
```

### 4.2 Reiniciar el Frontend (Vercel)

```bash
# Desde el dashboard de Vercel
# Vercel Dashboard → Portfolio → Deployments → Latest → "..." → "Redeploy"

# O vía CLI
vercel --prod --force
```

### 4.3 Verificar que los servicios están levantados

```bash
# API
curl https://api.portfolio.com/api/projects

# Esperado: 200 + JSON array
# Si falla: curl -v para ver el código de error exacto

# Frontend
curl -I https://portfolio.com

# Esperado: HTTP/2 200
```

---

## 5. Logs

### 5.1 Logs de la API (Render)

```
Render Dashboard → Portfolio API → Logs
```

Filtrar por nivel:
```bash
# En el campo de búsqueda de logs
level:error     # Solo errores
level:warning   # Warnings y errores
```

### 5.2 Logs locales

```bash
# En terminal separada mientras corre localmente
dotnet run --project src/Api

# Los logs van directo a stdout en desarrollo
```

---

## 6. Variables de Entorno

### Backend (.NET)

| Variable | Descripción | Dev | Staging | Producción |
|----------|-------------|-----|---------|------------|
| `ConnectionStrings__DefaultConnection` | Connection string de DB | SQLite local | Render PostgreSQL | Turso PostgreSQL |
| `Jwt__Secret` | Secret para JWT (min 32 chars) | `dev-secret-min-32-chars!!` | (Render env var) | (Render env var) |
| `Jwt__Issuer` | Issuer del token | `localhost` | `staging-api.portfolio.com` | `api.portfolio.com` |
| `Jwt__Audience` | Audience del token | `localhost` | `staging.portfolio.com` | `portfolio.com` |
| `AllowedHosts__0` | CORS - frontend URL | `http://localhost:4321` | `https://staging.portfolio.com` | `https://portfolio.com` |

### Frontend (Vercel)

| Variable | Descripción | Valor |
|----------|-------------|-------|
| `PUBLIC_API_URL` | URL base de la API | `https://api.portfolio.com` |
| `PUBLIC_FRONTEND_URL` | URL del frontend | `https://portfolio.com` |

---

## 7. Respuesta a Incidentes

### Incidente 1: La API responde 500

**Síntoma:** `curl https://api.portfolio.com/api/projects` → 500 Internal Server Error

**Pasos:**

1. **Revisar logs** (Render Dashboard → Logs)
   - Buscar excepciones en los últimos 5 minutos
   - Filtrar: `level:error`

2. **Causa común: Database unreachable**
   ```
   Conexión a Turso caída o credenciales vencidas
   ```
   - Ir a Turso Dashboard → Databases → Verificar que la DB está activa
   - Verificar `DATABASE_URL` en Render

3. **Si el deploy más reciente falló:**
   ```
   Render Dashboard → Portfolio API → Deploys
   → Identificar el último deploy fallido
   → "Redeploy" al último commit que funcionaba
   ```

4. **Verificar que volvió:**
   ```bash
   curl https://api.portfolio.com/api/projects
   # Esperado: 200
   ```

### Incidente 2: CORS error en el browser

**Síntoma:** `Access to fetch at 'https://api.portfolio.com' from origin 'https://portfolio.com' has been blocked by CORS policy`

**Pasos:**

1. **Verificar `AllowedHosts` en appsettings.json de producción**
   ```json
   {
     "AllowedHosts": "api.portfolio.com"
   }
   ```

2. **Verificar que el frontend URL está en `AllowedHosts__0` en Render**
   ```
   Render Dashboard → Portfolio API → Environment
   → Buscar AllowedHosts__0
   → Valor debería ser: https://portfolio.com
   ```

3. **Redeploy si se cambió el config:**
   ```bash
   # Trigger redeploy desde Render dashboard
   ```

### Incidente 3: JWT no funciona (401 en requests autenticadas)

**Síntoma:** POST/PUT/DELETE devuelve 401 aunque se envía el token

**Pasos:**

1. **Verificar que el token no expiró**
   - El token JWT tiene 7 días de validez por defecto
   - Login de nuevo para obtener token fresco

2. **Verificar que `Jwt__Issuer` y `Jwt__Audience` coinciden entre dev y prod**
   - Un token emitido por `localhost` no funciona en producción

3. **Verificar que el secret es identical**
   - `Jwt__Secret` en Render debe ser igual al que se usó para emitir el token

---

## 8. Comandos de Verificación Rápida

```bash
# ¿La API está viva?
curl -f https://api.portfolio.com/api/projects || echo "API DOWN"

# ¿El frontend está vivo?
curl -f -I https://portfolio.com || echo "FRONTEND DOWN"

# ¿La DB acepta conexiones? (local)
sqlite3 src/Api/portfolio.db "SELECT COUNT(*) FROM projects;"

# ¿Hay migraciones pendientes? (local)
dotnet ef migrations list --project src/Api
```

---

## 9. Contacts

| Role | Contact | Cuando contactar |
|------|---------|-----------------|
| Desarrollador | Tu nombre | Cualquier incidente |
| Hosting Render | render.com/support | Problemas de infraestructura en Render |
| Hosting Turso | turso.tech | Problemas de base de datos |

---

## 10. Checklist Post-Incidente

Después de resolver un incidente, documentar:

- [ ] ¿Qué causó el incidente?
- [ ] ¿Cuánto tiempo duró?
- [ ] ¿Qué acciones se tomaron?
- [ ] ¿Qué se puede hacer para prevenir que vuelva a ocurrir?
- [ ] ¿Se necesita crear un ADR con la lección aprendida?

---

*Última actualización: 2026-06-21*
*Responsable: Usuario (owner del proyecto)*
