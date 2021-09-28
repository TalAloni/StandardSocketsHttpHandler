using System.Runtime.InteropServices;

namespace System
{
    internal class RuntimeUtils
    {
        public static bool IsDotNetFramework()
        {
#if NET461
            return true;
#else
            const string DotnetFrameworkDescription = ".NET Framework";
            string frameworkDescription = RuntimeInformation.FrameworkDescription;
            return frameworkDescription.StartsWith(DotnetFrameworkDescription);
#endif
        }
    }
}
