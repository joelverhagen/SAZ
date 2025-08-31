using System.Runtime.CompilerServices;
using EmptyFiles;

namespace Knapcode.SAZ;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.InitializePlugins();
        VerifierSettings.AutoVerify(includeBuildServer: false);
        FileExtensions.AddTextExtension("x-www-form-urlencoded");
    }
}
