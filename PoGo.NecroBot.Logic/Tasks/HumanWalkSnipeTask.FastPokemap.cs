﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Enums;

namespace PoGo.NecroBot.Logic.Tasks
{
    public partial class HumanWalkSnipeTask
    {
        private static string ip;

        private static Task taskDataLive;

        public class FastPokemapItem
        {
            public class Lnglat
            {
                public string type { get; set; }
                public List<double> coordinates { get; set; }
            }

            public string _id { get; set; }
            public string pokemon_id { get; set; }
            public string encounter_id { get; set; }
            public string spawn_id { get; set; }
            public DateTime expireAt { get; set; }
            public int __v { get; set; }
            public Lnglat lnglat { get; set; }
        }

        private static string GetIP()
        {
            string p = @"\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}";

            if (string.IsNullOrEmpty(ip))
            {
                var client = new HttpClient();
                var task = client.GetStringAsync("http://checkip.dyndns.org/");
                task.Wait();
                ip = Regex.Match(task.Result, p).Value;
            }
            return ip;
        }

        public static async Task StartFastPokemapAsync(ISession session, CancellationToken cancellationToken)
        {
            await Task.Delay(0).ConfigureAwait(false); // Just added to get rid of compiler warning. Remove this if async code is used below.

            return;
            /*
            double defaultLatitude = session.Settings.DefaultLatitude;
            double defaultLongitude = session.Settings.DefaultLongitude;

            while (true)
            {
                await Task.Delay(30 * 1000).ConfigureAwait(false);//sleep for 30 sec

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var scanOffset = session.LogicSettings.HumanWalkingSnipeSnipingScanOffset;
                //scanOffset = 0.065;
                var step = scanOffset / 5;

                //Logger.Write($"Execute map scan data Offset: {scanOffset} :))");
                double lat = session.Client.CurrentLatitude;

                double lng = session.Client.CurrentLongitude;
                await OffsetScanFPM(scanOffset, step, lat, lng).ConfigureAwait(false);
                if(LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude, session.Client.CurrentLongitude, defaultLatitude, defaultLongitude) >1000)
                {
                    await OffsetScanFPM(scanOffset, step, defaultLatitude, defaultLongitude).ConfigureAwait(false);
                }
                sw.Stop();

                //Logger.Write($"Map scan finished! Time elapsed {sw.Elapsed.ToString(@"mm\:ss")}");
            }
            */
        }

        private static async Task OffsetScanFPM(double scanOffset, double step, double lat, double lng)
        {
            for (var x = -scanOffset; x <= scanOffset;)
            {
                for (var y = -scanOffset; y <= scanOffset;)
                {
                    try
                    {
                        var scanLat = lat + x;
                        var scanLng = lng + y;
                        string scanurl = $"https://cache.fastpokemap.se/?key=2fe7ce70-90b8-460a-bffb-d7f3b4b74cc2&ts=57c9d27c&compute={GetIP()}&lat={scanLat}&lng={scanLng}";

                        var json = await DownloadContent(scanurl).ConfigureAwait(false);
                        var data = JsonConvert.DeserializeObject<List<FastPokemapItem>>(json);
                        List<SnipePokemonInfo> chunk = new List<SnipePokemonInfo>();
                        foreach (var item in data)
                        {
                            var pItem = Map(item);
                            if (pItem != null && pItem.Id > 0)
                            {
                                chunk.Add(pItem);
                            }
                        }
                        // TODO - await is legal here! USE it or use pragma to suppress compilerwarning and write a comment why it is not used
                        // TODO: Attention - do not touch (add pragma) when you do not know what you are doing ;)
                        // jjskuld - Ignore CS4014 warning for now.
                        #pragma warning disable 4014
                        PostProcessDataFetched(chunk, false);
                        #pragma warning restore 4014
                    }
                    catch
                    {
                        // TODO Bad practice! Wanna log this?
                    }
                    finally
                    {
                        y += step;
                    }
                }
                x += step;
            }
        }

        private static void StartAsyncPollingTask(ISession session, CancellationToken cancellationToken)
        {
            return;

            // TODO unreachable
            // jjskuld - Ignore CS0162 warning for now.
            #pragma warning disable 0162
            if (!session.LogicSettings.HumanWalkingSnipeUseFastPokemap) return;

            if (taskDataLive != null && !taskDataLive.IsCompleted) return;
            taskDataLive = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        string key = "d39dbc96-e963-4747-89f6-15b18441b6a5";
                        string ts = "6ff886bc";
                        //cancellationToken.ThrowIfCancellationRequested();
                        string content = "";
                        do
                        {
                            var lat = _session.Client.CurrentLatitude;
                            var lng = _session.Client.CurrentLongitude;
                            var api = $"https://api.fastpokemap.se/?key={key}&ts={ts}&lat={lat}&lng={lng}";
                            content = DownloadContent(api).Result;
                            Task.Delay(2000).Wait();
                        } while (content.Contains("error"));

                        Task.Delay(1 * 60 * 1000).Wait();
                    }
                    catch
                    {
                    }
                }
            });
            #pragma warning restore 0162
        }

        private static SnipePokemonInfo Map(FastPokemapItem result)
        {
            return new SnipePokemonInfo()
            {
                Latitude = result.lnglat.coordinates[1],
                Longitude = result.lnglat.coordinates[0],
                Id = GetId(result.pokemon_id),
                ExpiredTime = result.expireAt.ToLocalTime(),
                Source = "Fastpokemap"
            };
        }

        public static int GetId(string name)
        {
            var t = name[0];
            var realName = new StringBuilder(name.ToLower());
            realName[0] = t;
            try
            {
                var p = (PokemonId) Enum.Parse(typeof(PokemonId), realName.ToString());
                return (int) p;
            }

            catch (Exception)
            {
            }
            return 0;
        }

        private static async Task<string> DownloadContent(string url)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("origin", "https://fastpokemap.se");
            request.Headers.Add("authority", "cache.fastpokemap.se");

            string result = "";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var task = await client.SendAsync(request).ConfigureAwait(false);
                    result = await task.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        private static async Task<List<SnipePokemonInfo>> FetchFromFastPokemap(double lat, double lng)
        {
            return new List<SnipePokemonInfo>();

            // TODO unreachable
            // jjskuld - Ignore CS0162 warning for now.
            #pragma warning disable 0162
            List<SnipePokemonInfo> results = new List<SnipePokemonInfo>();
            if (!_setting.HumanWalkingSnipeUseFastPokemap) return results;

            //var startFetchTime = DateTime.Now;
            try
            {
                string key = "20d638c9-c5b3-40cf-bf8b-1c9c52cc2cc2"; //allow-all
                string ts = "18503bfe";

                string url = $"https://cache.fastpokemap.se/?key={key}&ts={ts}&lat={lat}&lng={lng}";

                var json = await DownloadContent(url).ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<List<FastPokemapItem>>(json);
                foreach (var item in data)
                {
                    var pItem = Map(item);
                    if (pItem != null && pItem.Id > 0)
                    {
                        results.Add(pItem);
                    }
                }
            }
            catch (Exception)
            {
                Logger.Write("Error loading data fastpokemap", LogLevel.Error, ConsoleColor.DarkRed);
            }

            //var endFetchTime = DateTime.Now;
            //Logger.Write($"FetchFromFastPokemap spent {(endFetchTime - startFetchTime).TotalSeconds} seconds", LogLevel.Info, ConsoleColor.White);
            return results;
            #pragma warning restore 0162
        }
    }
}