using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lime.Client.TestConsole
{
    public static class FileUtil
    {
        public const string APP_DATA_FOLDER_NAME = "LIME Test Console";

        public static IEnumerable<string[]> GetFileLines(string fileName, char separator)
        {
            var appDataFileName = GetAppDataFileName(fileName);

            if (!File.Exists(appDataFileName))
            {
                if (File.Exists(fileName))
                {
                    File.Copy(fileName, appDataFileName);
                }
                else
                {
                    File.Create(appDataFileName).Close();
                }
            }

            fileName = appDataFileName;

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

        public static T GetFileContent<T>(string fileName)
        {
            var appDataFileName = GetAppDataFileName(fileName);
            T fileContent;

            if (!File.Exists(appDataFileName))
            {
                if (File.Exists(fileName))
                {
                    File.Copy(fileName, appDataFileName);
                }
                else
                {
                    File.Create(appDataFileName).Close();
                }
            }

            fileName = appDataFileName;

            using (StreamReader file = File.OpenText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                fileContent = (T)serializer.Deserialize(file, typeof(T));
            }

            return fileContent;
        }

        public static void SaveFile(IEnumerable<string[]> content, string fileName, char separator)
        {
            fileName = GetAppDataFileName(fileName);
            File.WriteAllLines(fileName, content.Select(s => string.Join(separator.ToString(CultureInfo.InvariantCulture), s)));
        }

        public static void SaveFile(object content, string fileName)
        {
            fileName = GetAppDataFileName(fileName);
            var stringContent = JsonConvert.SerializeObject(content);
            File.WriteAllText(fileName, stringContent);
        }

        private static string GetAppDataFileName(string fileName)
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
                APP_DATA_FOLDER_NAME);

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            var appDataFileName = Path.Combine(
                appDataFolder, fileName);
            return appDataFileName;
        }
    }
}
