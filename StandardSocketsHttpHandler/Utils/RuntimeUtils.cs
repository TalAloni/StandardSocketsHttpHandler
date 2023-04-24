using System.Runtime.InteropServices;

namespace System
{
    internal class RuntimeUtils
    {
        public static bool IsDotNetFramework()
        {
            const string DotnetFrameworkDescription = ".NET Framework";
            string frameworkDescription = RuntimeInformation.FrameworkDescription;
            return frameworkDescription.StartsWith(DotnetFrameworkDescription);
        }

        // .NET Native compiles C# code to native CPU instructions ahead-of-time (AoT) rather than using the CLR to compile IL just-in-time (JIT) to native code later.
        public static bool IsDotNetNative()
        {
            const string DotnetFrameworkDescription = ".NET Native";
            string frameworkDescription = RuntimeInformation.FrameworkDescription;
            return frameworkDescription.StartsWith(DotnetFrameworkDescription);
        }
    }
}
