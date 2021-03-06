﻿using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace MKOTHDiscordBot
{
    public class RequireDeveloperAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User.Id == ApplicationContext.BotOwner.Id) // Or more in the future
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command can only be used by the developer of the bot."));
            }
        }

        public override string ToString()
        {
            return "Require developer";
        }
    }
}
