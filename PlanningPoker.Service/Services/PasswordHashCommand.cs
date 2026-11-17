namespace PlanningPoker.Service.Services;

/// <summary>The <c>--hash-password</c> switch: reads a password from the input (never from an argument, which shells keep in history) and prints its hash.</summary>
public static class PasswordHashCommand
{
    public const string Switch = "--hash-password";

    public static int Run(IPasswordHashingService hashing, TextReader input, TextWriter output, TextWriter error)
    {
        var password = input.ReadLine();
        if (string.IsNullOrEmpty(password))
        {
            error.WriteLine("Provide the password on standard input, for example: echo \"the password\" | dotnet run --project PlanningPoker.Api -- --hash-password");
            return 1;
        }

        output.WriteLine(hashing.Hash(password));
        return 0;
    }
}
