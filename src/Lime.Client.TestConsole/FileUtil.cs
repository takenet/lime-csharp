using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole
{
    public static class FileUtil
    {
        public static IEnumerable<string[]> GetFileLines(string fileName, char separator)
        {
            using (var fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            yield return line.Split(separator);
                        }
                    }
                }
            }
        }

        public static void SaveFile(IEnumerable<string[]> content, string fileName, char separator)
        {
            File.WriteAllLines(fileName, content.Select(s => string.Join(separator.ToString(CultureInfo.InvariantCulture), s)));
        }
    }
}
