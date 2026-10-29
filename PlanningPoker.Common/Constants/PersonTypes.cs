namespace PlanningPoker.Common.Constants;

public static class PersonTypes
{
    public const string Developer = "dev";
    public const string Tester = "test";
    public const string Observer = "obs";

    public static bool IsValid(string? value)
    {
        return value is Developer or Tester or Observer;
    }
}
