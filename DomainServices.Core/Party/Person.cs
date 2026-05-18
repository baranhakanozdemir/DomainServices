using DomainServices.Core.Validation;

namespace DomainServices.Core.Party;

public class Person : Party, IPerson
{
    public override PartyType PartyType => PartyType.Person;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.Unknown;

    protected override void OnValidate(ModelValidator validator)
    {
        validator.RequireNotEmpty(FirstName, nameof(FirstName));
        validator.RequireNotEmpty(LastName, nameof(LastName));

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = string.IsNullOrWhiteSpace(MiddleName)
                ? $"{FirstName} {LastName}"
                : $"{FirstName} {MiddleName} {LastName}";
        }

        base.OnValidate(validator);
    }
}
