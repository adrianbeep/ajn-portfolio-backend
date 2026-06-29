```mermaid
erDiagram
	USERS {
		uuid id PK
		string password_hash
		string email
	}
	
	PROJECTS {
		uuid id PK
		string title
		string slug
		string url_thumbnail
		string description
		string content
		enum state
		timestamp created_at
		timestamp updated_at
		timestamp deleted_at
	}
```
