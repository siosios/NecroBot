﻿using System;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Networking.Responses;

namespace PoGo.NecroBot.Logic.Tasks
{
    public class UseLuckyEggTask
    {
        public static async Task Execute(ISession session)
        {
            var response = await session.Client.Inventory.UseItemXpBoost().ConfigureAwait(false);
            switch (response.Result)
            {
                case UseItemXpBoostResponse.Types.Result.Success:
                    Logger.Write($"Using a Lucky Egg");
                    Logger.Write($"Lucky Egg is valid until: {DateTime.Now.AddMinutes(30)}");
                    break;
                case UseItemXpBoostResponse.Types.Result.ErrorXpBoostAlreadyActive:
                    Logger.Write($"A Lucky Egg is already active!", LogLevel.Warning);
                    break;
                case UseItemXpBoostResponse.Types.Result.ErrorLocationUnset:
                    Logger.Write($"Bot must be running first!", LogLevel.Error);
                    break;
                case UseItemXpBoostResponse.Types.Result.Unset:
                    break;
                case UseItemXpBoostResponse.Types.Result.ErrorInvalidItemType:
                    break;
                case UseItemXpBoostResponse.Types.Result.ErrorNoItemsRemaining:
                    break;
                default:
                    Logger.Write($"Failed to use a Lucky Egg!", LogLevel.Error);
                    break;
            }
        }
    }
}