namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IPasswordHasher
{
    byte[] Hash(string password);
    bool Verify(string password, byte[] hash);
}
