-- Add migration to project:
dotnet ef --startup-project ../VR migrations add <name> -c VRPersistenceDbContext

-- Apply the migration to the database manually
dotnet ef --startup-project ../VR database update -c VRPersistenceDbContext

