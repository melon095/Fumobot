namespace Fumo.Shared.Enums;

public enum BanphraseReason
{
    /// <summary>
    /// Not banned
    /// </summary>
    None,

    /// <summary>
    /// Banned by glbaol regex
    /// </summary>
    Global,

    /// <summary>
    /// Banned by channels pajbot instance
    /// </summary>
    Pajbot,

    /// <summary>
    /// Failed to check due to timeout
    /// </summary>
    PajbotTimeout,
}

public static class BanphraseReasonExtension
{
    public static string ToReasonString(this BanphraseReason reason)
        => reason switch
        {
            BanphraseReason.None | BanphraseReason.PajbotTimeout => string.Empty,
            BanphraseReason.Global => "Message blocked due to naughty words monkaS",
            BanphraseReason.Pajbot => "Message blocked by pajbot monkaS",
            _ => "🤷‍",
        };
}
