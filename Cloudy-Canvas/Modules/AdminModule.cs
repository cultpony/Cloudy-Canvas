﻿namespace Cloudy_Canvas.Modules
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord;
    using Discord.Commands;

    [Summary("Module for managing admin functions")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly LoggingService _logger;
        private readonly BooruService _booru;
        private readonly AllPreloadedSettings _servers;

        public AdminModule(LoggingService logger, BooruService booru, AllPreloadedSettings servers)
        {
            _logger = logger;
            _booru = booru;
            _servers = servers;
        }

        [Command("setup")]
        [Summary("Bot setup command")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupCommandAsync(
            int filterId,
            [Summary("Admin channel name")] string adminChannelName = "",
            [Remainder] [Summary("Admin role name")] string adminRoleName = "")
        {
            var settings = new ServerSettings();
            ulong channelSetId;
            var checkedFilterId = await _booru.CheckFilterAsync(filterId);
            if (checkedFilterId == 0)
            {
                await ReplyAsync("I could not find that filter; please make sure it exists and is set to public. You may change the filter later with `;admin filter set <filterId>`. Continuing setup with my default filter of 175.");
                filterId = 175;
            }

            settings.defaultFilterId = filterId;
            await ReplyAsync($"Using <https://manebooru.art/filters/{filterId}>");
            await ReplyAsync("Moving in to my new place...");
            if (adminChannelName == "")
            {
                channelSetId = Context.Channel.Id;
            }
            else
            {
                channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(adminChannelName, Context);
            }

            if (channelSetId > 0)
            {
                settings.adminChannel = channelSetId;
                await ReplyAsync($"Moved into <#{channelSetId}>!");
                var adminChannel = Context.Guild.GetTextChannel(settings.adminChannel);
                await adminChannel.SendMessageAsync("Howdy neighbors! I will send important message here now.");
            }
            else
            {
                await ReplyAsync($"I couldn't find a place called #{adminChannelName}.");
                await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <FAIL>, role {adminRoleName} <NOT CHECKED>", Context);
                return;
            }

            await ReplyAsync("Looking for the bosses...");
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(adminRoleName, Context);
            if (roleSetId > 0)
            {
                settings.adminRole = roleSetId;
                await ReplyAsync($"<@&{roleSetId}> is in charge now!", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"I couldn't find @{adminRoleName}.");
                await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <FAIL>", Context, true);
                return;
            }

            await ReplyAsync("Setting the remaining admin settings to default values (all alerts will post to the admin channel, and no roles will be pinged)...");
            settings.redAlertChannel = settings.adminChannel;
            settings.yellowAlertChannel = settings.adminChannel;
            settings.logPostChannel = settings.adminChannel;
            settings.reportChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync(
                "Settings saved. Now building the spoiler list and redlist. This may take a few minutes, depending on how many tags are spoilered or hidden in the filter. Please wait until they are completed; I will let you know when I am finished.");
            await _booru.RefreshListsAsync(Context, settings);
            await ReplyAsync("The lists have been built. I'm all set! Type `;help admin` for a list of other admin setup commands.");
            await _logger.Log($"setup: filterId: {filterId}, channel {adminChannelName} <SUCCESS>, role {adminRoleName} <SUCCESS>", Context, true);
        }

        [Command("admin")]
        [Summary("Manages admin commands")]
        public async Task AdminCommandAsync(
            [Summary("First subcommand")] string commandOne = "",
            [Summary("Second subcommand")] string commandTwo = "",
            [Summary("Third subcommand")] string commandThree = "", 
            [Remainder] [Summary("Fourth subcommand")] int commandFour = 175)
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            switch (commandOne)
            {
                case "":
                    await ReplyAsync("You need to specify an admin command.");
                    await _logger.Log("admin: <FAIL>", Context);
                    break;
                case "filter":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await FilterGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await FilterSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "adminchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "adminrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AdminRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await AdminRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorechannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreChannelAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreChannelRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.ignoredChannels.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Ignored channels list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "filterchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await FilterChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await FilterChannelAddAsync(commandThree, commandFour, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await FilterChannelRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.filteredChannels.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Channel-specific filters cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "ignorerole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await IgnoreRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await IgnoreRoleAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await IgnoreRoleRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.ignoredRoles.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Ignored roles list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "allowuser":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await AllowUserGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "add":
                            await AllowUserAddAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "remove":
                            await AllowUserRemoveAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            settings.allowedUsers.Clear();
                            await FileHelper.SaveServerSettingsAsync(settings, Context);
                            await ReplyAsync("Allowed users list cleared.");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "yellowchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await YellowChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await YellowChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await YellowChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "yellowrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await YellowRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await YellowRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await YellowRoleClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "redchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await RedChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await RedChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await RedChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "redrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await RedRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await RedRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await RedRoleClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "reportchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await ReportChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await ReportChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await ReportChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "reportrole":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await ReportRoleGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await ReportRoleSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await ReportRoleClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                case "logchannel":
                    switch (commandTwo)
                    {
                        case "":
                            await ReplyAsync("You must specify a subcommand.");
                            await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                            break;
                        case "get":
                            await LogChannelGetAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} <SUCCESS>", Context);
                            break;
                        case "set":
                            await LogChannelSetAsync(commandThree, settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        case "clear":
                            await LogChannelClearAsync(settings);
                            await _logger.Log($"admin: {commandOne} {commandTwo} {commandThree} <SUCCESS>", Context, true);
                            break;
                        default:
                            await ReplyAsync($"Invalid command {commandTwo}");
                            await _logger.Log($"admin: {commandOne} {commandTwo} <FAIL>", Context);
                            break;
                    }

                    break;
                default:
                    await ReplyAsync($"Invalid command `{commandOne}`");
                    await _logger.Log($"admin: {commandOne} <FAIL>", Context);
                    break;
            }
        }

        [Command("echo")]
        [Summary("Posts a message to a specified channel")]
        public async Task EchoCommandAsync([Summary("The channel to send to")] string channelName = "", [Remainder] [Summary("The message to send")] string message = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            if (channelName == "")
            {
                await ReplyAsync("You must specify a channel name or a message.");
                await _logger.Log("echo: <FAIL>", Context);
                return;
            }

            var channelId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);

            if (channelId > 0)
            {
                var channel = Context.Guild.GetTextChannel(channelId);
                if (message == "")
                {
                    await ReplyAsync("There's no message to send there.");
                    await _logger.Log($"echo: {channelName} <FAIL>", Context);
                    return;
                }

                if (channel != null)
                {
                    await channel.SendMessageAsync(message);
                    await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
                    return;
                }


                await ReplyAsync("I can't send a message there.");
                await _logger.Log($"echo: {channelName} {message} <FAIL>", Context);
                return;
            }

            await ReplyAsync($"{channelName} {message}");
            await _logger.Log($"echo: {channelName} {message} <SUCCESS>", Context, true);
        }

        [Command("setprefix")]
        [Summary("Sets the bot listen prefix")]
        public async Task SetPrefixCommandAsync([Summary("The prefix character")] char prefix = ';')
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            serverPresettings.prefix = prefix;
            await ReplyAsync($"I will now listen for '{prefix}' on this server.");
            _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
            await FileHelper.SaveAllPresettingsAsync(_servers);
        }

        [Command("listentobots")]
        [Summary("Sets the bot listen prefix")]
        public async Task ListenToBotsCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            switch (command.ToLower())
            {
                case "":
                    var not = "";
                    if (!serverPresettings.listenToBots)
                    {
                        not = " not";
                    }

                    await ReplyAsync($"Currently{not} listening to bots.");
                    break;
                case "y":
                case "yes":
                case "on":
                case "true":
                    await ReplyAsync("Now listening to bots.");
                    serverPresettings.listenToBots = true;
                    _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    break;
                case "n":
                case "no":
                case "off":
                case "false":
                    await ReplyAsync("Not listening to bots.");
                    serverPresettings.listenToBots = false;
                    _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    break;
                default:
                    await ReplyAsync("Invalid command.");
                    break;
            }
        }

        [Command("safemode")]
        [Summary("Sets the safemode")]
        public async Task SafeModeCommandAsync([Summary("yes or no")] string command = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            
            switch (command.ToLower())
            {
                case "":
                    var not = "";
                    if (!settings.safeMode)
                    {
                        not = " not";
                    }

                    await ReplyAsync($"Currently{not} in Safe Mode.");
                    break;
                case "y":
                case "yes":
                case "on":
                case "true":
                    await ReplyAsync("Now in Safe Mode.");
                    settings.safeMode = true;
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    break;
                case "n":
                case "no":
                case "off":
                case "false":
                    await ReplyAsync("Now leaving Safe Mode.");
                    settings.safeMode = false;
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    break;
                default:
                    await ReplyAsync("Invalid command.");
                    break;
            }
        }

        [Command("alias")]
        [Summary("Sets an alias")]
        public async Task AliasCommandAsync(string subcommand = "", string shortForm = "", [Remainder] string longForm = "")
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            switch (subcommand)
            {
                case "":
                    await ReplyAsync("You must enter a subcommand");
                    break;
                case "get":
                    var output = $"__Current aliases:__{Environment.NewLine}";
                    foreach (var (shortFormA, longFormA) in serverPresettings.aliases)
                    {
                        output += $"`{shortFormA}`: `{longFormA}`{Environment.NewLine}";
                    }

                    await ReplyAsync(output);
                    break;
                case "add":
                    if (serverPresettings.aliases.ContainsKey(shortForm))
                    {
                        serverPresettings.aliases[shortForm] = longForm;
                        _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                        await FileHelper.SaveAllPresettingsAsync(_servers);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`, replacing what was there before.");
                    }
                    else
                    {
                        serverPresettings.aliases.Add(shortForm, longForm);
                        _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                        await FileHelper.SaveAllPresettingsAsync(_servers);
                        await ReplyAsync($"`{shortForm}` now aliased to `{longForm}`");
                    }

                    break;
                case "remove":
                    serverPresettings.aliases.Remove(shortForm);
                    _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    await ReplyAsync($"`{shortForm}` alias cleared.");
                    break;
                case "clear":
                    serverPresettings.aliases.Clear();
                    _servers.settings[Context.IsPrivate ? Context.User.Id : Context.Guild.Id] = serverPresettings;
                    await FileHelper.SaveAllPresettingsAsync(_servers);
                    await ReplyAsync("All aliases cleared.");
                    break;
                default:
                    await ReplyAsync($"Invalid subcommand {subcommand}");
                    break;
            }
        }

        [Command("getsettings")]
        [Summary("Posts the settings file to the log channel")]
        public async Task GetSettingsCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                await ReplyAsync("Cannot get settings in a DM.");
                return;
            }

            var errorMessage = await SettingsGetAsync(Context, settings);
            if (errorMessage.Contains("<ERROR>"))
            {
                await ReplyAsync(errorMessage);
                await _logger.Log($"getsettings: {errorMessage} <FAIL>", Context);
                return;
            }

            await _logger.Log($"getsettings: <SUCCESS>", Context);
        }

        private async Task<string> SettingsGetAsync(SocketCommandContext context, ServerSettings settings)
        {
            await ReplyAsync("Retrieving settings file...");
            var filepath = FileHelper.SetUpFilepath(FilePathType.Server, "settings", "conf", Context);
            if (!File.Exists(filepath))
            {
                return "<ERROR> File does not exist";
            }

            var logPostChannel = context.Guild.GetTextChannel(settings.logPostChannel);
            await logPostChannel.SendFileAsync(filepath, $"{context.Guild.Name}-settings.conf");
            return "SUCCESS";
        }

        [Command("<blank message>")]
        [Summary("Runs on a blank message")]
        public async Task BlankMessageCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await ReplyAsync("Did you need something?");
        }

        [Command("<invalid command>")]
        [Summary("Runs on an invalid command")]
        public async Task InvalidCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            await ReplyAsync("I don't know that command.");
        }

        [Command("<mention>")]
        [Summary("Runs on a name ping")]
        public async Task MentionCommandAsync()
        {
            var settings = await FileHelper.LoadServerSettingsAsync(Context);
            if (!DiscordHelper.CanUserRunThisCommand(Context, settings))
            {
                return;
            }

            var serverPresettings = await FileHelper.LoadServerPresettingsAsync(Context);
            await ReplyAsync($"The current prefix is '{serverPresettings.prefix}'. Type `{serverPresettings.prefix}help` for a list of commands.");
        }

        private async Task FilterSetAsync(string filter, ServerSettings settings)
        {
            var filterId = await _booru.CheckFilterAsync(int.Parse(filter));
            if (filterId > 0)
            {
                settings.defaultFilterId = filterId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Filter set to {filterId}. Please wait while the spoiler list and redlist are rebuilt.");
                await _booru.RefreshListsAsync(Context, settings);
                await ReplyAsync($"The lists have been refreshed for Filter {filterId}");
            }
            else
            {
                await ReplyAsync($"Invalid filter {filter}. Make sure the requested filter exists and is set to public");
            }
        }

        private async Task FilterGetAsync(ServerSettings settings)
        {
            await ReplyAsync($"The current filter is <https://manebooru.art/filters/{settings.defaultFilterId}>");
        }

        private async Task AdminChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.adminChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Admin channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task AdminChannelGetAsync(ServerSettings settings)
        {
            if (settings.adminChannel > 0)
            {
                await ReplyAsync($"Admin channel is <#{settings.adminChannel}>");
            }
            else
            {
                await ReplyAsync("Admin channel not set yet.");
            }
        }

        private async Task AdminRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.adminRole = roleSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Admin role set to <@&{roleSetId}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.");
            }
        }

        private async Task AdminRoleGetAsync(ServerSettings settings)
        {
            if (settings.adminRole > 0)
            {
                await ReplyAsync($"Admin role is <@&{settings.adminRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Admin role not set yet.");
            }
        }

        private async Task IgnoreChannelGetAsync(ServerSettings settings)
        {
            if (settings.ignoredChannels.Count > 0)
            {
                var output = $"__Channel Ignore List:__{Environment.NewLine}";
                foreach (var channel in settings.ignoredChannels)
                {
                    output += $"<#{channel}>{Environment.NewLine}";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync("No channels on ignore list.");
            }
        }

        private async Task IgnoreChannelRemoveAsync(string channelName, ServerSettings settings)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                for (var x = settings.ignoredChannels.Count - 1; x >= 0; x--)
                {
                    var channel = settings.ignoredChannels[x];
                    if (channel != channelRemoveId)
                    {
                        continue;
                    }

                    settings.ignoredChannels.Remove(channel);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from ignore list.");
                    return;
                }

                await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreChannelAddAsync(string channelName, ServerSettings settings)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelAddId > 0)
            {
                foreach (var channel in settings.ignoredChannels)
                {
                    if (channel != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<#{channelAddId}> is already on the list.");
                    return;
                }

                settings.ignoredChannels.Add(channelAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <#{channelAddId}> to ignore list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task FilterChannelGetAsync(ServerSettings settings)
        {
            if (settings.filteredChannels.Count > 0)
            {
                var output = $"__Channel-Specific Filter List:__{Environment.NewLine}(Any channel not listed here uses the server filter {settings.defaultFilterId}){Environment.NewLine}";
                foreach (var (channel, filter) in settings.filteredChannels)
                {
                    output += $"<#{channel}>: Filter {filter}{Environment.NewLine}";
                }

                await ReplyAsync(output);
            }
            else
            {
                await ReplyAsync($"No channel-specific filters are currently set. All channels use the server filter {settings.defaultFilterId}.");
            }
        }

        private async Task FilterChannelRemoveAsync(string channelName, ServerSettings settings)
        {
            var channelRemoveId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelRemoveId > 0)
            {
                for (var x = settings.filteredChannels.Count - 1; x >= 0; x--)
                {
                    var channel = settings.filteredChannels[x];
                    if (channel.Item1 != channelRemoveId)
                    {
                        continue;
                    }

                    settings.filteredChannels.Remove(channel);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <#{channelRemoveId}> from channel-specific filter list.");
                    return;
                }

                await ReplyAsync($"<#{channelRemoveId}> was not on the list.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task FilterChannelAddAsync(string channelName, int filterId, ServerSettings settings)
        {
            var channelAddId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelAddId > 0)
            {
                foreach (var channel in settings.filteredChannels)
                {
                    if (channel.Item1 != channelAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"Updated the filter for <#{channelAddId}> from {channel.Item2} to {filterId}.");
                    settings.filteredChannels.Remove(channel);
                    settings.filteredChannels.Add(new Tuple<ulong, int>(channelAddId, filterId));
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    return;
                }

                settings.filteredChannels.Add(new Tuple<ulong, int>(channelAddId, filterId));
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Set <#{channelAddId}> to use filter {filterId}.");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task IgnoreRoleRemoveAsync(string roleName, ServerSettings settings)
        {
            var roleRemoveId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleRemoveId > 0)
            {
                for (var x = settings.ignoredRoles.Count - 1; x >= 0; x--)
                {
                    var role = settings.ignoredRoles[x];
                    if (role != roleRemoveId)
                    {
                        continue;
                    }

                    settings.ignoredRoles.Remove(role);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <@&{roleRemoveId}> from ignore list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                await ReplyAsync($"<@&{roleRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid channel name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleAddAsync(string roleName, ServerSettings settings)
        {
            var roleAddId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleAddId > 0)
            {
                foreach (var role in settings.ignoredRoles)
                {
                    if (role != roleAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@&{roleAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.ignoredRoles.Add(roleAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <@&{roleAddId}> to ignore list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name @{roleName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task IgnoreRoleGetAsync(ServerSettings settings)
        {
            if (settings.ignoredRoles.Count > 0)
            {
                var output = $"__Role Ignore List:__{Environment.NewLine}";
                foreach (var role in settings.ignoredRoles)
                {
                    output += $"<@&{role}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No roles on ignore list.");
            }
        }

        private async Task AllowUserRemoveAsync(string userName, ServerSettings settings)
        {
            var userRemoveId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userRemoveId > 0)
            {
                for (var x = settings.allowedUsers.Count - 1; x >= 0; x--)
                {
                    var user = settings.allowedUsers[x];
                    if (user != userRemoveId)
                    {
                        continue;
                    }

                    settings.allowedUsers.Remove(user);
                    await FileHelper.SaveServerSettingsAsync(settings, Context);
                    await ReplyAsync($"Removed <@{userRemoveId}> from allow list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                await ReplyAsync($"<@{userRemoveId}> was not on the list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserAddAsync(string userName, ServerSettings settings)
        {
            var userAddId = await DiscordHelper.GeUserIdFromPingOrIfOnlySearchResultAsync(userName, Context);
            if (userAddId > 0)
            {
                foreach (var user in settings.allowedUsers)
                {
                    if (user != userAddId)
                    {
                        continue;
                    }

                    await ReplyAsync($"<@{userAddId}> is already on the list.", allowedMentions: AllowedMentions.None);
                    return;
                }

                settings.allowedUsers.Add(userAddId);
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Added <@{userAddId}> to allow list.", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid user name @{userName}.", allowedMentions: AllowedMentions.None);
            }
        }

        private async Task AllowUserGetAsync(ServerSettings settings)
        {
            if (settings.allowedUsers.Count > 0)
            {
                var output = $"__Allowed User List:__{Environment.NewLine}";
                foreach (var user in settings.allowedUsers)
                {
                    output += $"<@{user}>{Environment.NewLine}";
                }

                await ReplyAsync(output, allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("No users on allow list.");
            }
        }

        private async Task YellowChannelClearAsync(ServerSettings settings)
        {
            settings.yellowAlertChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Yellow alert channel reset to the current admin channel, <#{settings.yellowAlertChannel}>");
        }

        private async Task YellowChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.yellowAlertChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Yellow alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task YellowChannelGetAsync(ServerSettings settings)
        {
            if (settings.yellowAlertChannel > 0)
            {
                await ReplyAsync($"Yellow alerts are being posted in <#{settings.yellowAlertChannel}>");
            }
            else
            {
                await ReplyAsync("Yellow alert channel not set yet.");
            }
        }

        private async Task YellowRoleClearAsync(ServerSettings settings)
        {
            settings.yellowAlertRole = 0;
            settings.yellowPing = false;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Yellow alerts will not ping anyone now in <#{settings.yellowAlertChannel}>");
        }

        private async Task YellowRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.yellowAlertRole = roleSetId;
                settings.yellowPing = true;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Yellow alerts will now ping <@&{settings.yellowAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name #{roleName}.");
            }
        }

        private async Task YellowRoleGetAsync(ServerSettings settings)
        {
            if (settings.yellowAlertRole > 0)
            {
                await ReplyAsync($"Yellow alerts will ping <@&{settings.yellowAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Yellow alert role not set yet.");
            }
        }

        private async Task RedChannelClearAsync(ServerSettings settings)
        {
            settings.redAlertChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Red alert channel reset to the current admin channel, <#{settings.redAlertChannel}>");
        }

        private async Task RedChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.redAlertChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Red alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task RedChannelGetAsync(ServerSettings settings)
        {
            if (settings.redAlertChannel > 0)
            {
                await ReplyAsync($"Red alerts are being posted in <#{settings.redAlertChannel}>");
            }
            else
            {
                await ReplyAsync("Red alert channel not set yet.");
            }
        }

        private async Task RedRoleClearAsync(ServerSettings settings)
        {
            settings.redAlertRole = 0;
            settings.redPing = false;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Red alerts will not ping anyone now in <#{settings.redAlertChannel}>");
        }

        private async Task RedRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.redAlertRole = roleSetId;
                settings.redPing = true;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Red alerts will now ping <@&{settings.redAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name #{roleName}.");
            }
        }

        private async Task RedRoleGetAsync(ServerSettings settings)
        {
            if (settings.redAlertRole > 0)
            {
                await ReplyAsync($"Red alerts will ping <@&{settings.redAlertRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Red alert role not set yet.");
            }
        }

        private async Task ReportChannelClearAsync(ServerSettings settings)
        {
            settings.reportChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alert channel reset to the current admin channel, <#{settings.reportChannel}>");
        }

        private async Task ReportChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.reportChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Report alert channel set to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task ReportChannelGetAsync(ServerSettings settings)
        {
            if (settings.reportChannel > 0)
            {
                await ReplyAsync($"Report alerts are being posted in <#{settings.reportChannel}>");
            }
            else
            {
                await ReplyAsync("Report alert channel not set yet.");
            }
        }

        private async Task ReportRoleClearAsync(ServerSettings settings)
        {
            settings.reportRole = 0;
            settings.reportPing = false;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alerts will not ping anyone now in <#{settings.reportChannel}>");
        }

        private async Task ReportRoleSetAsync(string roleName, ServerSettings settings)
        {
            var roleSetId = DiscordHelper.GetRoleIdIfAccessAsync(roleName, Context);
            if (roleSetId > 0)
            {
                settings.reportRole = roleSetId;
                settings.reportPing = true;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Report alerts will now ping <@&{settings.reportRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync($"Invalid role name #{roleName}.");
            }
        }

        private async Task ReportRoleGetAsync(ServerSettings settings)
        {
            if (settings.reportRole > 0)
            {
                await ReplyAsync($"Report alerts will ping <@&{settings.reportRole}>", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await ReplyAsync("Report alert role not set yet.");
            }
        }

        private async Task LogChannelClearAsync(ServerSettings settings)
        {
            settings.logPostChannel = settings.adminChannel;
            await FileHelper.SaveServerSettingsAsync(settings, Context);
            await ReplyAsync($"Report alert channel reset to the current admin channel, <#{settings.logPostChannel}>");
        }

        private async Task LogChannelSetAsync(string channelName, ServerSettings settings)
        {
            var channelSetId = await DiscordHelper.GetChannelIdIfAccessAsync(channelName, Context);
            if (channelSetId > 0)
            {
                settings.logPostChannel = channelSetId;
                await FileHelper.SaveServerSettingsAsync(settings, Context);
                await ReplyAsync($"Retrieved logs will be sent to <#{channelSetId}>");
            }
            else
            {
                await ReplyAsync($"Invalid channel name #{channelName}.");
            }
        }

        private async Task LogChannelGetAsync(ServerSettings settings)
        {
            if (settings.logPostChannel > 0)
            {
                await ReplyAsync($"Logs are being posted in <#{settings.logPostChannel}>");
            }
            else
            {
                await ReplyAsync("Log posting channel not set yet.");
            }
        }

        [Summary("Submodule for managing the yellowlist")]
        public class BadlistModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;

            public BadlistModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("yellowlist")]
            [Summary("Manages the search term yellowlist")]
            public async Task YellowListCommandAsync([Summary("Subcommand")] string command = "", [Remainder] [Summary("Search term")] string term = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
                {
                    return;
                }

                switch (command)
                {
                    case "":
                        await ReplyAsync("You must specify a subcommand.");
                        await _logger.Log("yellowlist: <FAIL>", Context);
                        break;
                    case "add":
                        var (addList, failList) = await BadlistHelper.AddYellowTerm(term, settings, Context);
                        if (failList.Count == 0)
                        {
                            var addOutput = "";
                            for (var x = 0; x < addList.Count; x++)
                            {
                                var addedTerm = addList[x];
                                addOutput += $"`{addedTerm}`";
                                if (x < addList.Count - 2)
                                {
                                    addOutput += ", ";
                                }

                                if (x == addList.Count - 2)
                                {
                                    addOutput += ", and ";
                                }
                            }

                            await ReplyAsync($"Added {addOutput} to the yellowlist.");
                            await _logger.Log($"yellowlist: add {addOutput} <SUCCESS>", Context, true);
                        }
                        else if (addList.Count == 0)
                        {
                            await ReplyAsync("All terms entered are already on the yellowlist.");
                            await _logger.Log($"yellowlist: add <FAIL> {term}", Context);
                        }
                        else
                        {
                            var failOutput = "";
                            var addOutput = "";
                            for (var x = 0; x < addList.Count; x++)
                            {
                                var addedTerm = addList[x];
                                addOutput += $"`{addedTerm}`";
                                if (x < addList.Count - 2)
                                {
                                    addOutput += ", ";
                                }

                                if (x == addList.Count - 2)
                                {
                                    addOutput += ", and ";
                                }
                            }

                            for (var x = 0; x < failList.Count; x++)
                            {
                                var failedTerm = failList[x];
                                failOutput += $"`{failedTerm}`";
                                if (x < failList.Count - 2)
                                {
                                    failOutput += ", ";
                                }

                                if (x == failList.Count - 2)
                                {
                                    failOutput += ", and ";
                                }
                            }

                            await ReplyAsync($"Added {addOutput} to the yellowlist, and the yellowlist already contained {failOutput}.");
                            await _logger.Log($"yellowlist: add {addOutput} <FAIL> {failOutput}", Context);
                        }

                        break;
                    case "remove":
                        var removed = await BadlistHelper.RemoveYellowTerm(term, settings, Context);
                        if (removed)
                        {
                            await ReplyAsync($"Removed `{term}` from the yellowlist.");
                            await _logger.Log($"yellowlist: remove {term} <SUCCESS>", Context, true);
                        }
                        else
                        {
                            await ReplyAsync($"`{term}` was not on the yellowlist.");
                            await _logger.Log($"yellowlist: remove {term} <FAIL>", Context);
                        }

                        break;
                    case "get":
                        var output = "The yellowlist is currently empty.";
                        foreach (var item in settings.yellowList)
                        {
                            if (output == "The yellowlist is currently empty.")
                            {
                                output = $"`{item}`";
                            }
                            else
                            {
                                output += $", `{item}`";
                            }
                        }

                        await ReplyAsync($"__Yellowlist Terms:__{Environment.NewLine}{output}");
                        await _logger.Log("yellowlist: get", Context);
                        break;
                    case "clear":
                        settings.yellowList.Clear();
                        await FileHelper.SaveServerSettingsAsync(settings, Context);
                        await ReplyAsync("Yellowlist cleared");
                        await _logger.Log("yellowlist: clear", Context, true);
                        break;
                    default:
                        await ReplyAsync("Invalid subcommand");
                        await _logger.Log($"yellowlist: {command} <FAIL>", Context);
                        break;
                }
            }
        }

        [Summary("Submodule for retreiving log files")]
        public class LogModule : ModuleBase<SocketCommandContext>
        {
            private readonly LoggingService _logger;

            public LogModule(LoggingService logger)
            {
                _logger = logger;
            }

            [Command("log")]
            [Summary("Retrieves a log file")]
            public async Task LogCommandAsync(
                [Summary("The channel to get the log from")] string channel = "",
                [Summary("The date (in format (YYYY-MM-DD) to get the log from")] string date = "")
            {
                var settings = await FileHelper.LoadServerSettingsAsync(Context);
                if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
                {
                    return;
                }

                if (Context.IsPrivate)
                {
                    await ReplyAsync("Cannot get logs in a DM.");
                    return;
                }

                if (channel == "")
                {
                    await ReplyAsync("You need to enter a channel and date.");
                    await _logger.Log("log: <FAIL>", Context);
                    return;
                }

                if (date == "")
                {
                    await ReplyAsync("You need to enter a date.");
                    await _logger.Log($"log: {channel} <FAIL>", Context);
                    return;
                }

                var errorMessage = await LogGetAsync(channel, date, Context, settings);
                if (errorMessage.Contains("<ERROR>"))
                {
                    await ReplyAsync(errorMessage);
                    await _logger.Log($"log: {channel} {date} {errorMessage} <FAIL>", Context);
                    return;
                }

                await _logger.Log($"log: {channel} {date} <SUCCESS>", Context);
            }

            private async Task<string> LogGetAsync(string channelName, string date, SocketCommandContext context, ServerSettings settings)
            {
                await ReplyAsync($"Retrieving log from {channelName} on {date}...");
                var confirmedName = DiscordHelper.ConvertChannelPingToName(channelName, context);
                if (confirmedName.Contains("<ERROR>"))
                {
                    return confirmedName;
                }

                if (settings.logPostChannel <= 0)
                {
                    return "<ERROR> Log post channel not set.";
                }

                var filepath = FileHelper.SetUpFilepath(FilePathType.LogRetrieval, date, "log", context, confirmedName, date);
                if (!File.Exists(filepath))
                {
                    return "<ERROR> File does not exist";
                }

                var logPostChannel = context.Guild.GetTextChannel(settings.logPostChannel);
                await logPostChannel.SendFileAsync(filepath, $"{confirmedName}-{date}.log");
                return "SUCCESS";
            }
        }
    }
}
