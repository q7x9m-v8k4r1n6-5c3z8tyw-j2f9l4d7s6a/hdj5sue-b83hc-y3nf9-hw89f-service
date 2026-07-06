using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Exceptions;

public class ExampleDomainException : BaseExecption
{
    public ExampleDomainException()
        : base("Example domain rule was violated.")
    {
    }
}
