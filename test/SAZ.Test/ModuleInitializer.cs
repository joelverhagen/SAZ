using System.Runtime.CompilerServices;

namespace Knapcode.SAZ;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
#if !NET8_0
        VerifyDiffPlex.Initialize();
        VerifierSettings.InitializePlugins();
        VerifierSettings.DontScrubDateTimes();
        VerifierSettings.AutoVerify(includeBuildServer: false);
        EmptyFiles.FileExtensions.AddTextExtension("x-www-form-urlencoded");
#endif
    }
}
