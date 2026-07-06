using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Common
{
    public class ApiResponseModel<T>
    {
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public string DetailError { get; init; } = string.Empty;
        public T? Data { get; init; }

        public ApiResponseModel()
        {
        }

        public ApiResponseModel(int statusCode, string message, string detailError = "", T? data = default)
        {
            StatusCode = statusCode;
            Message = message;
            DetailError = detailError;
            Data = data;
        }
    }

    public sealed class InternalServerErrorModel : ApiResponseModel<object>
    {
        public InternalServerErrorModel()
            : base(APIContansts.StatusCode.InternalServerError, APIContansts.StatusMessage.InternalServerError)
        {
        }

        public InternalServerErrorModel(string detailError = "")
            : base(APIContansts.StatusCode.InternalServerError, APIContansts.StatusMessage.InternalServerError, detailError)
        {
        }
    }
}