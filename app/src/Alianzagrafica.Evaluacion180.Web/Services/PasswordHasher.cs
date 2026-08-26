using System.Security.Cryptography;

namespace Alianzagrafica.Evaluacion180.Web.Services;

/// <summary>
/// Hashing de contraseñas con PBKDF2-SHA256 (Rfc2898DeriveBytes), sin dependencias externas.
/// Solo aplica a usuarios con Usuario.TipoAutenticacion = 'Local' (ver sección 8.5 del documento
/// de diseño). En producción, el personal con usuario de Active Directory se autentica mediante
/// Windows Authentication configurada a nivel de IIS, sin pasar por esta clase.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public byte[] Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algoritmo, KeySize);
        return [.. salt, .. key];
    }

    public bool Verify(string password, byte[] hash)
    {
        if (hash.Length != SaltSize + KeySize) return false;
        var salt = hash[..SaltSize];
        var expectedKey = hash[SaltSize..];
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algoritmo, KeySize);
        return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
    }
}
