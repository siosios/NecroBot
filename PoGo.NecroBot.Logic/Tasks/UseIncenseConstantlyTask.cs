﻿using System.Threading;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using TinyIoC;
using System;

namespace PoGo.NecroBot.Logic.Tasks
{
    public class UseIncenseConstantlyTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TinyIoC.TinyIoCContainer.Current.Resolve<MultiAccountManager>().ThrowIfSwitchAccountRequested();
            
            var currentAmountOfIncense = await session.Inventory.GetItemAmountByType(ItemId.ItemIncenseOrdinary).ConfigureAwait(false);
            if (currentAmountOfIncense == 0)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.NoIncenseAvailable));
                return;
            }
            else
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.UseIncenseAmount, currentAmountOfIncense));
            }

            var UseIncense = await session.Inventory.UseIncenseConstantly().ConfigureAwait(false);

            if (UseIncense.Result == UseIncenseResponse.Types.Result.Success)
            {
                var totalMS = UseIncense.AppliedIncense.ExpireMs - UseIncense.AppliedIncense.AppliedMs;

                TinyIoCContainer.Current.Resolve<MultiAccountManager>().DisableSwitchAccountUntil(DateTime.Now.AddMilliseconds(totalMS));
                Logger.Write(session.Translation.GetTranslation(TranslationString.UsedIncense));
            }
            else if (UseIncense.Result == UseIncenseResponse.Types.Result.NoneInInventory)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.NoIncenseAvailable));
            }
            else if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive || (UseIncense.AppliedIncense == null))
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.UseIncenseActive));
            }
        }
    }
}