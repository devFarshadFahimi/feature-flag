using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Options.Extensions;

public abstract class BaseOptionsSetup<TOption>(IConfiguration configuration, string sectionName) : IConfigureOptions<TOption>
    where TOption : class
{
    public virtual void Configure(TOption options)
    {
        configuration.GetSection(sectionName).Bind(options);
    }
}
