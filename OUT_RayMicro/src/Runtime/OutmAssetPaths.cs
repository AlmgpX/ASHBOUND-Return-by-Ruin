namespace OUT_RayMicro.Runtime;

public static class OutmAssetPaths
{
    public const string DataFolderName = "data";

    public static string DataRoot
    {
        get
        {
            string outputData = Path.Combine(AppContext.BaseDirectory, DataFolderName);
            if (Directory.Exists(outputData))
                return outputData;

            string cwdData = Path.Combine(Environment.CurrentDirectory, DataFolderName);
            if (Directory.Exists(cwdData))
                return cwdData;

            return outputData;
        }
    }

    public static string ResolveData(string relativePath)
    {
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(DataRoot, relativePath);
    }
}
