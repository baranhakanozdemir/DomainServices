using DomainServices.Core.Models;
using DomainServices.Core.Validation;

namespace DomainServices.Core.Party;

public abstract class Party : CoreDomainModel, IParty
{
    public abstract PartyType PartyType { get; }

    public string DisplayName { get; set; } = string.Empty;

    public ContactInfo? Contact { get; set; }

    protected override void OnValidate(ModelValidator validator)
    {
        base.OnValidate(validator);
        validator.RequireNotEmpty(DisplayName, nameof(DisplayName));
    }
}
