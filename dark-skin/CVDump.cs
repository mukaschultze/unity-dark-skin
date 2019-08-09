using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DarkSkin {
    public class CVDump : IDisposable {

        private const string CVDUMP_RESOURCE = "cvdump.exe";

        private readonly string cvDumpExe;

        public CVDump() {
            cvDumpExe = ExportExeResouce();
        }

        public void Execute(string exePath, DataReceivedEventHandler outputCallback, DataReceivedEventHandler errorCallback, string arguments = "-headers -p") {

            var startOptions = new ProcessStartInfo {
                FileName = cvDumpExe,
                Arguments = string.Format("{0} {1}", arguments, exePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startOptions);

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.OutputDataReceived += outputCallback;
            process.ErrorDataReceived += errorCallback;

            process.WaitForExit();
        }

        private static string ExportExeResouce() {
            var tempFile = Path.GetTempFileName();
            var resourceBytes = GetEmbeddedResource(CVDUMP_RESOURCE);

            File.WriteAllBytes(tempFile, resourceBytes);

            return tempFile;
        }

        private void Cleanup() {
            if (!string.IsNullOrEmpty(cvDumpExe) && File.Exists(cvDumpExe))
                File.Delete(cvDumpExe);
        }

        private static byte[] GetEmbeddedResource(string resourceName) {
            var type = typeof(CVDump);
            var cvdumpExe = FormatResourceName(type.Namespace, resourceName);

            using(var resourceStream = type.Assembly.GetManifestResourceStream(cvdumpExe)) {
                if (resourceStream == null)
                    return null;

                var buffer = new byte[resourceStream.Length];
                resourceStream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private static string FormatResourceName(string nameSpace, string resourceName) {
            return nameSpace + "." + resourceName.Replace(" ", "_")
                .Replace("\\", ".")
                .Replace("/", ".");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                Cleanup();
                disposedValue = true;
            }
        }

        ~CVDump() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}