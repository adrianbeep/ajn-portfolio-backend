# AJN Portfolio — Frontend API Contract & Screen-to-Endpoint Guide

> **Single source of truth for frontend consumers.**  
> Aligned with backend implementation in `Api/Endpoints/*.cs` and `Application/**/*.cs`.

---

## 1. Screen-to-Endpoint Master Map

This matrix connects every frontend view/route directly to its backing endpoints, authentication requirements, and critical UI states.

| # | Screen / View | Suggested Frontend Route | Backing Endpoint(s) | Method | Auth | Primary UI States |
|---|---------------|--------------------------|---------------------|--------|------|-------------------|
| **1** | **Landing / Home** | `/` | `/projects` | `GET` | Public | Loading skeleton, Partial error, Featured items |
| **2** | **Projects Catalog** | `/projects` | `/projects?page={p}&pageSize={s}` | `GET` | Public | Loading grid, Empty state, Paged grid |
| **3** | **Project Detail** | `/projects/:slug` | `/projects/:slug` | `GET` | Public | Loading hero/content, 404 Not Found, Rendered markdown |
| **4** | **Contributions Catalog** | `/contributions` | `/contributions?page={p}&pageSize={s}` | `GET` | Public | Loading grid, Empty state, Paged cards with external links |
| **5** | **Contribution Detail** | `/contributions/:slug` | `/contributions/:slug` | `GET` | Public | Loading hero/content, 404 Not Found, External PR button, Rendered markdown |
| **6** | **Admin Login** | `/admin/login` | `/admin/auth/login` | `POST` | Public | Submitting spinner, 401 Invalid Credentials, Form validation |
| **7** | **Admin Projects Dashboard** | `/admin/projects` | `/admin/projects`<br>`/admin/projects/:slug`<br>`/admin/projects/:slug/restore` | `GET`<br>`DELETE`<br>`POST` | Admin Cookie | Loading table, Empty list, Soft-delete confirmation, Restore action |
| **8** | **Admin Project Create** | `/admin/projects/new` | `/admin/projects` | `POST` | Admin Cookie | Form validation errors, Markdown live preview, 201 Created redirect |
| **9** | **Admin Project Edit** | `/admin/projects/:slug/edit` | `/admin/projects/:slug`<br>`/admin/projects/:slug` | `GET`<br>`PUT` | Admin Cookie | Loading form, Form validation errors, Markdown live preview, 200 OK toast |
| **10** | **Admin Contributions Dashboard** | `/admin/contributions` | `/admin/contributions`<br>`/admin/contributions/:slug`<br>`/admin/contributions/:slug/restore` | `GET`<br>`DELETE`<br>`POST` | Admin Cookie | Loading table, Empty list, Soft-delete confirmation, Restore action |
| **11** | **Admin Contribution Create** | `/admin/contributions/new` | `/admin/contributions` | `POST` | Admin Cookie | Form validation errors, Markdown live preview, 201 Created redirect |
| **12** | **Admin Contribution Edit** | `/admin/contributions/:slug/edit` | `/admin/contributions/:slug`<br>`/admin/contributions/:slug` | `GET`<br>`PUT` | Admin Cookie | Loading form, Form validation errors, Markdown live preview, 200 OK toast |

---

## 2. Core Architecture Rules for Frontend

### 2.1 Base URL & Environment

| Environment | Base URL | Notes |
|-------------|----------|-------|
| **Development** | `http://localhost:5000` (or `https://localhost:5001`) | Set in frontend `.env` (`PUBLIC_API_BASE_URL`) |
| **Production** | Configured API domain (e.g., Render backend URL) | Configured with HTTPS |

> [!IMPORTANT]
> **No API version prefix exists.** Routes are mounted directly at root (e.g. `/projects`, `/admin/*`).

### 2.2 Authentication & Cookies (`credentials: "include"`)

The backend uses **HTTP-only cookie authentication** (`auth_token`).

1. **`credentials: "include"` is mandatory** on every `fetch` / `axios` call directed to `/admin/*`, including `/admin/auth/login`. Without this, the browser will neither store nor send the cookie.
2. **Never attempt to read the JWT token in JavaScript**: The cookie is `HttpOnly`, `SameSite=Strict`, and `Secure` (over HTTPS).
3. **Session Expiry Handling**: When a session expires or is missing, admin requests return `401 Unauthorized`. The frontend must implement an HTTP response interceptor to redirect the user to `/admin/login`.
4. **Logout**: Call `POST /admin/auth/logout` with `credentials: "include"` to clear the server cookie.

