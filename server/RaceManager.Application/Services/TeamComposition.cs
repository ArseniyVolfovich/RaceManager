using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public static class TeamComposition
{
    public static void Normalize(User owner)
    {
        owner.Profile.OrganizationMembers = NormalizeMembers(owner, owner.Profile.OrganizationName, owner.Profile.OrganizationMembers);
        owner.Profile.RacingTeamMembers = NormalizeMembers(owner, owner.Profile.RacingTeamName, owner.Profile.RacingTeamMembers);
    }

    private static List<OrganizationMember> NormalizeMembers(User owner, string teamName, IEnumerable<OrganizationMember> members)
    {
        if (string.IsNullOrWhiteSpace(teamName)) return [];

        var accepted = members
            .Where(member => member.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            .Where(member => !member.UserId.Equals(owner.Id, StringComparison.OrdinalIgnoreCase))
            .GroupBy(member => !string.IsNullOrWhiteSpace(member.UserId) ? member.UserId : member.Email, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => group.First())
            .ToList();

        accepted.Insert(0, new OrganizationMember
        {
            UserId = owner.Id,
            Status = "Owner",
            FullName = OwnerName(owner),
            Phone = owner.Profile.Phone,
            Email = owner.Email
        });
        return accepted;
    }

    private static string OwnerName(User owner)
    {
        var name = string.Join(" ", new[] { owner.Profile.LastName, owner.Profile.FirstName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? owner.Login : name;
    }
}
