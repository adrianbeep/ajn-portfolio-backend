using Domain.Entities;

namespace Infrastructure.Persistence.Seed;

public static class ContributionSeed
{
    // Sin contribuciones sembradas, a propósito.
    //
    // Lo que había acá era un clon del seed de proyectos: los mismos GUIDs
    // (22222222-…, 33333333-…), los mismos títulos, los mismos slugs, la misma
    // fecha de creación y un contenido de relleno de una línea. La API publicaba
    // contribuciones que el dueño del sitio nunca escribió, y el frontend las
    // horneaba en el HTML como si fueran reales.
    //
    // La base tiene que reflejar la realidad: cero contribuciones.
    //
    // El catálogo se va a poblar con el CRUD de administración
    // (POST /admin/contributions), que es una pieza de trabajo posterior. Cuando
    // exista, las contribuciones se crean por la API y no por un seed: un seed con
    // contenido real se convierte en deuda que hay que migrar cada vez que el
    // texto cambia.
    // El tipo del elemento va explícito porque un inicializador vacío no
    // permite inferirlo: `new[] { }` no compila.
    public static readonly Contribution[] Data = new Contribution[] { };
}
