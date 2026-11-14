# UserManagementAPI

API mínima de ASP.NET Core (.NET 10) para gestionar usuarios (crear, consultar, actualizar y eliminar), protegida con un token Bearer.

> Los datos se guardan en memoria: se pierden al reiniciar la aplicación.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)

## Ejecución

La aplicación no arranca si no se configura el token de autenticación `Authentication:BearerToken`. Usa un valor aleatorio y no lo subas al repositorio.

PowerShell:

```powershell
$env:Authentication__BearerToken = "<tu-token>"
dotnet run                          # http://localhost:5095
dotnet run --launch-profile https   # https://localhost:7022
```

Bash:

```bash
Authentication__BearerToken="<tu-token>" dotnet run
```

También puedes usar user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Authentication:BearerToken" "<tu-token>"
```

En el entorno Development, el documento OpenAPI está disponible en `/openapi/v1.json` (también requiere el token).

## Autenticación

Todas las peticiones deben incluir la cabecera:

```
Authorization: Bearer <tu-token>
```

Si falta o no es válida, la API responde `401` con `{ "error": "Unauthorized." }`.

## Endpoints

| Método | Ruta                    | Descripción                  | Respuestas          |
|--------|-------------------------|------------------------------|---------------------|
| GET    | `/api/users`            | Lista todos los usuarios     | 200                 |
| GET    | `/api/users/{id}`       | Obtiene un usuario por ID    | 200, 404            |
| POST   | `/api/users`            | Crea un usuario              | 201, 400            |
| PUT    | `/api/users/{id}`       | Actualiza un usuario         | 200, 400, 404       |
| DELETE | `/api/users/{id}`       | Elimina un usuario           | 204, 404            |

`{id}` es un GUID. Cuerpo de las peticiones POST y PUT:

```json
{
  "name": "Alex Morgan",
  "email": "alex.morgan@techhive.example",
  "department": "IT"
}
```

Validación:

- `name`: obligatorio, máximo 100 caracteres.
- `email`: obligatorio, formato de correo válido, máximo 254 caracteres.
- `department`: obligatorio, máximo 100 caracteres.

Los espacios al inicio y al final se eliminan. Si la validación falla, la respuesta es `400` en formato ProblemDetails con los errores de cada campo.

Los errores no controlados devuelven `500` con `{ "error": "Internal server error." }`.

## Pruebas manuales

[UserManagementAPI.http](UserManagementAPI.http) contiene peticiones para todos los casos (401, 400, 404, CRUD y 500). Ábrelo con la extensión REST Client de VS Code o con Visual Studio y asigna a `@token` el mismo valor que configuraste en `Authentication__BearerToken`.

## Estructura

- `Program.cs`: configuración, pipeline, endpoints y validación.
- `Middleware/`: autenticación Bearer, registro de peticiones y respuestas, y manejo global de excepciones.
