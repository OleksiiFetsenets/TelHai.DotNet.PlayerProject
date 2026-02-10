using System.IO;
using System.Text.Json;

namespace TelHai.DotNet.PlayerProject
{
    public class AppSettings
    {
        public List<string> MusicFolders { get; set; } = new List<string>();
        private const string SETTINGS_FILE = "settings.json";

        public static void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json =  JsonSerializer.Serialize (settings,options);
            File.WriteAllText(SETTINGS_FILE, json);
        }

        public static AppSettings Load()
        {
            if (File.Exists(SETTINGS_FILE))
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }
    }

}
