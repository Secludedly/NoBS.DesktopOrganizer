using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NoBS.Core.Profiles
{
    public static class ProfileManager
    {
        public static readonly string ProfilesFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");

        static ProfileManager()
        {
            if (!Directory.Exists(ProfilesFolder))
                Directory.CreateDirectory(ProfilesFolder);
        }

        public static List<WorkspaceProfile> LoadAllProfiles()
        {
            var profiles = new List<WorkspaceProfile>();

            foreach (var file in Directory.GetFiles(ProfilesFolder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<WorkspaceProfile>(json);
                    if (profile != null)
                        profiles.Add(profile);
                }
                catch
                {
                    // ignored
                }
            }

            // Sort by DisplayOrder, then by Name
            return profiles.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList();
        }

        public static void SaveProfile(WorkspaceProfile profile)
        {
            var filePath = Path.Combine(ProfilesFolder, $"{profile.Name}.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }

        public static bool RenameProfile(string oldName, string newName)
        {
            var oldPath = Path.Combine(ProfilesFolder, $"{oldName}.json");
            var newPath = Path.Combine(ProfilesFolder, $"{newName}.json");

            if (!File.Exists(oldPath))
                return false;

            if (File.Exists(newPath))
                return false;

            File.Move(oldPath, newPath);
            return true;
        }

        public static void DeleteProfile(string profileName)
        {
            var filePath = Path.Combine(ProfilesFolder, $"{profileName}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public static void ReorderProfiles(List<WorkspaceProfile> orderedProfiles)
        {
            for (int i = 0; i < orderedProfiles.Count; i++)
            {
                orderedProfiles[i].DisplayOrder = i;
                SaveProfile(orderedProfiles[i]);
            }
        }
    }
}
