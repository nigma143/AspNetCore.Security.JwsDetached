using System;
using System.IO;

namespace AspNetCore.Security.JwsDetached
{
    internal static class AspNetCoreTempDirectory
    {
        // ReSharper disable once InconsistentNaming
        private static string? _tempDirectory;

        public static string TempDirectory
        {
            get
            {
                if (_tempDirectory == null)
                {
                    // Look for folders in the following order.
                    var temp = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? // ASPNETCORE_TEMP - User set temporary location.
                               Path.GetTempPath();                                      // Fall back.

                    if (!Directory.Exists(temp))
                    {
                        throw new DirectoryNotFoundException(temp);
                    }

                    _tempDirectory = temp;
                }

                return _tempDirectory;
            }
        }

        public static Func<string> TempDirectoryFactory => () => TempDirectory;
    }
}
