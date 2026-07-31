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

## Running the project
Run `dotnet restore` then `dotnet run`.

## Entity Framework reminders
- To create a migration: `dotnet ef migrations add {NameOfMigrationHere}`.
- To run migrations: `dotnet ef database update`.
