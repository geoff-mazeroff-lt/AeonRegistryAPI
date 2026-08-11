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

## User management
This project leverages ASP.NET Identity to handle authentication and authorization. The user information is stored as part of the Postgres DB. The API makes use of built-in endpoints to interact with accounts.

From the Swagger interface, use `/api/auth/register-admin` to create a user. As long as the user doesn't already exist, it will be created with a temporary password that's hard-coded (see `CustomIdentityEndpoints.cs`). That user will also be assigned the role of `Researcher` (if said role exists).

**Warnings:** There are some elements from the Udemy video I typed verbatim that are inconsistent. Maybe these will be addressed in future segments.
- Why is the endpoint called `register-admin`? Is it to register a new administrator, or is it only supposed to be called by an administrator?
- Registering a user writes the content of a would-be email to the new user, and that content contains a password reset link. Perhaps the presenter included this to show how you would start setting it up, but the link and reset token **do nothing**.
- `register-admin` assigns a `Researcher` role, but I'm not sure how that gets created or what it does.

## Running the project
Run `dotnet restore` then `dotnet run`.

## Entity Framework reminders
- To create a migration: `dotnet ef migrations add {NameOfMigrationHere}`.
- To run migrations: `dotnet ef database update`.
