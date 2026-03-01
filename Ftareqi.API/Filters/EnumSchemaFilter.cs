using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ftareqi.API.Filters
{
	public class EnumSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (context.Type.IsEnum)
			{
				schema.Enum.Clear();
				foreach (var enumValue in Enum.GetNames(context.Type))
				{
					schema.Enum.Add(new OpenApiString(enumValue));
				}
				schema.Type = "string";
				schema.Format = null;
			}
		}
	}
}	