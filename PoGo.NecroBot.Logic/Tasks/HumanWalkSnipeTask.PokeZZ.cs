﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.Utils;
using WebSocket4Net;

namespace PoGo.NecroBot.Logic.Tasks
{
    public partial class HumanWalkSnipeTask
    {
        private static async Task<List<SnipePokemonInfo>> FetchFromPokeZZ(double lat, double lng)
        {
            List<SnipePokemonInfo> results = new List<SnipePokemonInfo>();
            if (!_setting.HumanWalkingSnipeUsePokeZZ) return results;

            //var startFetchTime = DateTime.Now;

            string url = "ws://pokezz.com/socket.io/?EIO=3&transport=websocket";
            try
            {
                using (var client = new WebSocket(url, "basic", WebSocketVersion.Rfc6455))
                {
                    client.MessageReceived += (s, e) =>
                    {
                        var message = e.Message;
                        var match = Regex.Match(message, @"^(1?\d+)\[""[a|b]"",""(2?.*)""\]$");
                        if (match.Success)
                        {
                            if (match.Groups[1].Value == "42")
                            {
                                var sniperInfos = Parse(match.Groups[2].Value);
                                if (sniperInfos != null && sniperInfos.Any())
                                {
                                    results.AddRange(sniperInfos
                                        .Where(p => LocationUtils.CalculateDistanceInMeters(lat, lng, p.Latitude, p.Longitude) < 5000)
                                        .ToList());
                                }
                            }
                        }
                    };
                    client.Open();
                    await Task.Delay(1000).ConfigureAwait(false);
                    client.Close();
                }
            }
            catch (Exception)
            {
                Logger.Write("Error loading data from Pokezz", LogLevel.Error, ConsoleColor.DarkRed);
            }

            //var endFetchTime = DateTime.Now;
            //Logger.Write($"FetchFromPokeZZ spent {(endFetchTime - startFetchTime).TotalSeconds} seconds", LogLevel.Info, ConsoleColor.White);
            return results;
        }

        private static List<SnipePokemonInfo> Parse(string reader)
        {
            var lines = reader.Split('~');
            var list = new List<SnipePokemonInfo>();

            foreach (var line in lines)
            {
                var sniperInfo = ParseLine(line);
                if (sniperInfo != null)
                {
                    list.Add(sniperInfo);
                }
            }
            return list;
        }

        private static SnipePokemonInfo ParseLine(string line)
        {
            var match = Regex.Match(line,
                @"(?<id>\d+)\|(?<lat>\-?\d+[\,|\.]\d+)\|(?<lon>\-?\d+[\,|\.]\d+)\|(?<expires>\d+)\|(?<verified>[1|0])\|\|");
            if (match.Success)
            {
                var pokemonId = Convert.ToInt32(match.Groups["id"].Value);
                var sniperInfo = new SnipePokemonInfo()
                {
                    Id = pokemonId,
                    Source = "PokeZZ"
                };

                var lat = Convert.ToDouble(match.Groups["lat"].Value);
                var lon = Convert.ToDouble(match.Groups["lon"].Value);

                sniperInfo.Latitude = Math.Round(lat, 7);
                sniperInfo.Longitude = Math.Round(lon, 7);

                var expires = Convert.ToInt64(match.Groups["expires"].Value);
                if (expires != default(long))
                {
                    sniperInfo.ExpiredTime = UnixTimeStampToDateTime(expires);
                }
                return sniperInfo;
            }
            return null;
        }
    }
}