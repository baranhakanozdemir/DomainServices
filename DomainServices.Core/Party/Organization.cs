using DomainServices.Core.Validation;

namespace DomainServices.Core.Party;

public class Organization : Party, IOrganization
{
    public override PartyType PartyType => PartyType.Organization;

    public string LegalName { get; set; } = string.Empty;

    public string? TaxId { get; set; }

    public string? RegistrationNumber { get; set; }

    public Guid? ParentOrganizationId { get; set; }

    protected override void OnValidate(ModelValidator validator)
    {
        validator.RequireNotEmpty(LegalName, nameof(LegalName));

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = LegalName;
        }

        base.OnValidate(validator);
    }
}
