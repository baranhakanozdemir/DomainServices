namespace DomainServices.Core.Party;

public interface IOrganization : IParty
{
    string LegalName { get; set; }

    string? TaxId { get; set; }

    string? RegistrationNumber { get; set; }

    Guid? ParentOrganizationId { get; set; }
}
