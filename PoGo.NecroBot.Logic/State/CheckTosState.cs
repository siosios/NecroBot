﻿#region using directives

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Protobuf.Collections;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Forms;
using PoGo.NecroBot.Logic.Utils;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using POGOProtos.Data.Player;
using System;
using System.IO;

#endregion

namespace PoGo.NecroBot.Logic.State
{
    public class CheckTosState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Always get a fresh PlayerData when checking tutorial state.
            var tutState = (await session.Client.Player.GetPlayer().ConfigureAwait(false)).PlayerData.TutorialState;

            if (tutState.Contains(TutorialState.FirstTimeExperienceComplete))
            {
                // If we somehow marked the tutorial as complete but have not yet created an avatar,
                // then create it.
                if (!tutState.Contains(TutorialState.AvatarSelection))
                {
                    var avatarRes = await session.Client.Player.SetAvatar(new PlayerAvatar()
                    {
                        Backpack = 0,
                        Eyes = 0,
                        Avatar = 0,
                        Hair = 0,
                        Hat = 0,
                        Pants = 0,
                        Shirt = 0,
                        Shoes = 0,
                        Skin = 0
                    }).ConfigureAwait(false);

                    EncounterTutorialCompleteResponse res = await
                    session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                    {
                        TutorialState.AvatarSelection
                    }).ConfigureAwait(false);
                }

