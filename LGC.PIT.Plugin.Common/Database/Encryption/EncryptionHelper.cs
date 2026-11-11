using System;
using System.Security.Cryptography;
using System.Text;

namespace LGC.PIT.Plugin.Common.Database.Encryption;

public static class EncryptionHelper
{
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(plainText);

        var encrypted =
            ProtectedData.Protect(
                bytes,
                null,
                DataProtectionScope.LocalMachine);

        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        var bytes = Convert.FromBase64String(cipherText);

        var decrypted =
            ProtectedData.Unprotect(
                bytes,
                null,
                DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(decrypted);
    }
}

