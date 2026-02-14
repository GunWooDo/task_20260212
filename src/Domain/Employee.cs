namespace Domain;

public sealed record Employee(string Name, string Email, string Tel, DateOnly Joined)
{
    public int Id { get; init; }
}
