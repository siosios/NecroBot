﻿using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PoGo.NecroBot.Logic.Captcha.Anti_Captcha;
using PoGo.NecroBot.Logic.Forms;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using LogLevel = PoGo.NecroBot.Logic.Logging.LogLevel;

namespace PoGo.NecroBot.Logic.Captcha
{
    public class CaptchaManager
    {
        const string POKEMON_GO_GOOGLE_KEY = "6LeeTScTAAAAADqvhqVMhPpr_vB9D364Ia-1dSgK";

        public static async Task<bool> SolveCaptcha(ISession session, string captchaUrl)
        {
            string captchaResponse = "";
            var cfg = session.LogicSettings.CaptchaConfig;
            bool resolved = false;
            bool needGetNewCaptcha = false;
            int retry = cfg.AutoCaptchaRetries;

            while (retry-- > 0 && !resolved)
            {
                //Use captcha solution to resolve captcha
                if (cfg.EnableCaptchaSolutions)
                {
                    if (needGetNewCaptcha)
                    {
                        captchaUrl = await GetNewCaptchaURL(session).ConfigureAwait(false);
                    }

                    Logger.Write("Auto resolving captcha by using captcha solution service, please wait..........");
                    CaptchaSolutionClient client = new CaptchaSolutionClient(cfg.CaptchaSolutionAPIKey,
                        cfg.CaptchaSolutionsSecretKey, cfg.AutoCaptchaTimeout);
                    captchaResponse = await client.ResolveCaptcha(POKEMON_GO_GOOGLE_KEY, captchaUrl).ConfigureAwait(false);
                    needGetNewCaptcha = true;
                    if (!string.IsNullOrEmpty(captchaResponse))
                    {
                        resolved = await Resolve(session, captchaResponse).ConfigureAwait(false);
                    }
                }

              

                //use 2 captcha
                if (!resolved && session.LogicSettings.CaptchaConfig.Enable2Captcha &&
                    !string.IsNullOrEmpty(session.LogicSettings.CaptchaConfig.TwoCaptchaAPIKey))
                {
                    if (needGetNewCaptcha)
                    {
                        captchaUrl = await GetNewCaptchaURL(session).ConfigureAwait(false);
                    }
                    if (string.IsNullOrEmpty(captchaUrl)) return true;

                    Logger.Write("Auto resolving captcha by using 2Captcha service");
                    captchaResponse = await GetCaptchaResposeBy2Captcha(session, captchaUrl).ConfigureAwait(false);
                    needGetNewCaptcha = true;
                    if (!string.IsNullOrEmpty(captchaResponse))
                    {
                        resolved = await Resolve(session, captchaResponse).ConfigureAwait(false);
                    }
                }

                if (!resolved && session.LogicSettings.CaptchaConfig.EnableAntiCaptcha && !string.IsNullOrEmpty(session.LogicSettings.CaptchaConfig.AntiCaptchaAPIKey))
                {
                    if (needGetNewCaptcha)
                    {
                        captchaUrl = await GetNewCaptchaURL(session).ConfigureAwait(false);
                    }
                    if (string.IsNullOrEmpty(captchaUrl)) return true;

                    Logger.Write("Auto resolving captcha by using anti captcha service");
                    captchaResponse = await GetCaptchaResposeByAntiCaptcha(session, captchaUrl).ConfigureAwait(false);
                    needGetNewCaptcha = true;
                    if (!string.IsNullOrEmpty(captchaResponse))
                    {
                        resolved = await Resolve(session, captchaResponse).ConfigureAwait(false);
                    }

                }

            }

            //captchaRespose = "";
            if (!resolved)
            {
                if (needGetNewCaptcha)
                {
                    captchaUrl = await GetNewCaptchaURL(session).ConfigureAwait(false);
                }

                if (session.LogicSettings.CaptchaConfig.PlaySoundOnCaptcha)
                {
                    SystemSounds.Asterisk.Play();
                }

                captchaResponse = GetCaptchaResposeManually(session, captchaUrl);
                //captchaResponse = await GetCaptchaTokenWithInternalForm(captchaUrl).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(captchaResponse))
                {
                    resolved = await Resolve(session, captchaResponse).ConfigureAwait(false);
                }
            }

            return resolved;
        }


        //NOT WORKING  SINCE WEB BROWSER CONTROL IS IE7, doesn't work with captcha page

