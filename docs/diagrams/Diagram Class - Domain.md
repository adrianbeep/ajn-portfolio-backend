Diagrama de clases del dominio. Refleja el estado actual del código:
`Domain/Entities/Project.cs`, `Domain/Entities/Contribution.cs`, `Domain/Enums/State.cs`
y `Domain/ValueObjects/TechnologySet.cs`.

El enum de estado es `Domain.Enums.State` con los valores `Active` e `Inactive`.
`Project` y `Contribution` lo usan para el estado de publicación; `ApplicationUser`
hereda de `IdentityUser` (ASP.NET Core Identity) y no tiene relación con los proyectos
ni con las contribuciones.

`TechnologySet` es un value object: es la única fuente de las reglas de las tecnologías
de un proyecto (cuántas caben, cuánto mide cada una, que no se repitan) y `Project` no
puede guardar un set inválido porque las aplica siempre a través de esa clase.

```mermaid
classDiagram
	class Project {
		<<entity>>
		Guid Id
		string Title
		string Slug
		string UrlThumbnail
		string Description
		string Content
		State State
		datetime CreatedAt
		datetime? UpdatedAt
		datetime? DeletedAt
		IReadOnlyList~string~ Technologies
	}
	
	class Contribution {
		<<entity>>
		Guid Id
		string Title
		string Slug
		string UrlThumbnail
		string UrlPrContribution
		string Description
		string Content
		State State
		datetime CreatedAt
		datetime? UpdatedAt
		datetime? DeletedAt
	}
	
	class State {
		<<enum>>
		Active
		Inactive
	}
	
	class TechnologySet {
		<<value object>>
		int MaxCount
		int MaxLength
		IReadOnlyList~string~ Items
	}
	
	class ApplicationUser {
		<<entity>>
	}
	
	IdentityUser <|-- ApplicationUser : hereda
	
	class IdentityUser {
		<<framework>>
	}
	
	Project --> State : State
	Contribution --> State : State
	Project ..> TechnologySet : reglas de Technologies
```
