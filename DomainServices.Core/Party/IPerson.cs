namespace DomainServices.Core.Party;

public interface IPerson : IParty
{
    string FirstName { get; set; }

    string? MiddleName { get; set; }

    string LastName { get; set; }

    DateOnly? DateOfBirth { get; set; }

    Gender Gender { get; set; }
}
