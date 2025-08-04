﻿using Shared;
using System;
using WorldServer.core.objects;
using WorldServer.core.worlds;

namespace WorldServer.core.commands
{
    public abstract partial class Command
    {
        internal class NameColor : Command
        {
            public override RankingType RankRequirement => RankingType.Donator;
            public override string CommandName => "namecolor";

            protected override bool Process(Player player, TickTime time, string color)
            {
                if (string.IsNullOrWhiteSpace(color))
                {
                    player.SendInfo("Usage: /namecolor <color> \n (use hexcode format: 0xFFFFFF)");
                    return true;
                }

                player.ColorNameChat = Utils.FromString(color);

                var acc = player.Client.Account;
                acc.ColorNameChat = player.ColorNameChat;
                acc.FlushAsync();

                return true;
            }
        }
    }
}
