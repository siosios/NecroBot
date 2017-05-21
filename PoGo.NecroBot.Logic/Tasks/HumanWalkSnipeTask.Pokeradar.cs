﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.NecroBot.Logic.Logging;

namespace PoGo.NecroBot.Logic.Tasks
{
    public partial class HumanWalkSnipeTask
    {
        public class PokeradarWrapper
        {
            public class PokeradarItem
            {
                public double created { get; set; }
                public double latitude { get; set; }
                public double longitude { get; set; }
                public int pokemonId { get; set; }
            }

            //should refactore this model - SnipeInfo
            public List<PokeradarItem> data { get; set; }
        }

        private static async Task<List<SnipePokemonInfo>> FetchFromPokeradar(double lat, double lng)
        {
            List<SnipePokemonInfo> results = new List<SnipePokemonInfo>();
            if (!_setting.HumanWalkingSnipeUsePokeRadar) return results;

            //var startFetchTime = DateTime.Now;

            try
            {
                HttpClient client = new HttpClient();
                double offset = _setting.HumanWalkingSnipeSnipingScanOffset; //0.015 
                string url = $"https://www.pokeradar.io/api/v1/submissions?deviceId=1fd29370661111e6b850a13a2bdc4ebf&minLatitude={lat - offset}&maxLatitude={lat + offset}&minLongitude={lng - offset}&maxLongitude={lng + offset}&pokemonId=0";

                var task = await client.GetStringAsync(url).ConfigureAwait(false);

                var data = JsonConvert.DeserializeObject<PokeradarWrapper>(task);
                results = data.data.Select(p => Map(p)).ToList();
            }
            catch (Exception)
            {
                Logger.Write("Error loading data from pokeradar", LogLevel.Error, ConsoleColor.DarkRed);
            }

            //var endFetchTime = DateTime.Now;
            //Logger.Write($"FetchFromPokeradar spent {(endFetchTime - startFetchTime).TotalSeconds} seconds", LogLevel.Info, ConsoleColor.White);
            return results;
        }

        private static SnipePokemonInfo Map(PokeradarWrapper.PokeradarItem item)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(item.created).ToLocalTime();
            var expiredTime = dtDateTime.AddMinutes(15);

            return new SnipePokemonInfo()
            {
                Latitude = item.latitude,
                Longitude = item.longitude,
                Id = item.pokemonId,
                ExpiredTime = expiredTime,
                Source = "Pokeradar"
            };
        }
    }
}