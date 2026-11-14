using System;
using System.IO;
using System.Text;

namespace WorldRush.Core
{
    /// <summary>Schreibt Dateien absturzsicher: erst Temp-Datei, dann Austausch; alte Version bleibt als .bak.</summary>
    public static class AtomicFile
    {
        public static void WriteAllText(string path, string content)
        {
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));

            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            try
            {
                File.Replace(tmp, path, bak);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (IOException) { }

            File.Copy(path, bak, true);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
    }
}
