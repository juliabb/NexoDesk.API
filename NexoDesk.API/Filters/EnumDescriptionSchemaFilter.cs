using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NexoDesk.API.Filters;

public class EnumDescriptionSchemaFilter : ISchemaFilter
{
    public void Apply(
        IOpenApiSchema schema,
        SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        var values = Enum.GetValues(context.Type);

        foreach (var value in values)
        {
            var name = Enum.GetName(context.Type, value);
            var number = Convert.ToInt32(value);

            schema.Description += $"{number} - {name}<br />";
        }
    }
}
