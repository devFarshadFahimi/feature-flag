using WebApi.ProgramConfiguration;

WebApplication.CreateBuilder(args)
    .AddServices()
    .Build()
    .UseWebApiConfiguration();