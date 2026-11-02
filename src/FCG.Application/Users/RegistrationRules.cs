using System.Net.Mail;

namespace FCG.Application.Users;

/// <summary>Applies validation and normalization rules to user registration.</summary>
public static class RegistrationRules
{
    /// <summary>Validates the registration command and returns field errors.</summary>
    public static IReadOnlyDictionary<string, string[]> Validate(RegisterUserCommand command)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            AddError(errors, nameof(command.Name), "O nome é obrigatório.");
        }

        if (!IsValidEmail(command.Email))
        {
            AddError(errors, nameof(command.Email), "O e-mail informado é inválido.");
        }

        if (!IsValidPassword(command.Password))
        {
            AddError(errors, nameof(command.Password), "A senha deve ter pelo menos 8 caracteres, com letras, números e caracteres especiais.");
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Normalizes an e-mail for case-insensitive lookup.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    /// <summary>Removes surrounding whitespace from a name.</summary>
    public static string NormalizeName(string name) => name.Trim();

    /// <summary>Determines whether an e-mail has a valid basic format.</summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address.Contains('@', StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Determines whether a password meets the Phase 1 security rules.</summary>
    public static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            return false;
        }

        var hasLetter = false;
        var hasNumber = false;
        var hasSpecial = false;

        foreach (var character in password)
        {
            if (char.IsLetter(character))
            {
                hasLetter = true;
            }
            else if (char.IsDigit(character))
            {
                hasNumber = true;
            }
            else
            {
                hasSpecial = true;
            }
        }

        return hasLetter && hasNumber && hasSpecial;
    }

    private static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(message);
    }
}