### 2.3 Slug Immutability (Critical Contract Detail)

- Entity `slug` is established **only during creation** (`POST`).
- On `PUT /admin/projects/{slug}` and `PUT /admin/contributions/{slug}`, the route parameter `{slug}` identifies the entity.
- The `slug` and `id` properties in update request bodies are ignored by the backend. **Slugs cannot be updated.**

### 2.4 Markdown Rendering & Security

The `content` field returned by detail endpoints is raw markdown.
- **Frontend responsibility**: You **MUST** sanitize rendered HTML (e.g., using `DOMPurify` or your framework's markdown pipeline) to prevent XSS.

### 2.5 Error Handling (RFC 7807 ProblemDetails)

All error responses return standard `ProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "ValidationFailed",
  "status": 400,
  "detail": "Title : Title is required.; Slug : Slug must be lowercase letters, numbers, and hyphens only.",
  "traceId": "0HM..."
}
```

#### Parsing Validation Errors

When `title === "ValidationFailed"`, `detail` formats errors as:  
`"{Field} : {Message}; {Field2} : {Message2}"`

Use this utility to map errors back to form inputs:

```ts
export function parseValidationErrors(detail?: string): Record<string, string> {
  if (!detail) return {};
  const errors: Record<string, string> = {};
  
  detail.split(";").forEach((chunk) => {
    const [field, message] = chunk.split(" : ");
    if (field && message) {
      // Lowercase first letter to match form field names (e.g., "Title" -> "title")
      const key = field.trim().charAt(0).toLowerCase() + field.trim().slice(1);
      errors[key] = message.trim();
    }
  });

  return errors;
}
```

#### Error Code Reference

| Status | `title` Code | Meaning / Frontend Action |
|--------|--------------|---------------------------|
| `400` | `ValidationFailed` | Display field-level errors next to form inputs. |
| `401` | `Unauthorized` | Redirect to `/admin/login`. |
| `401` | `Auth.InvalidCredentials` | Show toast / alert: "Invalid email or password". |
| `403` | `Forbidden` | Authenticated user lacks `Admin` role. Show permission error. |
| `404` | `ProjectNotFound` / `ContributionNotFound` | Render 404 view or route back to catalog. |
| `409` | `Auth.EmailAlreadyExists` | Show email error: "Email is already registered". |

### 2.6 Pagination

Both listing endpoints (`GET /projects`, `GET /contributions`) take the same two query parameters and return the same envelope.

| Parameter | Default | Range | Notes |
|-----------|---------|-------|-------|
| `page` | `1` | `1..∞` | Anything below `1` is clamped to `1`. |
| `pageSize` | `20` | `1..100` | Anything below `1` falls back to `20`; anything above `100` is clamped to `100`. |

- The response echoes the **applied** `page` and `pageSize`, not the requested ones. The client never has to guess whether its request was honoured.
- `total` is the count of **all non-deleted rows** of the catalogue, not the size of the current page.
- `totalPages = ceil(total / pageSize)`.
- Items are ordered by **`CreatedAt DESC, Id DESC`**. `CreatedAt` alone is not unique, so the `Id` tiebreaker makes the order total: a row can never appear on two pages, and no row is skipped at a page boundary. A client that stitches the pages together (or walks all of them) gets every row exactly once.

---

## 3. Screen-by-Screen Specifications

---

### Screen 1: Landing / Home (`/`)

#### Purpose
Show the intro and the most recent projects. The home does **not** list contributions: they have their own catalogue at `/contributions/`, and the home stays about projects.

#### Backing Requests
```ts
// Fetch the 4 projects previewed on the home
GET /projects?page=1&pageSize=4
```

#### Response Shapes
Returns `ProjectListDto`.

#### UI State Specifications
- **Loading**: Show skeleton cards for the project grid.
- **Empty**: If `items.length === 0`, show friendly placeholder (e.g., "No projects published yet").
- **Success**: Render thumbnail (`urlThumbnail`), title, and short description. Card links to `/projects/${slug}`.

---

### Screen 2: Projects Catalog (`/projects`)

#### Purpose
Paginated grid of all active portfolio projects.

#### Backing Request
```http
GET /projects?page={page}&pageSize={pageSize}
```
- Query Parameters:
  - `page` (number, default `1`, min `1`)
  - `pageSize` (number, default `20`, range `1..100`)

#### Success Response (200 OK)
```json
{
  "items": [
    {
      "id": "e4f8d67a-1234-4b56-8c90-abcdef123456",
      "slug": "portfolio-backend",
      "title": "AJN Portfolio Backend",
      "urlThumbnail": "https://cdn.example.com/thumbnails/backend.webp",
      "description": "Clean Architecture ASP.NET Core 10 backend with CQRS and EF Core.",
      "technologies": ["ASP.NET Core", "EF Core", "SQLite"],
      "state": "Active",
      "createdAt": "2026-09-30T18:00:00Z"
    }
  ],
  "total": 42,
  "page": 1,
  "pageSize": 20
}
```

#### UI State Specifications
- **Pagination controls**: Calculate `totalPages = Math.ceil(total / pageSize)`. Disable Prev when `page === 1`; disable Next when `page === totalPages`.
- **Empty State**: Show when `total === 0`.
- **Error State**: Show retry button on network failure.

---

### Screen 3: Project Detail (`/projects/:slug`)

#### Purpose
Full project presentation with markdown case study, tech stack, and metadata.

#### Backing Request
```http
GET /projects/{slug}
```

#### Success Response (200 OK)
```json
{
  "id": "e4f8d67a-1234-4b56-8c90-abcdef123456",
  "title": "AJN Portfolio Backend",
  "slug": "portfolio-backend",
  "urlThumbnail": "https://cdn.example.com/thumbnails/backend.webp",
  "description": "Clean Architecture ASP.NET Core 10 backend with CQRS and EF Core.",
  "technologies": ["ASP.NET Core", "EF Core", "SQLite"],
  "content": "# Overview\n\nDetailed markdown content describing the architecture...",
  "state": "Active",
  "createdAt": "2026-09-30T18:00:00Z",
  "updatedAt": "2026-10-05T14:30:00Z"
}
```

#### UI State Specifications
- **404 Not Found**: If backend returns `404 ProjectNotFound`, display a dedicated "Project not found" screen with a back-link to `/projects`.
- **Content Rendering**: Render `content` with a Markdown component + syntax highlighter. Ensure HTML sanitization is active.

---

### Screen 4: Contributions Catalog (`/contributions`)

#### Purpose
Paginated list of open-source contributions, bugfixes, and community PRs.

#### Backing Request
```http
GET /contributions?page={page}&pageSize={pageSize}
```

#### Success Response (200 OK)
```json
{
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
      "slug": "dotnet-runtime-fix",
      "title": "Fix memory leak in ThreadPool work queue",
      "urlThumbnail": "https://cdn.example.com/thumbs/dotnet.svg",
      "urlPrContribution": "https://github.com/dotnet/runtime/pull/99999",
      "description": "Resolved race condition during concurrent dequeues.",
      "state": "Active",
      "createdAt": "2026-08-15T12:00:00Z"
    }
  ],
  "total": 15,
  "page": 1,
  "pageSize": 20
}
```

#### UI State Specifications
- **External PR link**: Include a direct "View on GitHub / External" button pointing to `urlPrContribution` with `target="_blank" rel="noopener noreferrer"`.
- **Card click**: Navigates to `/contributions/:slug` for the full post-mortem / contribution writeup.

---

### Screen 5: Contribution Detail (`/contributions/:slug`)

#### Purpose
In-depth write-up of a contribution (context, challenge, solution, and learnings).

#### Backing Request
```http
GET /contributions/{slug}
```

#### Success Response (200 OK)
```json
{
  "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "title": "Fix memory leak in ThreadPool work queue",
  "slug": "dotnet-runtime-fix",
  "urlThumbnail": "https://cdn.example.com/thumbs/dotnet.svg",
  "urlPrContribution": "https://github.com/dotnet/runtime/pull/99999",
  "description": "Resolved race condition during concurrent dequeues.",
  "content": "## Background\n\nWhile profiling high-concurrency workloads...",
  "state": "Active",
  "createdAt": "2026-08-15T12:00:00Z",
  "updatedAt": null
}
```

---

### Screen 6: Admin Login (`/admin/login`)

#### Purpose
Admin authentication form (Email + Password).

#### Backing Request
```http
POST /admin/auth/login
Content-Type: application/json
```
```json
{
  "email": "admin@example.com",
  "password": "SecurePassword123!"
}
```
> [!IMPORTANT]
> The request MUST include `credentials: "include"`. The server returns `200 OK` and writes the `auth_token` HTTP-only cookie.

#### Success Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsIn...",
  "expiresAt": "2026-11-16T04:39:00Z"
}
```
*Action*: Redirect immediately to `/admin/projects`.

#### Error Responses
- `401 Unauthorized` with `title: "Auth.InvalidCredentials"`: Show inline message: *"Invalid email or password."*
- `400 Bad Request` with `title: "ValidationFailed"`: Invalid email format or missing fields.

---

### Screen 7: Admin Projects Dashboard (`/admin/projects`)

#### Purpose
Admin table listing **all** projects (both active and soft-deleted), with options to create, edit, delete, or restore.

#### Backing Requests
1. **Load projects**:
   ```http
   GET /admin/projects
   ```
   *Note: Not paginated; returns full array.*
   
2. **Soft-delete project**:
   ```http
   DELETE /admin/projects/{slug}
   ```
   *Response*: `204 No Content` on success.

3. **Restore soft-deleted project**:
   ```http
   POST /admin/projects/{slug}/restore
   ```
   *Response*: `200 OK` on success.

#### Success Response for Listing (200 OK)
```json
[
  {
    "id": "e4f8d67a-1234-4b56-8c90-abcdef123456",
    "slug": "portfolio-backend",
    "title": "AJN Portfolio Backend",
    "createdAt": "2026-09-30T18:00:00Z",
    "deletedAt": null
  },
  {
    "id": "c3d2e1f0-9876-5432-10fe-dcba98765432",
    "slug": "old-prototype",
    "title": "Legacy Prototype",
    "createdAt": "2025-01-10T09:00:00Z",
    "deletedAt": "2026-05-12T11:20:00Z"
  }
]
```

#### UI State Specifications
- If `deletedAt !== null`:
  - Show row with a visual "Archived / Soft-Deleted" badge.
  - Disable "Edit" button or mark as archived.
  - Show "Restore" action button instead of "Delete".
- If `deletedAt === null`:
  - Show "Edit" button (links to `/admin/projects/${slug}/edit`).
  - Show "Delete" button with a confirmation modal.

---

### Screen 8: Admin Project Create (`/admin/projects/new`)

#### Purpose
Form to publish a new portfolio project.

#### Backing Request
```http
POST /admin/projects
Content-Type: application/json
```
```json
{
  "title": "NextGen Payment Gateway",
  "slug": "nextgen-payment-gateway",
  "urlThumbnail": "https://cdn.example.com/thumbs/payments.png",
  "description": "High-throughput payment orchestration engine.",
  "content": "# NextGen Payments\n\nFull architecture breakdown...",
  "technologies": ["Go", "PostgreSQL", "Redis"]
}
```

#### Form Validation Rules
| Field | Required | Max Length | Format / Pattern |
|-------|----------|------------|------------------|
| `title` | Yes | 200 chars | Non-empty text |
| `slug` | Yes | 100 chars | `^[a-z0-9]+(?:-[a-z0-9]+)*$` (lowercase, numbers, single hyphens) |
| `urlThumbnail` | Yes | 2000 chars | Valid URL |
| `description` | Yes | 1000 chars | Non-empty summary |
| `content` | Yes | 50000 chars | Markdown text |
| `technologies` | No | Up to 4 items, each up to 30 chars | Free-text tags. No duplicates (case-insensitive) and no empty item after trimming |

#### Success Response (201 Created)
- Header: `Location: /projects/nextgen-payment-gateway`
- Body:
```json
{
  "id": "7b8c9d0e-1a2b-3c4d-5e6f-7a8b9c0d1e2f"
}
```
*Action*: Toast "Project created successfully!" and redirect to `/admin/projects`.

---

### Screen 9: Admin Project Edit (`/admin/projects/:slug/edit`)

#### Purpose
Form to update an existing project's title, description, thumbnail, or markdown body.

#### Backing Requests
1. **Initial Form Load**:
   ```http
   GET /admin/projects/{slug}
   ```
   Populates form fields with current data.

2. **Form Submit**:
   ```http
   PUT /admin/projects/{slug}
   Content-Type: application/json
   ```
   ```json
   {
     "id": "e4f8d67a-1234-4b56-8c90-abcdef123456",
     "title": "AJN Portfolio Backend v2",
     "slug": "portfolio-backend",
     "urlThumbnail": "https://cdn.example.com/thumbnails/backend-v2.webp",
     "description": "Updated summary of the Clean Architecture backend.",
     "content": "# Updated Content...",
     "technologies": ["ASP.NET Core", "EF Core", "SQLite"]
   }
   ```

> [!NOTE]
> The `id` and `slug` fields in the payload are ignored by the server. The project is identified solely by the `{slug}` in the URL. Disable or make the slug field read-only in the UI.

> [!NOTE]
> `PUT /admin/projects/{slug}` **replaces the whole representation**: every field missing from the payload is reset to its empty value. Omitting `technologies` therefore **clears** the stored technologies. Always send the current list explicitly if the tags should be preserved.

#### Success Response (200 OK)
Returns the updated `ProjectDetailDto`.  
*Action*: Toast "Changes saved successfully!".

---

### Screen 10: Admin Contributions Dashboard (`/admin/contributions`)

#### Purpose
Admin table listing all contributions with active/deleted indicators and quick actions.

#### Backing Requests
- **List all**: `GET /admin/contributions`
- **Soft-delete**: `DELETE /admin/contributions/{slug}` (returns `204`)
- **Restore**: `POST /admin/contributions/{slug}/restore` (returns `200`)

#### Success Response for Listing (200 OK)
```json
[
  {
    "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "slug": "dotnet-runtime-fix",
    "title": "Fix memory leak in ThreadPool work queue",
    "createdAt": "2026-08-15T12:00:00Z",
    "deletedAt": null
  }
]
```

---

### Screen 11: Admin Contribution Create (`/admin/contributions/new`)

#### Purpose
Form to register a new open-source contribution.

#### Backing Request
```http
POST /admin/contributions
Content-Type: application/json
```
```json
{
  "title": "Added OpenTelemetry Tracing to FastEndpoints",
  "slug": "fastendpoints-opentelemetry",
  "urlThumbnail": "https://cdn.example.com/thumbs/fe.png",
  "urlPrContribution": "https://github.com/FastEndpoints/FastEndpoints/pull/542",
  "description": "Implemented activity source and distributed context propagation.",
  "content": "### Problem Statement\n\nFastEndpoints lacked native trace exports..."
}
```

#### Form Validation Rules
| Field | Required | Max Length | Pattern |
|-------|----------|------------|---------|
| `title` | Yes | 200 chars | Non-empty text |
| `slug` | Yes | 100 chars | `^[a-z0-9]+(?:-[a-z0-9]+)*$` |
| `urlThumbnail` | Yes | 2000 chars | Valid URL |
| `urlPrContribution` | Yes | 2000 chars | Valid external URL |
| `description` | Yes | 1000 chars | Non-empty summary |
| `content` | Yes | 50000 chars | Markdown text |

#### Success Response (201 Created)
- Header: `Location: /contributions/fastendpoints-opentelemetry`
- Body: `{ "id": "3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f" }`

---

### Screen 12: Admin Contribution Edit (`/admin/contributions/:slug/edit`)

#### Purpose
Form to update an existing contribution.

#### Backing Requests
1. **Initial Load**: `GET /admin/contributions/{slug}`
2. **Submit Update**:
   ```http
   PUT /admin/contributions/{slug}
   Content-Type: application/json
   ```
   ```json
   {
     "id": "3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f",
     "title": "Added OpenTelemetry Tracing to FastEndpoints (v2)",
     "slug": "fastendpoints-opentelemetry",
     "urlThumbnail": "https://cdn.example.com/thumbs/fe-v2.png",
     "urlPrContribution": "https://github.com/FastEndpoints/FastEndpoints/pull/542",
     "description": "Updated description with telemetry benchmarks.",
     "content": "### Updated Content..."
   }
   ```

#### Success Response (200 OK)
Returns `ContributionDetailDto`.

---

## 4. TypeScript Definitions (Copy-Paste Ready)

Create `src/types/api.ts` in your frontend project:

```ts
/**
 * Common Types
 */
