# Aeon Registry API
To learn more about minimal APIs in .NET 10, I followed this Udemy course: https://www.udemy.com/course/minimal-api-net10.

## Requirements
- .NET Core 10 SDK
- Entity Framework Core CLI
  - Run `dotnet tool install --global dotnet-ef`
- PostgreSQL and pgAdmin installed locally

## Project setup
### Database creation
In the pgAdmin app, create a new database called `AeonRegistry` using default settings.

### Connection string
The `.csproj` file makes use of `<UserSecretsId>` to manage the DB connection string. The existing GUID there points to `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json`. To set the value locally run `dotnet user-secrets set "ConnectionStrings:DbConnection" "Host=localhost;Port=5432;Database=AeonRegistry;User Id=postgres;Password=PutYourPasswordHere"`. Adjust the `Port` and `Password` values to match your local DB values.

If the directory does not exist on your machine, remove the `<UserSecretsId>` element from the `.csproj` file, create the `secrets.json` file at the project root as shown below, then run `dotnet user-secrets init` to add the appropriate element to your project file.
```
{
  "ConnectionStrings": {
    "DbConnection": "Host=localhost;Port=5432;Database=AeonRegistry;User Id=postgres;Password=PutYourPasswordHere"
  }
}
```

### Scaffolding the database
Run `dotnet ef database update` to apply the migrations to the new (or existing) database.

## Seed data
On startup a seed utility will run migrations to ensure the DB is current, then check if the database is empty. If it's empty example data will be populated (see `Data/SeedData/`).

## Running the project
Run `dotnet restore` then `dotnet run`.

## User management
This project leverages ASP.NET Identity to handle authentication and authorization. The user information is stored as part of the local Postgres DB. This API makes use some of built-in endpoints to interact with accounts. (Note: To demonstrate how to extend the Identity functionality -- in this case we add two new properties for first and last name -- the existing ones are hidden so that we can provide new ones with slightly different names.)

Note: The seed data populates several sample users if you don't want to create one yourself.

### Logging in
From the Swagger interface, use `/api/auth/login`. A successful login will return a bearer token that you can use in the Swagger interface to authenticate with.

### Registering a user and resetting a password
From the Swagger interface, use `/api/auth/register-admin` to create a user. As long as the user doesn't already exist, it will be created with a temporary password that's hard-coded (see `CustomIdentityEndpoints.cs`). That user will also be assigned the role of `Researcher` (if said role exists). Because there's no real email service (it just writes to the console), the reset password code will be written to the console. Use the reset code written to the console to call `/api/auth/reset-password` to reset the password.

**Warnings:** There are some elements from the Udemy video I typed verbatim that are inconsistent. Maybe these will be addressed in future segments.
- Why is the endpoint called `register-admin`? Is it to register a new administrator, or is it only supposed to be called by an administrator?
- Registering a user writes the content of a would-be email to the new user, and that content contains a password reset link. Perhaps the presenter included this to show how you would start setting it up, but the link **does nothing**.

### Forgot password
From the Swagger interface, use `/api/auth/forgot-password` to initiate the password reset flow. The password reset token is written to the console, which can then be used with `/api/auth/reset-password`.

## Entity Framework reminders
- To create a migration: `dotnet ef migrations add {NameOfMigrationHere}`.
- To run migrations: `dotnet ef database update`.

## Product notes and future ideas
The API is incomplete. The Udemy course covered the basic mechanics of getting data in and out of the system. However, there are certain entities (such as Catalog Records) that don't have endpoints.

The seed data defines user roles for role-based access control (RBAC); however, the course never made use of those. I created an example endpoint (`/api/private/sites/{id}/archive`) that demonstrates how this works.

Something I would have done differently at the beginning of the project was have the database (and admin interface) hosted in a container rather than requiring those tools to be explicitly installed locally as the course required.

## Project structure and conventions

## API conventions
Endpoints are grouped by entity and access. Although the routes to public and private endpoints differ, this grouping makes the Swagger interface easier to scan. Another advantage for implementation is that certain attributes can be applied at the group level rather than having to remember to apply to each endpoint (e.g., `.RequiresAuthorization()`, `.WithTags()`).

### Groups
Define with `.WithTags()`.

- Use plural nouns.
- If there are public and private endpoints for a particular entity, create two groups with the appropriate suffix (e.g., "Sites - Public" and "Sites - Private").

### Summary
Define with `.WithSummary()`.

- One short phrase, no trailing period.
- Start with an imperative verb, sentence case (capitalize only the first word and proper nouns).
  - GET (single): "Retrieve"
  - GET (collection): "List"
  - POST (create): "Create"
  - POST (non-CRUD action): The domain verb itself (e.g., "Cancel", "Archive")
  - PUT: "Update"
  - DELETE: "Delete"
- Keep it under 80 characters. It should name the single primary action only.

### Description
Define with `.WithDescription()`.

- One or more complete sentences, each ending in a period.
- Present tense, describing what the API does: "Retrieves a customer record by its unique identifier."
- Cover, in order, only what's relevant:
  - What the endpoint does (can restate/expand the Summary in full-sentence form)
  - Notable behavior or side effects (e.g., "Also marks the invitation as expired.")
  - Constraints worth calling out (idempotency, rate limits, eventual consistency)
  - Pointers to related endpoints, if genuinely useful

### Components without documentation
For brevity the properties for request and response DTOs are not documented. For requests that have multiple properties, Swagger already generates the schema to show constraints like max length or if certain values are required.

The endpoints are descriptive enough that the response codes aren't further documented (e.g., 401, 404). **Note**: It is still important to use `.Produces()` or `.Produces<T>()` and/or `.ProducesValidationProblem()` when defining endpoints.
