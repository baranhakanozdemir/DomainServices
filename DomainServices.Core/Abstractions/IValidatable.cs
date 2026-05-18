using DomainServices.Core.Validation;

namespace DomainServices.Core.Abstractions;

public interface IValidatable
{
    ModelValidator Validate();
}
