using WebApi.ProgramConfiguration;

await WebApplication.CreateBuilder(args)
    .AddServices()
    .Build()
    .UseWebApiConfiguration();