export type Guid = string;
export type ISODateTime = string;
export type EntityState = "Active" | "Inactive";

export interface PagedList<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
  instance?: string;
}

/**
 * Projects
 */
export interface ProjectSummaryDto {
  id: Guid;
  slug: string;
  title: string;
  urlThumbnail: string;
  description: string;
  technologies: string[];
  state: EntityState;
  createdAt: ISODateTime;
}

export interface ProjectDetailDto {
  id: Guid;
  title: string;
  slug: string;
  urlThumbnail: string;
  description: string;
  technologies: string[];
  content: string;
  state: EntityState;
  createdAt: ISODateTime;
  updatedAt: ISODateTime | null;
}

export type ProjectListDto = PagedList<ProjectSummaryDto>;

export interface ProjectAdminListDto {
  id: Guid;
  slug: string;
  title: string;
  createdAt: ISODateTime;
  deletedAt: ISODateTime | null;
}

export interface CreateProjectRequest {
  title: string;
  slug: string;
  urlThumbnail: string;
  description: string;
  content: string;
  technologies?: string[];
}

export interface UpdateProjectRequest {
  id: Guid;
  title: string;
  slug: string;
  urlThumbnail: string;
  description: string;
  content: string;
  technologies?: string[];
}

/**
 * Contributions
 */