                if (!tutState.Contains(TutorialState.PokemonBerry))
                {
                    await
                        session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.PokemonBerry
                        }).ConfigureAwait(false);
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = "Finish extra tutorial : Berry tutorial"
                    });
                    await DelayingUtils.DelayAsync(3000, 2000, cancellationToken).ConfigureAwait(false);
                }

                return new InfoState();
            }

            if (!tutState.Contains(TutorialState.LegalScreen))
            {
                EncounterTutorialCompleteResponse res = await
                    session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                    {
                        TutorialState.LegalScreen
                    }).ConfigureAwait(false);

                if (res.Result == EncounterTutorialCompleteResponse.Types.Result.Success)
                {
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = "Just read the Niantic ToS, looks legit, accepting!"
                    });
                    await DelayingUtils.DelayAsync(5000, 2000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = "Error reading the Niantic ToS!"
                    });
                }
            }

            if (session.LogicSettings.SkipFirstTimeTutorial)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = "Skipping the first time tutorial."
                });

                return new InfoState();
            }

            if (!session.LogicSettings.AutoFinishTutorial)
            {
                InitialTutorialForm form = new InitialTutorialForm(this, tutState, session);

                if (form.ShowDialog() == DialogResult.OK)
                {
                }
                else
                {
                    return new CheckTosState();
                }

            }
            else
            {
                if (!tutState.Contains(TutorialState.AvatarSelection))
                {
                    //string genderString = GlobalSettings.PromptForString(session.Translation, session.Translation.GetTranslation(TranslationString.FirstStartSetupAutoCompleteTutGenderPrompt), new string[] { "Male", "Female" }, "You didn't set a valid gender.", false);

                    //Gender gen;
                    //switch (genderString)
                    //{
                    //    case "Male":
                    //    case "male":
                    //        gen = Gender.Male;
                    //        break;
                    //    case "Female":
                    //    case "female":
                    //        gen = Gender.Female;
                    //        break;
                    //    default:
                    //        // We should never get here, since the prompt should only allow valid options.
                    //        gen = Gender.Male;
                    //        break;
                    //}


                    var avatarRes = await session.Client.Player.SetAvatar(new PlayerAvatar()
                    {
                        Backpack = 0,
                        Eyes = 0,
                        Hair = 0,
                        Hat = 0,
                        Pants = 0,
                        Shirt = 0,
                        Shoes = 0,
                        Skin = 0   ,
                        Avatar =1
                    }).ConfigureAwait(false);
                    if (avatarRes.Status == SetAvatarResponse.Types.Status.AvatarAlreadySet ||
                        avatarRes.Status == SetAvatarResponse.Types.Status.Success)
                    {
                        await session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.AvatarSelection
                        }).ConfigureAwait(false);
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = $"Selected your avatar, now you are Male!"
                        });
                    }
                }
                if (!tutState.Contains(TutorialState.PokemonCapture))
                {
                    await CatchFirstPokemon(session, cancellationToken).ConfigureAwait(false);
                }
                if (!tutState.Contains(TutorialState.NameSelection))
                {
                    await SelectNickname(session, cancellationToken).ConfigureAwait(false);
                }
                if (!tutState.Contains(TutorialState.FirstTimeExperienceComplete))
                {
                    await
                        session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                                TutorialState.FirstTimeExperienceComplete 
                        }).ConfigureAwait(false);
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = "First time experience complete, looks like i just spinned an virtual pokestop :P"
                    });
                    await DelayingUtils.DelayAsync(3000, 2000, cancellationToken).ConfigureAwait(false);
                }

                if (!tutState.Contains(TutorialState.PokemonBerry))
                {
                    await
                        session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.PokemonBerry
                        }).ConfigureAwait(false);
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = "Finish berry tutorial..."
                    });
                    await DelayingUtils.DelayAsync(3000, 2000, cancellationToken).ConfigureAwait(false);
                }

            }
            return new InfoState();
        }

        public async Task<bool> CatchFirstPokemon(ISession session, CancellationToken cancellationToken)
        {
            var firstPokeList = new List<PokemonId>
            {
                PokemonId.Bulbasaur,
                PokemonId.Charmander,
                PokemonId.Squirtle
            };
            //string pokemonString = GlobalSettings.PromptForString(session.Translation,
            //    session.Translation.GetTranslation(TranslationString.FirstStartSetupAutoCompleteTutStarterPrompt),
            //    new string[] { "Bulbasaur", "Charmander", "Squirtle" }, "You didn't enter a valid pokemon.", false);
            //var firstpokenum = 0;
            //switch (pokemonString)
            //{
            //    case "Bulbasaur":
            //    case "bulbasaur":
            //        firstpokenum = 0;
            //        break;
            //    case "Charmander":
            //    case "charmander":
            //        firstpokenum = 1;
            //        break;
            //    case "Squirtle":
            //    case "squirtle":
            //        firstpokenum = 2;
            //        break;
            //    default:
            //        // We should never get here.
            //        firstpokenum = 0;
            //        break;
            //}

            var firstPoke = firstPokeList[(new Random()).Next(0,2)];

            var res = await session.Client.Encounter.EncounterTutorialComplete(firstPoke).ConfigureAwait(false);
            await DelayingUtils.DelayAsync(7000, 2000, cancellationToken).ConfigureAwait(false);
            if (res.Result != EncounterTutorialCompleteResponse.Types.Result.Success) return false;
            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = $"Caught Tutorial pokemon! it's {firstPoke}!"
            });
            return true;
        }

        public async Task<bool> SelectNickname(ISession session, CancellationToken cancellationToken)
        {
            var nickname = !session.Settings.Username.Contains("@") ? session.Settings.Username : session.Settings.Username.Split(new char[] { '@' })[0];

            while (true)
            {
                //string nickname = GlobalSettings.PromptForString(session.Translation,
                //    session.Translation.GetTranslation(TranslationString.FirstStartSetupAutoCompleteTutNicknamePrompt),
                //    null, "You entered an invalid nickname.");

                //if (nickname.Length > 15 || nickname.Length == 0)
                //{
                //    session.EventDispatcher.Send(new ErrorEvent()
                //    {
                //        Message = "Your desired nickname is too long (max length 15 characters)!"
                //    });
                //    continue;
                //}
               
                if (nickname.Length > 15) nickname = nickname.Substring(0, 15);

                var res = await session.Client.Misc.ClaimCodename(nickname).ConfigureAwait(false);

                bool markTutorialComplete = false;
                string errorText = null;
                string warningText = null;
                string infoText = null;
                bool nameIsvalid = true;
                switch (res.Status)
                {
                    case ClaimCodenameResponse.Types.Status.Unset:
                        errorText = "Unset, somehow";
                        break;
                    case ClaimCodenameResponse.Types.Status.Success:
                        infoText = $"Your name is now: {res.Codename}";
                        markTutorialComplete = true;
                        break;
                    case ClaimCodenameResponse.Types.Status.CodenameNotAvailable:
                        nameIsvalid = false;
                        errorText = $"That nickname ({nickname}) isn't available, pick another one!";
                        break;
                    case ClaimCodenameResponse.Types.Status.CodenameNotValid:
                        nameIsvalid = false;
                        errorText = $"That nickname ({nickname}) isn't valid, pick another one!";
                        break;
                    case ClaimCodenameResponse.Types.Status.CurrentOwner:
                        nameIsvalid = false;
                        warningText = $"You already own that nickname!";
                        markTutorialComplete = true;
                        break;
                    case ClaimCodenameResponse.Types.Status.CodenameChangeNotAllowed:
                        warningText = "You can't change your nickname anymore!";
                        markTutorialComplete = true;
                        break;
                    default:
                        errorText = "Unknown Niantic error while changing nickname.";
                        break;
                }

                if (!string.IsNullOrEmpty(infoText))
                {
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = infoText
                    });
                }
                else if (!string.IsNullOrEmpty(warningText))
                {
                    session.EventDispatcher.Send(new WarnEvent()
                    {
                        Message = warningText
                    });
                }
                else if (!string.IsNullOrEmpty(errorText))
                {
                    session.EventDispatcher.Send(new ErrorEvent()
                    {
                        Message = errorText
                    });
                    if(!nameIsvalid)
                    {
                        nickname = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        continue;
                    }
                    
                }

                if (markTutorialComplete)
                {
                    await session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                    {
                        TutorialState.NameSelection
                    }).ConfigureAwait(false);

                    await DelayingUtils.DelayAsync(3000, 2000, cancellationToken).ConfigureAwait(false);
                    return res.Status == ClaimCodenameResponse.Types.Status.Success;
                }
            }
        }
    }
}