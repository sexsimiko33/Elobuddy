
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;

namespace SexsiTwitchAddon
{
    class Program
    {
        static void Main()
        {
            Loading.OnLoadingComplete += delegate
            {
                if (!File.Exists(DllPath))
                {
                    Download("https://github.com/Sexsimiko7/Elobuddy/raw/master/Confused/SexsiTwitch.dll", DllPath);
                    return;
                }
                File.Delete(DllPath);
                Download("https://github.com/Sexsimiko7/Elobuddy/raw/master/Confused/SexsiTwitch.dll", DllPath);
            };
        }

        private static void Download(string url, string storePath)
        {
            using (WebClient webClient = new WebClient())
            {
                Uri address = new Uri(url);
                webClient.DownloadFileAsync(address, storePath);
                webClient.DownloadFileCompleted += Client_DownloadFileCompleted;
            }
        }

        private static readonly string DllPath = Path.Combine(SandboxConfig.DataDirectory, "Addons", "Libraries", "SexsiTwitch.dll");

        private static void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Core.DelayAction(delegate
            {
                Console.WriteLine("SexsiTwitch download completed!");
                Assembly.LoadFrom(DllPath).GetType("Class11").GetMethod("smethod_0", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }, 2000);
        }
    }
}