        private static async Task<string> GetCaptchaTokenWithInternalForm(string captchaUrl)
        {
            string response = "";
            var t = new Thread(() =>
            {
                CaptchaSolveForm captcha = new CaptchaSolveForm(captchaUrl);
                if (captcha.ShowDialog() == DialogResult.OK)
                {
                    response = "Aaa";
                }

                //captcha.TopMost = true;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            int count = 120;
            while (true && count > 0)
            {
                count--;
                //Thread.Sleep(1000);
                await Task.Delay(1000).ConfigureAwait(false);
            }

            return response;
        }

        private static async Task<string> GetNewCaptchaURL(ISession session)
        {
            var res = await session.Client.Player.CheckChallenge().ConfigureAwait(false);
            if (res.ShowChallenge)
            {
                return res.ChallengeUrl;
            }
            return string.Empty;
        }

        private static async Task<bool> Resolve(ISession session, string captchaRespose)
        {
            if (string.IsNullOrEmpty(captchaRespose)) return false;
            try
            {
                var verifyChallengeResponse = await session.Client.Player.VerifyChallenge(captchaRespose).ConfigureAwait(false);
                if (!verifyChallengeResponse.Success)
                {
                    Logger.Write($"(CAPTCHA) Failed to resolve captcha, try resolved captcha by official app. ");
                    return false;
                }
                Logger.Write($"(CAPTCHA) Great!!! Captcha has been by passed", color: ConsoleColor.Green);
                return verifyChallengeResponse.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        private static async Task<string> GetCaptchaResposeByAntiCaptcha(ISession session, string captchaUrl)
        {
            bool solved = false;
            string result = null;

            {
                result = await AntiCaptchaClient.SolveCaptcha(captchaUrl,
                    session.LogicSettings.CaptchaConfig.AntiCaptchaAPIKey,
                    POKEMON_GO_GOOGLE_KEY,
                    session.LogicSettings.CaptchaConfig.ProxyHost,
                    session.LogicSettings.CaptchaConfig.ProxyPort).ConfigureAwait(false);
                solved = !string.IsNullOrEmpty(result);
            }
            if (solved)
            {
                Logger.Write("Captcha has been resolved automatically by Anti-Captcha ");
            }
            return result;
        }


        private static async Task<string> GetCaptchaResposeBy2Captcha(ISession session, string captchaUrl)
        {
            bool solved = false;
            int retries = session.LogicSettings.CaptchaConfig.AutoCaptchaRetries;
            string result = null;

            while (retries-- > 0 && !solved)
            {
                TwoCaptchaClient client = new TwoCaptchaClient(session.LogicSettings.CaptchaConfig.TwoCaptchaAPIKey);

                result = await client.SolveRecaptchaV2(POKEMON_GO_GOOGLE_KEY, captchaUrl, string.Empty, ProxyType.HTTP).ConfigureAwait(false);
                solved = !string.IsNullOrEmpty(result);
            }
            if (solved)
            {
                Logger.Write("Captcha has been resolved automatically by 2Captcha ");
            }
            return result;
        }

        public static string GetCaptchaResposeManually(ISession session, string url)
        {
            if (!session.LogicSettings.CaptchaConfig.AllowManualCaptchaResolve) return null;

            if (!File.Exists("chromedriver.exe"))
            {
                Logger.Write(
                    $"You enable manual captcha resolve but bot didn't setup files properly, please download chromedriver.exe put in same folder.",
                    LogLevel.Error
                );
                return null;
            }
            IWebDriver webDriver = null;
            try
            {
                webDriver = new ChromeDriver(Environment.CurrentDirectory, new ChromeOptions()
                {
                });

                webDriver.Navigate().GoToUrl(url);
                Logger.Write($"Captcha is being show in separate thread window, please check your chrome browser and resolve it before {session.LogicSettings.CaptchaConfig.ManualCaptchaTimeout} seconds");

                var wait = new WebDriverWait(
                    webDriver,
                    TimeSpan.FromSeconds(session.LogicSettings.CaptchaConfig.ManualCaptchaTimeout)
                );
                //wait.Until(ExpectedConditions.ElementIsVisible(By.Id("recaptcha-verify-button")));
                wait.Until(d =>
                {
                    var ele = d.FindElement(By.Id("g-recaptcha-response"));

                    if (ele == null) return false;
                    return ele.GetAttribute("value").Length > 0;
                });

                string token = wait.Until<string>(driver =>
                {
                    var ele = driver.FindElement(By.Id("g-recaptcha-response"));
                    string t = ele.GetAttribute("value");
                    return t;
                });

                return token;
            }
            catch (Exception ex)
            {
                Logger.Write($"You didn't resolve captcha in the given time: {ex.Message} ", LogLevel.Error);
            }
            finally
            {
                if (webDriver != null) webDriver.Close();
            }

            return null;
        }
    }
}