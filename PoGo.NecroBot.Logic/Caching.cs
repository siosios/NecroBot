﻿using PoGo.NecroBot.Logic.State;
using System;
using System.Runtime.Caching;
using TinyIoC;

namespace PoGo.NecroBot.Logic
{
    public class Caching
    {
        private const int CACH_DURATION = 15; //minutes
        private static MemoryCache encounteredPokemons = new MemoryCache("encounteredPokemons");

        public static void AddEncounteredPokemon(ulong encounterId)
        {
            ISession session = TinyIoCContainer.Current.Resolve<ISession>();
            string uniqueKey = $"{session.Settings.Username}-{encounterId}";
            encounteredPokemons.Add(uniqueKey, encounterId, DateTime.Now.AddMinutes(CACH_DURATION));

        }
    }
}
