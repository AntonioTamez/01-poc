var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

postgres.AddDatabase("ordersdb");

builder.Build().Run();
