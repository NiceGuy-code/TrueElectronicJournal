using Journal.Models;
using Microsoft.AspNetCore.Identity;

namespace Journal.Services;

// Заглушка: пароль хранится открытым (учебный проект)
public class PlainTextPasswordHasher : IPasswordHasher<ApplicationUser>
{
    public string HashPassword(ApplicationUser user, string password) => password;

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
    {
        return hashedPassword == providedPassword
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }
}
