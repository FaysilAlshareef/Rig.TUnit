namespace Rig.TUnit.Observability.Logging.Analyzers;

/// <summary>
/// Copy of the canonical PII token list. Must stay in sync with
/// <c>Rig.TUnit.Observability.Logging.Options.LoggingDetectorOptions.CanonicalPiiTokens</c>.
/// Duplicated (not referenced) because analyzers target netstandard2.0 and cannot
/// reference the net10.0 library.
/// </summary>
internal static class PiiTokens
{
    public static readonly string[] Canonical =
    {
        "password", "pwd", "secret", "token", "apikey", "api_key",
        "authorization", "auth", "credential", "credentials",
        "ssn", "socialsecurity", "social_security",
        "creditcard", "credit_card", "cardnumber", "card_number", "cvv", "cvc",
        "email", "phone", "phonenumber", "phone_number",
        "dob", "dateofbirth", "date_of_birth",
        "address", "streetaddress", "street_address",
        "connectionstring", "connection_string",
    };

    public static bool IsPii(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < Canonical.Length; i++)
        {
            if (name.IndexOf(Canonical[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }
}
