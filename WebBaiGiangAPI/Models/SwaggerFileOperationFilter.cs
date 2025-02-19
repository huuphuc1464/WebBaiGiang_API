using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using WebBaiGiangAPI.Models;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Lấy danh sách các tham số từ [FromForm]
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null);

        // Nếu RequestBody chưa khởi tạo, tạo mới
        if (operation.RequestBody == null)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>()
            };
        }

        // Khởi tạo nội dung "multipart/form-data" nếu chưa tồn tại
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

        // Lặp qua các tham số có [FromForm]
        foreach (var parameter in formParameters)
        {
            if (parameter.ParameterType == typeof(IFormFile) || typeof(IEnumerable<IFormFile>).IsAssignableFrom(parameter.ParameterType))
            {
                // Nếu là file, thêm trường với kiểu binary
                schema.Properties[parameter.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else
            {
                // Nếu là object, thêm từng thuộc tính của object
                var objectProperties = parameter.ParameterType.GetProperties();
                foreach (var prop in objectProperties)
                {
                    if (!schema.Properties.ContainsKey(prop.Name))
                    {
                        schema.Properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "string" // Mặc định dạng text input
                        };
                    }
                }
            }
        }
    }
}

