


using System.IO;

public static class AiderPackageUtils
{
    public static readonly string packageName = "com.kosmosisdire.aider-unity";
    public static readonly string packagePath = Path.Combine("Packages", packageName);

    public static string GetPath(string path)
    {
        return Path.Combine(packagePath, path);
    }
}