﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides guidance of using the MKOTH Discord Bot.")]
    [Remarks("Module A")]
    public class Help : ModuleBase<SocketCommandContext>
    {
        private CommandService commands;

        public Help(CommandService _commands)
        {
            commands = _commands;
        }

        [Command("Help")]
        [Alias("H", "Manual")]
        [Summary("Displays the help information.")]
        public async Task MKOTHHelp()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("❓ Manual")
                .WithDescription("This shows the list of command modules. " +
                "A command module is a catergory for a group of related commands.\n" +
                "Use `.help <module>` to show the commands in the module.\n" +
                "Use `.help <.command>` to show the details of the command.\n" +
                "Most commands will also come with alias(abbreviation) to ease typing, e.g `.h` is the same for `.help`. " +
                "Alias for a command can be found in the command details of it.");

            commands.Modules.OrderBy(x => x.Remarks ?? "Module Z").ToList().ForEach(x =>
            {
                embed.AddField(x.Name, x.Summary ?? "In Development".MarkdownCodeBlock());
            });

            await ReplyAsync(string.Empty, false, embed.Build());
        }

        [Command("Help")]
        [Alias("H", "Info")]
        [Summary("Use with an input `<para>`(module or command name) to find the details and usage about a module or a command.")]
        public async Task MKOTHHelp([Remainder]string para)
        {
            para = para.ToLower();
            var embed = new EmbedBuilder().WithColor(Color.Orange);

            var module = commands.Modules.ToList().Find(x => x.Name.ToLower().StartsWith(para));
            if (module != null)
            {
                string commandList = "";
                module.Commands.ToList().ForEach(x => commandList += $".{x.Name.AddSpace() + x.GetCommandParametersInfo()}\n");
                embed.WithAuthor("📦 Module information")
                    .WithTitle(module.Name)
                    .WithDescription(module.Summary ?? "In Development".MarkdownCodeBlock())
                    .AddField("Commands", commandList.MarkdownCodeBlock("css"));
                goto helpReplyProcedure;
            }

            para = para.StartsWith(".") ? para.TrimStart('.') : para;
            var command = commands.Commands.ToList().FindAll(x => x.Name.ToLower() == para || x.Aliases.ToList().Find(y => y.ToLower() == para) != null);
            command = para == "info" ? command.FindAll(x => x.Name == "Info").ToList() : command;
            if (command.Count > 0)
            {
                var baseCommand = command.First();
                string commandDescription = "";
                command.ForEach(x =>
                {
                    commandDescription = 
                    !commandDescription.Contains(x.Summary ?? "") ? 
                    commandDescription + (x.Summary == null ? "" : x.Summary.AddSpace()) : commandDescription;
                });
                commandDescription = commandDescription == "" ? "In Development".MarkdownCodeBlock() : commandDescription;
                embed.WithAuthor("📃 Command information")
                    .WithTitle(baseCommand.Name)
                    .WithDescription(commandDescription);
                if (baseCommand.Aliases.Count > 0)
                {
                    string alias = "";
                    baseCommand.Aliases.ToList().ForEach(x =>
                    {
                        alias += $"{("." + x).MarkdownCodeLine()}\t";
                    });
                    embed.AddField("Alias", alias);
                }
                string restrictions = null;
                baseCommand.Module.Preconditions.ToList().ForEach(x => restrictions += x.ToString().MarkdownCodeLine().AddLine());
                baseCommand.Preconditions.ToList().ForEach(x => restrictions += x.ToString().MarkdownCodeLine().AddLine());
                if (restrictions != null)
                {
                    embed.AddField("Restrictions", restrictions);
                }

                string usage = "";
                command.ForEach(x => usage += $".{x.Name.AddSpace() + x.GetCommandParametersInfo()}\n");
                embed.AddField("Usage", usage.MarkdownCodeBlock("css"));

                embed.WithFooter($"📦 {baseCommand.Module.Name} module");

                goto helpReplyProcedure;
            }

            embed.WithDescription("🔎 module / command not found.");

            helpReplyProcedure:
            await ReplyAsync(string.Empty, false, embed.Build());
        }
    }
}
