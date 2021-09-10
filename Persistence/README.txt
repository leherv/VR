-- Add migration to project:
dotnet ef --startup-project ../VR migrations add <name> -c VRPersistenceDbContext

-- Apply the migration to the database manually
dotnet ef --startup-project ../VR database update -c VRPersistenceDbContext

Note: you need to set the environment variable with the connection string locally