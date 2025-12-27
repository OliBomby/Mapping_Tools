using System;
using System.Diagnostics;

namespace Mapping_Tools.Classes.SystemTools
{
    public static class Browser
    {
        public static void OpenLink(Uri uri)
        {
            OpenLink(uri.AbsoluteUri);
        }

        public static void OpenLink(string uri)
        {
            // https://github.com/dotnet/runtime/issues/17938
            var startInfo = new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(startInfo);
        }
    }
}