export interface ContributionSummaryDto {
  id: Guid;
  slug: string;
  title: string;
  urlThumbnail: string;
  urlPrContribution: string;
  description: string;
  state: EntityState;
  createdAt: ISODateTime;
}

export interface ContributionDetailDto {
  id: Guid;
  title: string;
  slug: string;
  urlThumbnail: string;
  urlPrContribution: string;
  description: string;
  content: string;
  state: EntityState;
  createdAt: ISODateTime;
  updatedAt: ISODateTime | null;
}

export type ContributionListDto = PagedList<ContributionSummaryDto>;

export interface ContributionAdminListDto {
  id: Guid;
  slug: string;
  title: string;
  createdAt: ISODateTime;
  deletedAt: ISODateTime | null;
}

export interface CreateContributionRequest {
  title: string;
  slug: string;
  urlThumbnail: string;
  urlPrContribution: string;
  description: string;
  content: string;
}

export interface UpdateContributionRequest {
  id: Guid;
  title: string;
  slug: string;
  urlThumbnail: string;
  urlPrContribution: string;
  description: string;
  content: string;
}

/**
 * Authentication
 */
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: ISODateTime;
}

export interface LogoutResponse {
  message: string;
}

export interface CreatedResponse {
  id: Guid;
}
```

---

## 5. Frontend API Client Boilerplate

Here is a ready-to-use fetch wrapper configured with `credentials: "include"`, automatic 401 handling, and RFC 7807 error extraction:

```ts
// src/api/client.ts
import type { ProblemDetails } from "../types/api";

const API_BASE_URL = import.meta.env.PUBLIC_API_BASE_URL || "http://localhost:5000";

export class ApiError extends Error {
  constructor(public problem: ProblemDetails) {
    super(problem.detail || problem.title || "An API error occurred");
    this.name = "ApiError";
  }
}

export async function apiClient<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;

  const config: RequestInit = {
    ...options,
    credentials: "include", // Essential for HttpOnly cookie authentication
    headers: {
      "Content-Type": "application/json",
      ...options.headers,
    },
  };

  const response = await fetch(url, config);

  // Handle 204 No Content
  if (response.status === 204) {
    return undefined as unknown as T;
  }

  // Handle Errors
  if (!response.ok) {
    if (response.status === 401 && !path.includes("/admin/auth/login")) {
      // Session expired or unauthorized: redirect to login
      window.location.href = "/admin/login";
    }

    let problem: ProblemDetails;
    try {
      problem = await response.json();
    } catch {
      problem = {
        title: "HttpError",
        status: response.status,
        detail: response.statusText,
      };
    }
    throw new ApiError(problem);
  }

  return response.json();
}
```
