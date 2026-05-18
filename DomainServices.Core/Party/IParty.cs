using DomainServices.Core.Models;

namespace DomainServices.Core.Party;

public interface IParty : ICoreDomainModel
{
    PartyType PartyType { get; }

    string DisplayName { get; set; }

    ContactInfo? Contact { get; set; }
}
