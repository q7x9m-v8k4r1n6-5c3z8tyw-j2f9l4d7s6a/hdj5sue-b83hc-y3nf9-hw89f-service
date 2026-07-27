namespace OVCMOVE.Application.Common;

public sealed record FileUploadModel(
    Stream Stream,
    string FileName,
    string ContentType);
