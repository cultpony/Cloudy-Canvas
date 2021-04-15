﻿namespace Cloudy_Canvas.Modules
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Blacklist;
    using Cloudy_Canvas.Service;
    using Discord;
    using Discord.Commands;

    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("admin")]
        public async Task AdminAsync(string commandOne = "", string commandTwo = "", [Remainder] string commandThree = "")
        {
            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await AdminChannelGetAsync();
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                case "ignorechannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            break;
                        case "get":
                            await IgnoreChannelGetAsync();
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree);
                            break;
                        case "clear":
                            await IgnoreChannelClearAsync();
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            break;
                    }

                    break;
                case "ignorerole":
                    break;
                case "adminrole":
                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    break;
            }
        }

        private static async Task<ulong> GetAdminChannelAsync(SocketCommandContext context)
        {
            var setting = await FileHelper.GetSetting("adminchannel", context);
            ulong channelId = 0;
            if (setting.Contains("<ERROR>"))
            {
                return channelId;
            }

            var split = setting.Split("<#", 2)[1].Split('>', 2)[0];
            channelId = ulong.Parse(split);
            return channelId;
        }

        private static async Task<List<ulong>> GetIgnoredChannelsAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                return new List<ulong>();
            }

            var channelList = await File.ReadAllLinesAsync(filename);
            var channelIdList = new List<ulong>();
            foreach (var channel in channelList)
            {
                channelIdList.Add(ulong.Parse(channel.Split("> #", 2)[0].Substring(2)));
            }

            return channelIdList;
        }

        private static async Task<bool> AddIgnoreChannelAsync(ulong channelId, string channelName, SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                await File.WriteAllTextAsync(filename, $"<#{channelId}> #{channelName}\n");
                return true;
            }

            var channelList = await File.ReadAllLinesAsync(filename);
            foreach (var channel in channelList)
            {
                if (channel.Contains(channelId.ToString()))
                {
                    return false;
                }
            }

            await File.AppendAllTextAsync(filename, $"<#{channelId}> #{channelName}\n");
            return true;
        }

        private static async Task<bool> RemoveIgnoreChannelAsync(ulong channelId, SocketCommandContext context)
        {
            var removed = false;
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            if (!File.Exists(filename))
            {
                return false;
            }

            var channelList = (await File.ReadAllLinesAsync(filename)).ToList();
            for (var x = channelList.Count - 1; x >= 0; x--)
            {
                if (!channelList[x].Contains(channelId.ToString()))
                {
                    continue;
                }

                channelList.Remove(channelList[x]);
                removed = true;
            }

            if (removed)
            {
                await File.WriteAllLinesAsync(filename, channelList);
            }

            return removed;
        }

        private static async Task ClearIgnoreChannelAsync(SocketCommandContext context)
        {
            var filename = FileHelper.SetUpFilepath(FilePathType.Server, "IgnoredChannels", "txt", context);
            await File.WriteAllTextAsync(filename, "");
        }

        private async Task AdminChannelSetAsync(string commandThree)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(commandThree, Context);
            var channelSetName = await DiscordHelper.ConvertChannelPingToNameAsync(commandThree, Context);
            if (channelSetId > 0)
            {
                if (channelSetName.Contains("<ERROR>"))
                {
                    await SetAdminChannelAsync(channelSetId, commandThree);
                }
                else
                {
                    await SetAdminChannelAsync(channelSetId, channelSetName);
                }

                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{commandThree}.");
            }
        }

        private async Task AdminChannelGetAsync()
        {
            var channelGetId = await GetAdminChannelAsync(Context);
            if (channelGetId > 0)
            {
                await ReplyAsync($"Admin channel is <#{channelGetId}>");
            }
            else
            {
                await ReplyAsync("Admin channel not set yet.");
            }
        }

        private async Task IgnoreChannelGetAsync()
        {
            var channelList = await GetIgnoredChannelsAsync(Context);
            if (channelList.Count > 0)
            {
                var output = "__Channel Ignore List:__\n";
                foreach (var channel in channelList)
                {
                    output += $"<#{channel}>\n";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No channels on ignore list.");
            }
        }

        private async Task IgnoreChannelClearAsync()
        {
            await ClearIgnoreChannelAsync(Context);
            await ReplyAsync("Ignore channel list cleared.");
        }

        private async Task IgnoreChannelRemoveAsync(string commandThree)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(commandThree, Context);
            if (channelRemoveId > 0)
            {
                var removed = await RemoveIgnoreChannelAsync(channelRemoveId, Context);

                if (removed)
                {
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                }
                else
                {
                    await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{commandThree}.");
            }
        }

        private async Task IgnoreChannelAddAsync(string commandThree)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(commandThree, Context);
            var channelAddName = await DiscordHelper.ConvertChannelPingToNameAsync(commandThree, Context);
            if (channelAddId > 0)
            {
                bool added;
                if (channelAddName.Contains("<ERROR>"))
                {
                    added = await AddIgnoreChannelAsync(channelAddId, commandThree, Context);
                }
                else
                {
                    added = await AddIgnoreChannelAsync(channelAddId, channelAddName, Context);
                }

                if (added)
                {
                    await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
                }
                else
                {
                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                }
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{commandThree}.");
            }
        }

        private async Task SetAdminChannelAsync(ulong channelId, string channelName)
        {
            await FileHelper.SetSetting("adminchannel", $"<#{channelId}> #{channelName}", Context);
        }

        public class BlacklistModule : ModuleBase<SocketCommandContext>
        {
            private readonly BlacklistService _blacklistService;

            private readonly LoggingHelperService _logger;

            public BlacklistModule(BlacklistService blacklistService, LoggingHelperService logger)
            {
                _blacklistService = blacklistService;
                _logger = logger;
            }

            [RequireUserPermission(GuildPermission.Administrator)]
            [Command("blacklist")]
            [Summary("Blacklist base command")]
            public async Task Blacklist(string arg = null, [Remainder] string term = null)
            {
                _blacklistService.InitializeList(Context);
                switch (arg)
                {
                    case null:
                        await ReplyAsync("You must specify a subcommand.");
                        await _logger.Log("blacklist null", Context);
                        break;
                    case "add":
                        var added = _blacklistService.AddTerm(term);
                        if (added)
                        {
                            await ReplyAsync($"Added `{term}` to the blacklist.");
                            await _logger.Log($"blacklist add (success): {term}", Context);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` is already on the blacklist.");
                            await _logger.Log($"blacklist add (fail): {term}", Context);
                        }

                        break;
                    case "remove":
                        var removed = _blacklistService.RemoveTerm(term);
                        if (removed)
                        {
                            await ReplyAsync($"Removed `{term}` from the blacklist.");
                            await _logger.Log($"blacklist remove (success): {term}", Context);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` was not on the blacklist.");
                            await _logger.Log($"blacklist remove (fail): {term}", Context);
                        }

                        break;
                    case "get":
                        var output = "The blacklist is currently empty.";
                        var blacklist = _blacklistService.GetList();
                        foreach (var item in blacklist)
                        {
                            if (output == "The blacklist is currently empty.")
                            {
                                output = $"`{item}`";
                            }
                            else
                            {
                                output += $", `{item}`";
                            }
                        }

                        await ReplyAsync($"__Blacklist Terms:__\n{output}");
                        await _logger.Log("blacklist get", Context);
                        break;
                    case "clear":
                        _blacklistService.ClearList();
                        await ReplyAsync("Blacklist cleared");
                        await _logger.Log("blacklist clear", Context);
                        break;
                    default:
                        await ReplyAsync("Invalid subcommand");
                        await _logger.Log($"blacklist invalid: {arg}", Context);
                        break;
                }
            }
        }
    }
}
