using System;
using System.Collections.Generic;

namespace OVCMOVE.Application.DTOs.ResultModels;

public class ExampleResultModel
{
    public class CreateExampleResultModel
    {
        public bool IsSuccess { get; init; }

        public string Message { get; init; } = string.Empty;
    }

    public class UpdateExampleResultModel
    {
    }

    public class DeleteExampleResultModel
    {
    }

    public class GetAllExamplesByFilterResultModel
    {
        public int TotalCount { get; init; }

        public IReadOnlyCollection<ExampleItemResultModel> Items { get; init; } = Array.Empty<ExampleItemResultModel>();
    }

    public class ExampleItemResultModel
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
