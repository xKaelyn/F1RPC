using System;
using System.ComponentModel.DataAnnotations;
using F1Sharp;
using F1Sharp.Packets;
using NetDiscordRpc;
using NetDiscordRpc.RPC;

namespace F1_23_Discord_RPC
{
    public class Program
    {
        static void Main(string[] args)
        {
            var discord = new DiscordRPC("1166791756554178671");
            var client = new TelemetryClient(20777);

            discord.Initialize();

            client.OnLapDataReceive += Client_OnLapDataReceive;
            resetStatus(client, discord);

            while (true)
            {

            }
        }

        private static void Client_OnLapDataReceive(LapDataPacket packet)
        {

        }

        private static void resetStatus(TelemetryClient client, DiscordRPC discord)
        {
            discord.SetPresence(new RichPresence
            {
                State = "In the main menu",
                Assets = new Assets
                {
                    LargeImageKey = "f1_23_logo",
                    LargeImageText = "F1 23"
                },
                Timestamps = new Timestamps(DateTime.UtcNow)
            });
            Console.WriteLine($"Updated Discord Status: {discord.CurrentPresence.State}");
        }
    }
}