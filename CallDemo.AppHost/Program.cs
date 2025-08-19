var builder = DistributedApplication.CreateBuilder(args);


var rabbitMQ = builder
    .AddRabbitMQ("RabbitMQ", port: 5672)
    .WithManagementPlugin(15672);

builder.AddProject<Projects.CallerService>("Caller")
    .WithReference(rabbitMQ)
    .WaitFor(rabbitMQ);

builder.AddProject<Projects.ResponderService>("Responder")
    .WithReference(rabbitMQ)
    .WaitFor(rabbitMQ);

builder.Build().Run();
