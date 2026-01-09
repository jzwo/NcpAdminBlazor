using Microsoft.AspNetCore.Identity;

namespace NcpAdminBlazor.Infrastructure.Utils;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}

internal sealed class IdentityPasswordHasher(IPasswordHasher<object> identityPasswordHasher) : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return identityPasswordHasher.HashPassword(null!, password);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = identityPasswordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}