﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot.Modules
{
    using static Globals.MKOTHGuild;

    [Summary("Contains the utilities for MKOTH needs and management.")]
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("UpdateMKOTH", RunMode = RunMode.Async)]
        [Summary("To be deprecated. Manually refresh the MKOTH member roles and nicknames.")]
        [RequireMKOTHMod]
        public async Task Updatemkoth()
        {
            await UpdateMKOTHAsync(Context);
        }

        [Command("MyId")]
        [Alias("MyFormId", "MySubmissionId", "MySubmissionCode", "What is My Id", "What is My Id?", "What is My MKOTH Id", "What is My MKOTH Id?")]
        [Summary("Sends your unique personal MKOTH series submission form identification code, if you are a MKOTH Member.")]
        public async Task MyID()
        {
            int code = Player.FetchCode(Context.User.Id);

            if (Player.Fetch(Context.User.Id).IsUnknown)
            {
                await ReplyAsync(Context.User.Mention + ", You are not a MKOTH Member!");
                return;
            }
            if (!Context.IsPrivate)
            {
                await ReplyAsync(Context.User.Mention + ", your code will now be sent to your direct message.");
            }
            if (code != 0)
            {
                await Context.User.SendMessageAsync("Your Identification for submission form is below. Please keep the code secret.");
                await Context.User.SendMessageAsync(code.ToString());
                Logger.Log("Sent code to " + Context.User.Username.AddTab().AddLine() + $"Discord Id: {Context.User.Id}" + code.ToString(), LogType.DIRECTMESSAGE);
            }
            else
            {
                await Context.User.SendMessageAsync("Your Identification is not found, please dm an admin for assistance");
                Logger.Log("Sent code to " + Context.User.Username.AddTab().AddLine() + $"Discord Id: {Context.User.Id}" + "Code not found/not member", LogType.DIRECTMESSAGE);
            }
        }

        [Command("MissingMembers")]
        [Alias("ListMissingMembers", "mm")]
        [Summary("Lists the MKOTH Members who are missing from the discord server.")]
        public async Task MissingMembers()
        {
            try
            {
                EmbedBuilder embed = new EmbedBuilder();
                IUserMessage msg;
                var playerlist = Player.List.Where(x => !x.IsRemoved).ToList();
                foreach (var user in Guild.Users)
                {
                    var index = playerlist.FindIndex(x => x.DiscordId == user.Id);
                    if (index > -1)
                    {
                        playerlist.RemoveAt(index);
                    }
                }

                var activemissinglist = playerlist.Where(x => !x.IsHoliday).ToList();
                var holidaymissinglist = playerlist.Where(x => x.IsHoliday).ToList();

                string activemisinglistfield = "";
                string holidaymisinglistfield = "";
                foreach (var item in activemissinglist)
                {
                    activemisinglistfield += $"{item.Class}: {item.Name}\n";
                }
                foreach (var item in holidaymissinglist)
                {
                    holidaymisinglistfield += $"{item.Class}: {item.Name}\n";
                }
                embed.Title = "Missing MKOTH Members from MKOTH Discord Server";
                embed.Description = "MKOTH Members who are not in the discord server but still remain active or in holiday in the MKOTH Ranking.";
                embed.AddField(activemissinglist.Count + " Active Members", $"```{activemisinglistfield}```");
                embed.AddField(holidaymissinglist.Count + " Holiday Members", $"```{holidaymisinglistfield}```");
                embed.Color = Color.Orange;

                msg = await ReplyAsync(string.Empty, embed: embed.Build());
            }
            catch (Exception e)
            {
                await Logger.SendError(e);
            }
        }

        public static async Task UpdateMKOTHAsync(SocketCommandContext context)
        {
            var starttime = DateTime.Now;

            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg = null;

            try
            {
                embed.Title = "Role Pools";
                embed.Description = $"{ChatMods.Name}\n{Member.Name}\n{Peasant.Name}\n{Vassal.Name}\n{Squire.Name}\n{Noble.Name}\n{King.Name}\n";
                if (context != null)
                {
                    msg = await context.Channel.SendMessageAsync("Updating Member Roles and Names", embed: embed.Build());
                    await Player.Load();
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Player.List, Newtonsoft.Json.Formatting.Indented));
                }

                int count = 0;
                foreach (var serveruser in Guild.Users)
                {
                    count++;
                    var player = Player.Fetch(serveruser.Id);
                    if (!player.IsUnknown && !player.IsRemoved && !serveruser.Roles.Contains(Member))
                    {
                        await serveruser.AddRoleAsync(Member);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Add MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    if (!player.IsUnknown && player.IsRemoved && serveruser.Roles.Contains(Member))
                    {
                        await serveruser.RemoveRoleAsync(Member);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Remove MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    else if (!player.IsUnknown && !player.IsRemoved && !serveruser.Roles.Contains(Stupid))
                    {
                        if (serveruser.GetDisplayName() != player.Name && !serveruser.Roles.Contains(ChatMods))
                        {
                            if (context != null)
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " => " + player.Name;
                                    x.Embed = embed.Build();
                                });
                            }
                            await serveruser.ModifyAsync(x => { x.Nickname = player.Name; });
                        }
                        switch (player.Class)
                        {
                            case PlayerClass.KING:
                                if (!serveruser.Roles.Contains(King))
                                {
                                    await serveruser.AddRoleAsync(King);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.NOBLE:
                                if (serveruser.Roles.Contains(King) || serveruser.Roles.Contains(Squire) || !serveruser.Roles.Contains(Noble))
                                {
                                    await serveruser.AddRoleAsync(Noble);
                                    await serveruser.RemoveRoleAsync(King);
                                    await serveruser.RemoveRoleAsync(Squire);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.SQUIRE:
                                if (serveruser.Roles.Contains(Noble) || serveruser.Roles.Contains(Vassal) || !serveruser.Roles.Contains(Squire))
                                {
                                    await serveruser.AddRoleAsync(Squire);
                                    await serveruser.RemoveRoleAsync(Noble);
                                    await serveruser.RemoveRoleAsync(Vassal);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.VASSAL:
                                if (serveruser.Roles.Contains(Squire) || serveruser.Roles.Contains(Peasant) || !serveruser.Roles.Contains(Vassal))
                                {
                                    await serveruser.AddRoleAsync(Vassal);
                                    await serveruser.RemoveRoleAsync(Squire);
                                    await serveruser.RemoveRoleAsync(Peasant);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.PEASANT:
                                if (serveruser.Roles.Contains(Vassal) || !serveruser.Roles.Contains(Peasant))
                                {
                                    await serveruser.AddRoleAsync(Peasant);
                                    await serveruser.RemoveRoleAsync(Vassal);
                                    updateRoleProgressStatus();
                                }
                                break;
                        }

                        async void updateRoleProgressStatus()
                        {
                            if (context != null)
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " Update Role";
                                    x.Embed = embed.Build();
                                });
                            }
                        }
                    }
                }
                if (context != null)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = "Updating nicknames and roles COMPLETED!";
                        embed.Description = embed.Description;
                        x.Embed = embed.Build();
                    });
                }
                Logger.Debug((DateTime.Now - starttime).TotalMilliseconds + " ms", "Update MKOTH Run");
            }
            catch (Exception e)
            {
                await Logger.SendError(e);
            }
        }
    }
}
