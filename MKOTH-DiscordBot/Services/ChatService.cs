﻿using System;
using System.Linq;
using System.Threading.Tasks;

using Cerlancism.ChatSystem;

using Discord;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Services
{
    public class ChatService : IDisposable
    {
        public readonly Chat ChatSystem;

        private readonly ResponseService responseService;
        private readonly DiscordLogger discordLogger;
        private readonly ulong officialChat;

        public ChatService(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            responseService = services.GetService<ResponseService>();
            discordLogger = services.GetService<DiscordLogger>();

            officialChat = appSettings.Value.Settings.ProductionGuild.Official;

            ChatSystem = new Chat(appSettings.Value.ConnectionStrings.ChatHistory);
            ChatSystem.Log += HandleLog;
        }

        public async Task AddSync(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return;
            }

            if (context.User.IsWebhook)
            {
                return;
            }

            if (context.Channel.Id != officialChat)
            {
                return;
            }

            var message = context.Message.Content;
            if (context.Message.MentionedUsers.Count > 0)
            {
                if (context.Message.MentionedUsers.Contains(context.Client.CurrentUser))
                {
                    return;
                }
                string CleanMessage = context.Message.Content;
                foreach (var user in context.Message.MentionedUsers)
                {
                    CleanMessage = CleanMessage.Replace("<@" + user.Id.ToString(), "<@!" + user.Id.ToString());
                    CleanMessage = CleanMessage.Replace(user.Mention, user.Username);
                }
                message = CleanMessage.Trim();
            }
            message = message.Replace("@", "`@`");

            await ChatSystem.AddAsync(context.User.Id, message);
        }

        public async Task ReplyAsync(SocketCommandContext context, string message)
        {
            if (message.Length < 2)
            {
                return;
            }

            var typing = responseService.StartTypingAsync(context);

            var delay = Task.Delay(500);
            var reply = await ChatSystem.ReplyAsync(message);
            //reply = UwuTranslator.Translate(reply);
            reply = reply.SliceBack(1900);

            await delay;

            await responseService.SendToContextAsync(context, reply, typing);

            if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                await responseService.SendToChannelAsync(discordLogger.LogChannel, "DM chat received:", new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription(message)
                    .AddField("Response", reply)
                    .Build());
            }
        }

        void HandleLog(string log)
        {
            Logger.Log(log, LogType.TrashReply);
        }

        public void Dispose()
        {
            Logger.Debug("Disposed", "ChatSystem");
            ChatSystem.Log -= HandleLog;
            ChatSystem.Dispose();
        }
    }
}
