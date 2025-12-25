public static class AppVersion
{
    public static string Current => "1.0.4";
    public static Version AsVersion => Version.Parse(Current);
}
