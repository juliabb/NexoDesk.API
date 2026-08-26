namespace NexoDesk.Helpers;

public static class CpfHelper
{
    public static string SomenteNumeros(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public static bool EhValido(string cpf)
    {
        cpf = SomenteNumeros(cpf);

        if (cpf.Length != 11)
        {
            return false;
        }

        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var numeros = cpf
            .Select(caractere => caractere - '0')
            .ToArray();

        var somaPrimeiroDigito = 0;

        for (var indice = 0; indice < 9; indice++)
        {
            somaPrimeiroDigito += numeros[indice] * (10 - indice);
        }

        var restoPrimeiroDigito = somaPrimeiroDigito % 11;

        var primeiroDigito = restoPrimeiroDigito < 2
            ? 0
            : 11 - restoPrimeiroDigito;

        if (numeros[9] != primeiroDigito)
        {
            return false;
        }

        var somaSegundoDigito = 0;

        for (var indice = 0; indice < 10; indice++)
        {
            somaSegundoDigito += numeros[indice] * (11 - indice);
        }

        var restoSegundoDigito = somaSegundoDigito % 11;

        var segundoDigito = restoSegundoDigito < 2
            ? 0
            : 11 - restoSegundoDigito;

        return numeros[10] == segundoDigito;
    }

    public static string Mascarar(string cpf)
    {
        cpf = SomenteNumeros(cpf);

        if (cpf.Length != 11)
        {
            return "***.***.***-**";
        }

        return $"***.***.{cpf.Substring(6, 3)}-**";
    }
}