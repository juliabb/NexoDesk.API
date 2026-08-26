using System.Security.Cryptography;
using System.Text;

namespace NexoDesk.Helpers;

public interface ICpfProtectionService
{
    string GerarHash(string cpf);
}

public sealed class CpfProtectionService : ICpfProtectionService
{
    private readonly byte[] _chave;

    public CpfProtectionService(string chave)
    {
        _chave = Encoding.UTF8.GetBytes(chave);
        if (_chave.Length < 32)
            throw new InvalidOperationException("A chave Security:CpfHmacKey deve possuir pelo menos 32 bytes.");
    }

    public string GerarHash(string cpf)
    {
        var normalizado = CpfHelper.SomenteNumeros(cpf);
        using var hmac = new HMACSHA256(_chave);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizado)));
    }
}
