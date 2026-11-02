using System.Text;
using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 12 || Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new ArgumentException("گذرواژه باید حداقل ۱۲ نویسه و حداکثر ۷۲ بایت با کدگذاری UTF-8 داشته باشد.");
        }

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || Encoding.UTF8.GetByteCount(password) > 72)
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}