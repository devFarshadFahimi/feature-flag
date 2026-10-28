using System.Text.Json;

namespace FeatureFlag.Provider.Providers;

public static class JSONSerializeExtensions
{
    public static readonly JsonSerializerOptions ResponseJSON = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true
    };
}
