﻿#region using directives

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Event.Inventory;
using PoGo.NecroBot.Logic.PoGoUtils;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Networking.Responses;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class FavoritePokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //await session.Inventory.RefreshCachedInventory().ConfigureAwait(false);

            if (!session.LogicSettings.AutoFavoritePokemon) return;

            var pokemons = (await session.Inventory.GetPokemons().ConfigureAwait(false)).Where(x=>x.Favorite ==0);

            foreach (var pokemon in pokemons)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TinyIoC.TinyIoCContainer.Current.Resolve<MultiAccountManager>().ThrowIfSwitchAccountRequested();
                var perfection = Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon));

                if (session.LogicSettings.FavoriteOperator.BoolFunc(
                    perfection >= session.LogicSettings.FavoriteMinIvPercentage , 
                    pokemon.Cp >= session.LogicSettings.FavoriteMinCp,
                    PokemonInfo.GetLevel(pokemon) >= session.LogicSettings.FavoriteMinLevel))
                {
                    var response = await session.Client.Inventory.SetFavoritePokemon(pokemon.Id, true).ConfigureAwait(false);
                    if (response.Result == SetFavoritePokemonResponse.Types.Result.Success)
                    {
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonFavorite, perfection,
                                    session.Translation.GetPokemonTranslation(pokemon.PokemonId), pokemon.Cp)
                        });
                    }
                }
            }
        }

        public static async Task Execute(ISession session, ulong pokemonId, bool favorite)
        {
            //using (var blocker = new BlockableScope(session, BotActions.Favorite))
            //{
                //if (!await blocker.WaitToRun().ConfigureAwait(false)) return;
                
                var pokemon = (await session.Inventory.GetPokemons().ConfigureAwait(false)).FirstOrDefault(p => p.Id == pokemonId);
                if (pokemon != null)
                {
                    var perfection = Math.Round(PokemonInfo.CalculatePokemonPerfection(pokemon));

                    var response = await session.Client.Inventory.SetFavoritePokemon(pokemonId, favorite).ConfigureAwait(false);
                    if (response.Result == SetFavoritePokemonResponse.Types.Result.Success)
                    {
                        // Reload pokemon to refresh favorite flag.
                        pokemon = (await session.Inventory.GetPokemons().ConfigureAwait(false)).FirstOrDefault(p => p.Id == pokemonId);

                        session.EventDispatcher.Send(new FavoriteEvent(pokemon, response));

                        string message;
                        if (favorite)
                            message = session.Translation.GetTranslation(TranslationString.PokemonFavorite, perfection,
                                session.Translation.GetPokemonTranslation(pokemon.PokemonId), pokemon.Cp);
                        else
                            message = session.Translation.GetTranslation(TranslationString.PokemonUnFavorite, perfection,
                                session.Translation.GetPokemonTranslation(pokemon.PokemonId), pokemon.Cp);
                        
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message = message
                        });
                    }
                }
            //}
        }
    }
}