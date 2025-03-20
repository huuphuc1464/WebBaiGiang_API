using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Chỉ áp dụng cho các HTTP methods khác GET
        if (context.ApiDescription.HttpMethod == "GET")
        {
            return;
        }

        // Lấy danh sách các tham số có [FromForm]
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null)
            .ToList();

        if (!formParameters.Any()) return;

        // Nếu RequestBody chưa khởi tạo, tạo mới
        if (operation.RequestBody == null)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>()
            };
        }

        // Kiểm tra nếu "multipart/form-data" chưa tồn tại, tạo mới
        if (!operation.RequestBody.Content.ContainsKey("multipart/form-data"))
        {
            operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>()
                }
            };
        }

        // Schema của "multipart/form-data"
        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;

        foreach (var parameter in formParameters)
        {
            if (typeof(IFormFile).IsAssignableFrom(parameter.ParameterType))
            {
                // Nếu là file đơn
                schema.Properties[parameter.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(parameter.ParameterType) ||
                     typeof(List<IFormFile>).IsAssignableFrom(parameter.ParameterType))
            {
                // Nếu là danh sách file (multiple files)
                schema.Properties[parameter.Name] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                };
            }
            else
            {
                // Nếu là tham số bình thường (string, int, ...)
                schema.Properties[parameter.Name] = new OpenApiSchema
                {
                    Type = GetOpenApiType(parameter.ParameterType)
                };
            }
        }
    }

    // Phương thức xác định kiểu OpenAPI từ kiểu C#
    private string GetOpenApiType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
        if (type == typeof(bool)) return "boolean";
        return "string"; // Mặc định là string nếu không thuộc các kiểu trên
    }
}
