using System.Security.Cryptography;

namespace GameServer.Matches;

public readonly record struct MatchId
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public string Value { get; }

    public MatchId(string value)
    {
        var code = new string((value ?? "").Where(c => !char.IsWhiteSpace(c) && c != '-')
            .Select(char.ToUpperInvariant).ToArray());
        Value = code.Length == 6 ? code.Insert(4, "-") : code;
    }

    // The registry checks collisions against both pending and active matches under its lock.
    public static MatchId New() => new(new string(Enumerable.Range(0, 6)
        .Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray()));

    public override string ToString() => Value;
}
