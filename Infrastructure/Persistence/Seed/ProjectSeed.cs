using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

public static class ProjectSeed
{
    public static readonly Project[] Data = new[]
    {
        new Project(
            id: new Guid("22222222-2222-2222-2222-222222222222"),
            createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            title: "Mi Portfolio",
            slug: "mi-portfolio",
            urlThumbnail: "https://example.com/img.png",
            description: "Sitio web de portfolio personal",
            technologies: new[] { "Astro", "TypeScript", "Vercel" },
            content: SeedText.Normalize("""
                ## El problema

                El sitio tiene que publicar contenido que vive en un backend .NET y que todavía cambia: proyectos nuevos, textos reescritos, imágenes actualizadas. La primera versión de la idea era servir el frontend contra la API en tiempo de ejecución. Se descartó por razones concretas, no por gusto técnico.

                La primera es que el contenido se lee mucho más de lo que se escribe. Un portfolio se consulta, no se administra a diario. La segunda es que cada visita a la API es una dependencia externa en el camino crítico: latencia de red, arranques en frío, caídas. La tercera es que un sitio estático se puede auditar entero antes de publicar: con el contenido horneado en el HTML, lo que CI revisa es exactamente lo que va a ver el usuario.

                A partir de esa decisión, el resto del diseño se vuelve más simple y también más estricto. No hay estado en el cliente, no hay caché de API en runtime, no hay invalidación. Hay un momento en el que se leen los datos, se transforman en HTML, y ese HTML es el producto final.

                ## Cómo está montado

                Astro 7 con `output: 'static'` y el adaptador `@astrojs/vercel`. Cada ruta se prerenderiza a HTML durante el build: la home, el listado paginado, el detalle de cada proyecto, el sitemap y el `robots.txt`. No hay SSR, no hay funciones de servidor y no hay runtime de Node en producción. El papel del adaptador es acotado: describir a Vercel cómo publicar un artefacto ya generado.

                ### Los datos entran en build time

                El frontend consume dos endpoints del contrato: `GET /projects?page&pageSize` para los resúmenes y `GET /projects/{slug}` para el detalle completo de cada proyecto. Las dos llamadas ocurren durante el build, nunca en el navegador. El HTML publicado no conserva ninguna referencia a la API de contenido.

                Como el build es un único proceso y varias rutas necesitan el mismo detalle, las respuestas se cachean por URL: el detalle de un proyecto pedido por el listado y por su propia ruta se resuelve una sola vez.

                ```ts
                const requestCache = new Map<string, Promise<unknown>>();

                function request<T>(path: string, params?: Record<string, string | number>): Promise<T> {
                  const url = apiUrl(path, params);
                  if (import.meta.env.DEV) return fetchJson<T>(url);

                  const cached = requestCache.get(url);
                  if (cached) return cached as Promise<T>;

                  const pending = fetchJson<T>(url);
                  requestCache.set(url, pending);
                  pending.catch(() => {});
                  return pending;
                }
                ```

                La caché se desactiva en desarrollo: `astro dev` no debe servir contenido viejo. Además, lo que queda cacheado es la promesa que resuelve, no la que rechaza: si una URL falló, queda cacheada con su valor de respaldo y las llamadas siguientes reciben ese mismo valor.

                ### El pipeline de markdown

                El cuerpo de cada proyecto se escribe en markdown en el panel de administración del backend y se renderiza en build time con `unified`. La cadena de plugins es corta, y cada etapa tiene una razón para estar donde está:

                | Etapa | Qué hace | Por qué en ese orden |
                | --- | --- | --- |
                | `remark-parse` | Markdown a árbol mdast | Entrada: sólo CommonMark |
                | `remark-gfm` | Tablas, listas de tareas, tachado | Extensión estándar de GFM |
                | `remark-rehype` | Pasa el árbol a hast | Corre sin `allowDangerousHtml` |
                | `rehype-sanitize` | Limpia el HTML del autor | Elimina scripts y atributos peligrosos |
                | `rehype-shiki` | Resalta el código con dos temas | Va después del sanitize |
                | `rehype-stringify` | Serializa a HTML | Salida final del pipeline |

                El orden de las dos últimas etapas se corrigió durante el desarrollo y merece explicación, porque es el tipo de detalle que no se detecta a simple vista. Shiki escribe la clase en el atributo `class` y agrega `tabindex` al `<pre>`. El sanitizador compara los nombres de los atributos de forma literal, así que cuando Shiki corría primero, el sanitizer le borraba `class="shiki"`, `class="line"` y `tabindex="0"`. Sin la clase, las reglas `.shiki` de la hoja de componentes nunca matcheaban: los bloques de código salían monocromos, sin fondo, y el `pre` con scroll horizontal no era navegable por teclado.

                Sanitizar primero y resaltar después resuelve las dos cosas a la vez. El sanitize limpia lo que escribió el autor; el markup que agrega Shiki después lo genera la aplicación, no el autor. Sigue siendo seguro porque `remark-rehype` corre sin `allowDangerousHtml`: el markdown no puede inyectar HTML crudo ni aunque lo intente.

                ### El design system como invariante, no como convención

                El CSS es vanilla: no hay Tailwind, ni PostCSS, ni CSS-in-JS. Son tres hojas de estilos, y la primera es la importante.

                - `tokens.css` es la única fuente de verdad de color, espaciado, tipografía, layout, radio y motion.
                - `base.css` hace el reset y define los valores por defecto de los elementos.
                - `components.css` define los componentes.

                La regla que hace que esto funcione no es "escribir bien CSS", sino que la regla se verifica. Un test unitario falla el build si `base.css` o `components.css` contienen un color literal o un espaciado hardcodeado:

                ```ts
                const offenders: string[] = [];
                const pattern =
                  /(?:margin|padding|gap|inset|top|left|right|bottom)[a-z-]*\s*:[^;]*\b\d+(?:\.\d+)?(?:px|rem|em)\b/;

                for (const file of SOURCE_FILES) {
                  const css = readStyle(file);
                  css.split("\n").forEach((line, index) => {
                    if (pattern.test(line)) offenders.push(`${file}: línea ${index + 1}: ${line.trim()}`);
                  });
                }

                expect(offenders, "Espaciado literal fuera de tokens.css").toEqual([]);
                ```

                El mismo test exige que todo `var(--x)` referenciado esté definido en `tokens.css`, y que todo token de color exista en los dos temas. Esto último resuelve un problema clásico de los temas: un token nuevo declarado sólo en el tema oscuro produce una variable sin valor en el tema claro y el componente se vuelve invisible. El test no compila el CSS, lo lee como texto; es más simple y falla antes.

                ```css
                :root {
                  --color-bg: #000000;
                  --color-surface: #0d0d0d;
                  --color-fg: #f5f5f4;
                  --color-accent: #ff6a3d;
                  --space-1: 0.25rem;
                  --space-4: 1rem;
                  --measure: 34rem;
                }

                [data-theme="light"] {
                  --color-bg: #fafaf7;
                  --color-surface: #ffffff;
                  --color-fg: #14110e;
                  --color-accent: #b8390a;
                }
                ```

                Los roles de color son semánticos, no descriptivos: `--color-bg`, `--color-surface`, `--color-surface-2`, `--color-fg`, `--color-fg-muted`, `--color-border`, `--color-accent`, `--color-accent-fg` y `--color-overlay`. Ningún componente sabe qué color tiene el naranja de marca; sólo sabe que existe un acento.

                Sobre ese palette hay un test de contraste que calcula el ratio WCAG de cada par de primer plano y superficie y exige 4.5:1 en los dos temas. El resultado no es un sitio bonito por construcción: es un sitio que no puede bajar de un umbral de legibilidad sin romper el build.

                | Grupo de tokens | Qué controla | Regla asociada |
                | --- | --- | --- |
                | Color | Fondo, superficies, texto, acento | Declarado en tema oscuro y en tema claro |
                | Espaciado | Escala de 4px, de `--space-1` a `--space-32` | Sólo existe en `tokens.css` |
                | Tipografía | Escala de texto, tracking y line-height | Un valor por rol |
                | Layout | Ancho de columna, medida, alto del header | Cambia la retícula, no el componente |
                | Forma y motion | Radios, duraciones, curvas | Transiciones homogéneas |

                ### Tema sin parpadeo y cero JavaScript

                El tema se resuelve con un atributo `data-theme` en `<html>`, con los valores `dark` y `light`. El script inline que lo decide va en el `<head>` y se ejecuta antes del primer pintado, con un orden de resolución explícito: `localStorage`, después `prefers-color-scheme`, y dark como último recurso. Eso elimina el flash de tema incorrecto, que es el defecto típico de este patrón.

                El sitio no empaqueta JavaScript. Cero bundles, cero framework en el cliente. Las dos únicas piezas de script son dos bloques inline: el que resuelve el tema y el que sincroniza el toggle. Todo lo demás es HTML y CSS.

                ## Decisiones que no son obvias

                ### Un build que no falla cuando la API cae

                Si el build aborta cuando la API no responde, un backend caído deja al frontend sin deploy, y el efecto se multiplica: un incidente del backend termina impactando al sitio público. Si el build publica un sitio vacío, un fallo del backend degrada el contenido pero no la disponibilidad.

                Se eligió la segunda opción: el build siempre termina, publica el sitio sin contenido de proyectos y escribe un warning en stderr. La warning es visible en el log de CI y una URL que falló no se reintenta, para no llenar el log de ruido.

                El riesgo de esta decisión es obvio: si el build degrada en silencio, un sitio vacío se publica sin que nadie se entere. La mitigación es que la degradación sea ruidosa y explícita: un warning en stderr, un estado vacío en el listado para que la página no rompa, y un job de CI dedicado que comprueba que el build con la API inaccesible produce exactamente eso.

                > Un backend caído no puede dejar al frontend sin deploy: el fallo de una dependencia se convierte en un warning, nunca en un build abortado.

                Los tres jobs de CI cubren casos distintos y ninguno es redundante:

                | Job de CI | Qué demuestra |
                | --- | --- |
                | Contenedor | Typecheck, tests unitarios y build hermético contra la API stub |
                | E2E | El sitio compilado pasa la suite completa de Playwright |
                | `build-without-api` | El build publica un sitio vacío con un warning, sin fallar |

                ### La paginación es real, no decorativa

                El listado no simula paginación partiendo un array en el cliente. La API devuelve un envelope con `items`, `total`, `page` y `pageSize`, y el frontend pide al backend la página exacta que va a prerenderizar, así que `/projects/2/` es una ruta de verdad. El sitio muestra diez proyectos por página: cuando el catálogo crece, la segunda página aparece sola, sin fabricar datos.

                La diferencia importa por una razón concreta: una paginación simulada en el cliente nunca falla y, por lo tanto, nunca se prueba. La paginación real puede devolver una página vacía, puede pedir una página que no existe, y todo eso ocurre durante el build, donde un error se traduce en un artefacto incorrecto y detectable.

                ### El hero es un contrato, no una preferencia visual

                El hero tiene que ocupar exactamente una pantalla. No es una preferencia estética: es una regla con un test detrás. Un caso e2e mide el borde inferior del hero y exige que caiga a menos de medio píxel del alto del viewport.

                La media precisión no es caprichosa. El alto del header está definido como un token (`--header-height`) y el hero se calcula como el viewport menos ese token. Sin el test, cualquier cambio en el header desalinearía la primera pantalla y nadie lo notaría hasta que se viera en un monitor con otra resolución.

                ## Qué aprendí

                Tres cosas, en orden de utilidad.

                La primera es que una invariante de diseño que depende de la disciplina de cada pull request no es una invariante: es una sugerencia. La regla de que el color literal vive sólo en `tokens.css` ya no se cumple por confianza, sino porque hay un test que rompe el build.

                La segunda es que el orden de los plugins no es un detalle de implementación. Sanitizar y resaltar parecían intercambiables por nombre, pero no lo eran: uno produce el markup que el otro necesita, y el sanitizador, por diseño, borra atributos en lugar de entenderlos. El bug era invisible a simple vista porque el sitio no rompía; sólo perdía el resaltado y la navegación por teclado.

                La tercera es que el build es parte del producto: el comportamiento en build time tiene que estar tan cuidado como el de runtime, y esa decisión necesita su propio job en CI para que nadie la revierta sin darse cuenta.
                """)),

        new Project(
            id: new Guid("33333333-3333-3333-3333-333333333333"),
            createdAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            title: "API de Gestión",
            slug: "api-gestion",
            urlThumbnail: "https://example.com/api.png",
            description: "API REST para gestión de proyectos",
            technologies: new[] { "ASP.NET Core", "EF Core", "SQLite" },
            content: SeedText.Normalize("""
                ## El problema

                El backend de este portfolio tiene un solo consumidor conocido: el frontend que lo consume en build time. Eso parece una simplificación y en realidad es la decisión de diseño que ordena todo lo demás.

                Si el consumidor es uno y es conocido, el contrato puede ser explícito y estricto. No hace falta un formato genérico, ni una API "flexible", ni un sobre de metadatos para un cliente futuro que no existe. Hace falta un contrato escrito, versionado en un documento, y respetado por los dos lados.

                El contrato vive en `docs/api-contract.md` y es la fuente de verdad compartida entre backend y frontend. Cuando el frontend declara sus tipos, no los inventa: los deriva del contrato, y cuando el contrato cambia, el cambio se propaga de forma explícita en los dos proyectos. La otra mitad de esa fuente de verdad es `Infrastructure/Persistence/Seed/ProjectSeed.cs`, que siembra los proyectos que el backend expone.

                ## El contrato como fuente de verdad

                El backend expone dos endpoints:

                | Endpoint | Devuelve | Uso |
                | --- | --- | --- |
                | `GET /projects` | Envelope paginado | Listado de tarjetas |
                | `GET /projects/{slug}` | Detalle en markdown crudo | Ficha de proyecto |

                El listado acepta `page` y `pageSize` como parámetros de consulta. Devuelve resúmenes: identificador, slug, título, miniatura, descripción, tecnologías, estado y fecha. El detalle devuelve el proyecto completo, incluido el cuerpo en markdown. La separación no es estética: permite que el listado dibuje las tarjetas sin cargar el cuerpo de ningún proyecto.

                ### La paginación es un envelope explícito

                El listado no devuelve un array pelado. Devuelve un objeto con cuatro campos:

                ```json
                {
                  "items": [
                    {
                      "id": "22222222-2222-2222-2222-222222222222",
                      "slug": "mi-portfolio",
                      "title": "Mi Portfolio",
                      "urlThumbnail": "https://example.com/img.png",
                      "description": "Sitio web de portfolio personal",
                      "technologies": ["Astro", "TypeScript", "Vercel"],
                      "state": "Active",
                      "createdAt": "2026-01-01T00:00:00Z"
                    }
                  ],
                  "total": 2,
                  "page": 1,
                  "pageSize": 20
                }
                ```

                La razón de meter el array dentro de un envelope es que un array no puede decir cuándo se acaba. Con un array, el cliente deduce el total contando elementos, y si la página actual tiene exactamente `pageSize` elementos no sabe si hay una página siguiente o si simplemente se terminó. Peor: para saber cuántas páginas hay, el cliente tiene que pedir la última página o una página vacía, lo que convierte el listado en varias peticiones.

                Con `total` explícito, el cliente sabe cuántas páginas hay antes de pedir la segunda, y el frontend puede construir la ruta `/projects/2/` sin adivinar. Es la diferencia entre una API que obliga a adivinar y una que describe su propia forma.

                Los límites también son explícitos: `pageSize` tiene un valor por defecto de 20 y un máximo de 100. El backend acota el valor pedido y devuelve el `pageSize` que realmente aplicó, de modo que el cliente nunca tiene que suponer si su petición fue respetada.

                ```csharp
                // GET /projects?page=1&pageSize=20
                app.MapGet("/projects", async (
                    int page = 1,
                    int pageSize = 20,
                    CancellationToken ct = default) =>
                {
                    var result = await query.ExecuteAsync(page, pageSize, ct);

                    return Results.Ok(new ProjectListDto(
                        Items: result.Items.Select(ProjectSummaryDto.FromEntity).ToList(),
                        Total: result.Total,
                        Page: result.Page,
                        PageSize: result.PageSize));
                })
                .WithName("GetProjects");
                ```

                ### El cuerpo queda en markdown crudo

                La decisión más discutida del contrato es que `content` se almacena y se devuelve como markdown crudo, sin transformar. El backend no lo renderiza, no lo escapa y no lo interpreta.

                Hay una tentación natural de hacerlo: si el backend devuelve HTML, el frontend no necesita pipeline de markdown y se simplifica el cliente. La tentación es incorrecta, por una razón que conviene enunciar con precisión: renderizar en el servidor no elimina el problema, sólo cambia quién lo tiene.

                Un backend que devuelve HTML tiene que decidir, en el momento de renderizar, qué sanitización aplica, y ese sanitizador pasa a formar parte del contrato: cada vulnerabilidad suya es una vulnerabilidad del API. Si además el HTML renderizado se guarda en la base, un fallo de sanitización queda congelado en los datos y hay que corregirlo con una migración.

                Dejando el markdown crudo, cada consumidor elige su propio pipeline y aplica sus propias reglas. El frontend usa `unified` con `remark-parse`, `remark-gfm`, `remark-rehype`, `rehype-sanitize` y `rehype-shiki`, y descarta el HTML crudo porque `remark-rehype` corre sin `allowDangerousHtml`. El markdown sigue siendo un documento: se puede revisar, versionar, diffear y volver a renderizar con reglas nuevas.

                > El backend guarda documentos, no páginas. Quien decide cómo se ven es el frontend.

                Lo que el backend sí hace es acotar el tamaño de los campos, porque un límite de longitud es una cuestión de almacenamiento y de contrato, no de presentación:

                | Campo | Límite | Motivo |
                | --- | --- | --- |
                | `content` | 50.000 caracteres | Cuerpo largo en markdown |
                | `description` | 1.000 caracteres | Texto de la tarjeta |
                | `title` | 200 caracteres | Título del proyecto |
                | `slug` | 100 caracteres | Segmento de URL |

                Validar el largo y no el formato es la línea que separa este backend de un editor de markdown. El backend garantiza que el dato entra bien; el frontend garantiza que se muestra bien.

                ### Los errores también son contrato

                Un backend que devuelve los errores con un formato propio obliga a cada cliente a aprender ese formato. ASP.NET Core ya define uno: `ProblemDetails`. Adoptarlo no es una decisión estética, es una decisión de interoperabilidad.

                ```csharp
                // GET /projects/{slug}
                app.MapGet("/projects/{slug}", async (string slug, CancellationToken ct) =>
                {
                    var project = await repository.FindBySlugAsync(slug, ct);

                    if (project is null)
                    {
                        return Results.Problem(
                            title: "Not Found",
                            statusCode: StatusCodes.Status404NotFound,
                            detail: $"Project '{slug}' was not found.");
                    }

                    return Results.Ok(ProjectDetailDto.FromEntity(project));
                })
                .WithName("GetProjectBySlug");
                ```

                La respuesta de error tiene una forma estable:

                ```json
                {
                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                  "title": "Not Found",
                  "status": 404,
                  "detail": "Project 'no-existe' was not found."
                }
                ```

                Para el frontend esto no es un detalle: es lo que le permite distinguir "este proyecto no existe" de "la API está caída". Son dos situaciones que exigen comportamientos opuestos. Un 404 significa que la ruta no se genera. Un error de conexión significa que el build tiene que sobrevivir, publicar el sitio sin ese contenido y avisar con un warning. Sin un formato de error distinguible, el cliente no puede tomar la decisión correcta.

                ## Decisiones que no son obvias

                ### El tradeoff de traer el contenido en build time

                El frontend es un sitio estático: todo el contenido se pide durante el build y queda horneado en el HTML. La ventaja es evidente, hay menos dependencias en tiempo de ejecución y el artefacto se puede auditar entero antes de publicar. El costo también es evidente: cada cambio en el backend necesita un rebuild para verse reflejado en el sitio.

                Ese costo es aceptable para un portfolio, donde el contenido cambia unas pocas veces al mes y no varias veces por minuto. Para un sistema con datos que cambian solos, el mismo contrato seguiría sirviendo, pero el frontend tendría que pasar a consultar la API en el navegador o a revalidar por TTL. El contrato ya está preparado para eso: el envelope de paginación, el `slug` como identificador estable y los `ProblemDetails` son exactamente lo que necesita un cliente que decide cuándo pedir.

                Lo relevante es que el backend no se modificó para acomodar al frontend, ni al revés. El contrato no tiene una rama "cuando el cliente es estático". Esa es la prueba de que el diseño no quedó amarrado a un solo consumidor.

                ### Un backend que no conoce a su cliente

                El backend no tiene ninguna lógica condicionada por "el frontend necesita esto". No sabe si el consumidor es el sitio, un exportador o un script. Esa ignorancia es deliberada: es lo que permite que el contrato signifique algo.

                La consecuencia práctica es que la lógica de negocio se mantiene en el backend y la de presentación en el frontend, sin que ninguna de las dos se escape de su capa. El backend no sabe que existe markdown, ni un sanitizador, ni un resaltador de sintaxis. El frontend no sabe cómo se persiste un proyecto, ni qué tabla lo guarda, ni cómo se calcula el total de una página.

                ### El frontend decide qué hacer con un error

                El frontend, como sitio estático, no puede "reintentar más tarde" ni "mostrar un error al usuario". Su única ventana de acción es el build. Por eso el backend no necesita un mecanismo de reintentos: quien reintenta es el pipeline de CI, que vuelve a construir.

                Esta asimetría es la que hace que el contrato de errores importe tanto. El frontend necesita razonar sobre el tipo de fallo sin adivinar, y `ProblemDetails` le da un `status` con el que trabajar.

                ## Qué aprendí

                La primera lección es que "dónde va el markdown" es una decisión de arquitectura, no de presentación. Si el backend renderiza, cada cliente hereda el sanitizador del backend y cada error de sanitización se convierte en un problema de seguridad compartido. Si el backend guarda crudo, cada cliente elige sus reglas y el dato sigue siendo un documento.

                La segunda es que la paginación necesita un envelope. Un array no puede describir su propia completitud, y obligar al cliente a adivinar cuántas páginas hay es una forma de deuda que se paga en peticiones de más y en rutas rotas.

                La tercera es que un formato de error estándar vale más que un error bien documentado. `ProblemDetails` no es más expresivo que un JSON propio; lo importante es que cualquier cliente HTTP sabe interpretarlo sin un documento adicional.

                Y la cuarta, la más incómoda: los límites de longitud parecían un detalle de validación, pero son los que mantienen la frontera entre un backend que almacena contenido y un backend que lo interpreta. Al no validar el formato, el backend se mantiene pequeño, y ese es exactamente el objetivo.
                """)),
    };
}
