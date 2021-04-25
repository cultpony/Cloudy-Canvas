﻿namespace Cloudy_Canvas.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloudy_Canvas.Helpers;
    using Cloudy_Canvas.Service;
    using Cloudy_Canvas.Settings;
    using Discord.Commands;

    [Summary("Module for interfacing with Manebooru")]
    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        private readonly BooruService _booru;
        private readonly LoggingService _logger;

        public BooruModule(BooruService booru, LoggingService logger)
        {
            _booru = booru;
            _logger = logger;
        }

        [Command("pick")]
        [Summary("Selects an image at random")]
        public async Task PickAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            if (!await CheckBadlists(query, settings))
            {
                return;
            }

            var (imageId, total, spoilered, spoilerList) = await _booru.GetRandomImageByQueryAsync(query, settings);
            if (total == 0)
            {
                await _logger.Log($"pick: {query}, total: {total}", Context);
                await ReplyAsync("I could not find any images with that query.");
            }
            else
            {
                var totalString = $"[{total} result";
                if (total == 1)
                {
                    totalString += "] ";
                }
                else
                {
                    totalString += "s] ";
                }

                totalString += $"[Id# {imageId}] ";

                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = totalString + $"Spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"pick: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    var output = totalString + $"https://manebooru.art/images/{imageId}";
                    await _logger.Log($"pick: {query}, total: {total} result: {imageId}", Context);
                    await ReplyAsync(output);
                }
            }
        }

        [Command("pickrecent")]
        [Summary("Selects first image in a search")]
        public async Task PickRecentAsync([Remainder] [Summary("Query string")] string query = "*")
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            if (!await CheckBadlists(query, settings))
            {
                return;
            }

            var (imageId, total, spoilered, spoilerList) = await _booru.GetFirstRecentImageByQueryAsync(query, settings);
            if (total == 0)
            {
                await _logger.Log($"pickrecent: {query}, total: {total}", Context);
                await ReplyAsync("I could not find any images with that query.");
            }
            else
            {
                var totalString = $"[{total} result";
                if (total == 1)
                {
                    totalString += "] ";
                }
                else
                {
                    totalString += "s] ";
                }

                totalString += $"[Id# {imageId}] ";

                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = totalString + $"Spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    var output = totalString + $"https://manebooru.art/images/{imageId}";
                    await _logger.Log($"pickrecent: {query}, total: {total} result: {imageId}", Context);
                    await ReplyAsync(output);
                }
            }
        }

        [Command("id")]
        [Summary("Selects an image by image id")]
        public async Task IdAsync([Summary("The image Id")] long id = 4010266)
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            if (!await CheckBadlists(id.ToString(), settings))
            {
                return;
            }

            var (imageId, spoilered, spoilerList) = await _booru.GetImageByIdAsync(id, settings);
            if (imageId == -1)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"id: requested {id}, NOT FOUND", Context);
            }
            else
            {
                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    var output = $"[Id# {imageId}] Result is a spoiler for {spoilerStrings}:{Environment.NewLine}|| https://manebooru.art/images/{imageId} ||";
                    await _logger.Log($"id: requested {id}, found {imageId} SPOILERED {spoilerStrings}", Context);
                    await ReplyAsync(output);
                }
                else
                {
                    await _logger.Log($"id: requested {id}, found {imageId}", Context);
                    await ReplyAsync($"[Id# {imageId}] https://manebooru.art/images/{imageId}");
                }
            }
        }

        [Command("tags")]
        [Summary("Selects a tag list by image Id")]
        public async Task TagsAsync([Summary("The image Id")] long id = 4010266)
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            if (!await CheckBadlists(id.ToString(), settings))
            {
                return;
            }

            var (tagList, spoilered, spoilerList) = await _booru.GetImageTagsIdAsync(id, settings);
            if (tagList.Count == 0)
            {
                await ReplyAsync("I could not find that image.");
                await _logger.Log($"tags: requested {id}, NOT FOUND", Context);
            }
            else
            {
                var tagStrings = SetupTagListOutput(tagList);
                var output = $"Image #{id} has the tags {tagStrings}";
                if (spoilered)
                {
                    var spoilerStrings = SetupTagListOutput(spoilerList);
                    output += $" including the spoiler tags {spoilerStrings}";
                    await _logger.Log($"tags: requested {id}, found {tagStrings} SPOILERED {spoilerStrings}", Context);
                }
                else
                {
                    await _logger.Log($"tags: requested {id}, found {tagStrings}", Context);
                }

                await ReplyAsync(output);
            }
        }

        [Command("getspoilers")]
        [Summary("Gets the list of spoiler tags")]
        public async Task GetSpoilersAsync()
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            var output = $"__Spoilered tags:__{Environment.NewLine}";
            for (var x = 0; x < settings.spoilerList.Count; x++)
            {
                output += $"`{settings.spoilerList[x]}`";
                if (x < settings.spoilerList.Count - 1)
                {
                    output += ", ";
                }
            }

            await _logger.Log("getspoilers", Context);
            await ReplyAsync(output);
        }

        [Command("featured")]
        [Summary("Selects the current Featured Image on Manebooru")]
        public async Task FeaturedAsync()
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            var featured = await _booru.GetFeaturedImageIdAsync();
            await _logger.Log("featured", Context);
            await ReplyAsync($"[Id# {featured}] https://manebooru.art/images/{featured}");
        }

        [Command("report")]
        [Summary("Reports an image id to the admin channel")]
        public async Task ReportAsync(long reportedImageId, [Remainder] string reason = "")
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.CanUserRunThisCommandAsync(Context, settings))
            {
                return;
            }

            var badTerms = BadlistHelper.CheckYellowList(reportedImageId.ToString(), settings);
            if (badTerms != "")
            {
                await ReplyAsync("That image is already blocked.");
            }
            else
            {
                var (imageId, _, _) = await _booru.GetImageByIdAsync(reportedImageId, settings);
                if (imageId == -1)
                {
                    await ReplyAsync("I could not find that image.");
                }
                else
                {
                    var output = $"<@{Context.User.Id}> has reported Image #{reportedImageId}";
                    if (reason != "")
                    {
                        output += $" with reason `{reason}`";
                    }

                    output += $" || <https://manebooru.art/images/{imageId}> ||";
                    await _logger.Log($"report: {reportedImageId} <SUCCESS>", Context, true);
                    await DiscordHelper.PostToAdminChannelAsync(output, Context, true);
                    await ReplyAsync("Admins have been notified. Thank you for your report.");
                }
            }
        }

        [Command("refreshlists")]
        [Summary("Refreshes the spoiler list and redlist")]
        public async Task RefreshListsAsync()
        {
            var settings = await FileHelper.LoadServerSettings(Context);
            if (!DiscordHelper.DoesUserHaveAdminRoleAsync(Context, settings))
            {
                return;
            }

            await ReplyAsync("Refreshing spoiler list and redlist. This may take a few minutes.");
            await _booru.RefreshListsAsync(Context);
            await ReplyAsync("Spoiler list and redlist refreshed!");
        }

        private static string SetupTagListOutput(IReadOnlyList<string> tagList)
        {
            var output = "";
            for (var x = 0; x < tagList.Count; x++)
            {
                var spoilerTerm = tagList[x];
                output += $"`{spoilerTerm}`";
                if (x < tagList.Count - 1)
                {
                    output += ", ";
                }
            }

            return output;
        }

        private async Task<bool> CheckBadlists(string query, ServerSettings settings)
        {
            var yellowTerms = BadlistHelper.CheckYellowList(query, settings);
            var redTerms = BadlistHelper.CheckRedList(query, settings);
            if (redTerms != "")
            {
                await _logger.Log($"pick: {query}, REDLISTED {redTerms[0]}", Context, true);
                await ReplyAsync("You're kidding me, right?");
                await DiscordHelper.PostToAdminChannelAsync($"<@{Context.User.Id}> searched for a banned term in <#{Context.Channel.Id}> RED TERMS: {redTerms[0]}", Context, true);
                await Context.Message.DeleteAsync();
                return false;
            }

            if (yellowTerms != "")
            {
                await _logger.Log($"pick: {query}, YELLOWLISTED {yellowTerms[0]}", Context, true);
                await ReplyAsync("I'm not gonna go look for that.");
                await DiscordHelper.PostToAdminChannelAsync($"<@{Context.User.Id}> searched for a naughty term in <#{Context.Channel.Id}> YELLOW TERMS: {yellowTerms[0]}", Context,
                    true);
                return false;
            }

            return true;
        }
    }
}
