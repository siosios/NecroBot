﻿using System;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Model;

namespace PoGo.NecroBot.Logic.State
{
    public class BlockableScope : IDisposable
    {
        private ISession session;

        private BotActions action;

        public BlockableScope(ISession session, BotActions action)
        {
            this.session = session;
            this.action = action;
        }

        public async Task<bool> WaitToRun(int timeout = 60000)
        {
            return await session.WaitUntilActionAccept(action, timeout).ConfigureAwait(false);
        }

        public void Dispose()
        {
            session.Actions.RemoveAll(x => x == action);
        }
    }
}