using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Example.Command.CreateNewExample;

public class CreateNewExampleCommand : IRequest<ExampleResultModel.CreateExampleResultModel>
{
}
