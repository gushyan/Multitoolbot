var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ProjectHost>("projecthost");

builder.Build().Run();
