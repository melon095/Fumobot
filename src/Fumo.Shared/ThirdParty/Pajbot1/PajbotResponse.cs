namespace Fumo.Shared.ThirdParty.Pajbot1;

public record BanphraseData(
    bool CaseSensitive,
    int ID,
    int Length,
    string Name,
    string Operator,
    bool Permanent,
    string Phrase,
    bool RemoveAccents,
    bool SubImmunity
);

public record PajbotResponse(bool Banned, BanphraseData BanphraseData, string InputMessage);

