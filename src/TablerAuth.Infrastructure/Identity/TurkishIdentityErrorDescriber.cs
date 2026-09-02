using Microsoft.AspNetCore.Identity;

namespace TablerAuth.Infrastructure.Identity;

public sealed class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Bilinmeyen bir hata oluştu." };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "Bu kayıt başka biri tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Şifre yanlış." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "Bu bağlantı geçersiz. Yeni bir tane isteyin." };

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Kurtarma kodu kullanılamadı." };

    public override IdentityError LoginAlreadyAssociated() =>
        new() { Code = nameof(LoginAlreadyAssociated), Description = "Bu dış hesap zaten başka bir kullanıcıya bağlı." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = $"\"{userName}\" geçerli bir kullanıcı adı değil." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = $"\"{email}\" geçerli bir e-posta adresi değil." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"\"{userName}\" zaten kayıtlı." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"\"{email}\" zaten kayıtlı." };

    public override IdentityError InvalidRoleName(string? role) =>
        new() { Code = nameof(InvalidRoleName), Description = $"\"{role}\" geçerli bir rol adı değil." };

    public override IdentityError DuplicateRoleName(string role) =>
        new() { Code = nameof(DuplicateRoleName), Description = $"\"{role}\" rolü zaten var." };

    public override IdentityError UserAlreadyHasPassword() =>
        new() { Code = nameof(UserAlreadyHasPassword), Description = "Bu hesabın zaten bir şifresi var." };

    public override IdentityError UserLockoutNotEnabled() =>
        new() { Code = nameof(UserLockoutNotEnabled), Description = "Bu hesap için kilitleme açık değil." };

    public override IdentityError UserAlreadyInRole(string role) =>
        new() { Code = nameof(UserAlreadyInRole), Description = $"Bu kullanıcı zaten \"{role}\" rolünde." };

    public override IdentityError UserNotInRole(string role) =>
        new() { Code = nameof(UserNotInRole), Description = $"Bu kullanıcı \"{role}\" rolünde değil." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalıdır." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifrede en az {uniqueChars} farklı karakter olmalıdır." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifrede en az bir özel karakter olmalıdır." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Şifrede en az bir rakam olmalıdır." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Şifrede en az bir küçük harf olmalıdır." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Şifrede en az bir büyük harf olmalıdır." };
}
