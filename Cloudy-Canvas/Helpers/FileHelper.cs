﻿namespace Cloudy_Canvas.Helpers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;
    using Newtonsoft.Json;

    public static class FileHelper
    {
        public static string SetUpFilepath(FilePathType type, string filename, string extension, SocketCommandContext context = null, string logChannel = "", string date = "")
        {
            //Root
            var filepath = DevSettings.RootPath;
            CreateDirectoryIfNotExists(filepath);

            //Server
            if (type != FilePathType.Root)
            {
                filepath = Path.Join(filepath, "Servers");
                CreateDirectoryIfNotExists(filepath);

                if (context != null && context.IsPrivate)
                {
                    filepath = Path.Join(filepath, "_UserDMs");
                    CreateDirectoryIfNotExists(filepath);
                    filepath = Path.Join(filepath, $"{context.User.Username}");
                    CreateDirectoryIfNotExists(filepath);
                }
                else
                {
                    if (context != null)
                    {
                        filepath = Path.Join(filepath, $"{context.Guild.Name}");
                        CreateDirectoryIfNotExists(filepath);

                        //channel
                        if (type != FilePathType.Server)
                        {
                            if (type == FilePathType.Channel)
                            {
                                filepath = Path.Join(filepath, $"{context.Channel.Name}");
                                CreateDirectoryIfNotExists(filepath);
                            }
                            else
                            {
                                filepath = Path.Join(filepath, $"{logChannel}");
                                CreateDirectoryIfNotExists(filepath);
                                filepath = Path.Join(filepath, $"{date}.{extension}");
                                return filepath;
                            }
                        }
                    }
                }
            }

            switch (filename)
            {
                case "":
                    filepath = Path.Join(filepath, $"Default.{extension}");
                    break;
                case "<date>":
                    filepath = Path.Join(filepath, $"{DateTime.UtcNow:yyyy-MM-dd}.{extension}");
                    break;
                default:
                    filepath = Path.Join(filepath, $"{filename}.{extension}");
                    break;
            }

            return filepath;
        }

        public static async Task<ServerSettings> LoadServerSettings(SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "Settings", "conf", context);
            var fileContents = await File.ReadAllTextAsync(filepath);
            var settings = JsonConvert.DeserializeObject<ServerSettings>(fileContents);
            return settings;
        }

        public static async Task SaveServerSettingsAsync(ServerSettings settings, SocketCommandContext context)
        {
            var filepath = SetUpFilepath(FilePathType.Server, "Settings", "conf", context);
            var fileContents = JsonConvert.SerializeObject(settings);
            await File.WriteAllTextAsync(filepath, fileContents);
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                directory.Create();
            }
        }
    }

    public enum FilePathType
    {
        Root,
        Server,
        Channel,
        LogRetrieval,
    }
}
