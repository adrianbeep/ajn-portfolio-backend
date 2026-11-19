namespace Domain.ValueObjects;

// Una violacion de las reglas de las tecnologias, sin lanzar excepcion.
// Index es el indice del elemento que incumple la regla, o null cuando la
// violacion es de la coleccion entera (demasiados elementos o duplicados).
public sealed record TechnologyViolation(int? Index, string Message);
