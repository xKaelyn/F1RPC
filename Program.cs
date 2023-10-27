using System.Diagnostics;
using F1RPC.Configuration;
using F1Sharp;
using F1Sharp.Packets;
using NetDiscordRpc;
using NetDiscordRpc.RPC;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace F1RPC
{
    public class F1RPC
    {
        public DiscordRPC discord { get; private set; }
        public static ConfigJson Config = new ConfigJson();
        public bool isF1Running = false;

        static void Main(string[] args)
        {
            var f1 = new F1RPC();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate, restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("logs/log.txt", outputTemplate: "{Timestamp:dd MMM yyyy - hh:mm:ss tt} [{Level:u3}] {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information)
                .MinimumLevel.Information()
                .CreateLogger();

            Log.Information("F1RPC | Version 1.0.0.0");
            Log.Information("Waiting for F1 to be detected..");

            // Check if F1 23 is running, if not, wait until it is - when it is, initialize the program and break loop.
            while (true)
            {
                f1.isF1Running = Process.GetProcessesByName("F1_23").Length > 0;
                if (f1.isF1Running)
                {
                    Log.Information("F1 detected. Initializing..");
                    f1.Initialize().GetAwaiter().GetResult();
                    break;
                }
            }
        }

        public async Task Initialize()
        {
            string json = await File.ReadAllTextAsync("assets/config/Configuration.json").ConfigureAwait(false);
            using (var fs = File.OpenRead("assets/config/Configuration.json")) Config = JsonConvert.DeserializeObject<ConfigJson>(json);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            Log.Information("Logger initialized.");
            Log.Information("If you have any problems, please raise a issue on GitHub and upload your log file in the logs folder.");

            if (configJson.AppId == "YOUR_APP_ID_HERE")
            {
                Log.Error("Please set your Discord App ID in assets/config/Configuration.json");
                return;
            }

            discord = new DiscordRPC(configJson.AppId);
            
            // Let's actually bring the Discord client online
            discord.Initialize();

            Log.Information("DiscordRPC initialized.");

            Log.Information("Program initialized. Setting up client..");
            TelemetryClient client = new TelemetryClient(20777);

            // Various variables to use
            int teamId = 0;
            string teamName = "";
            var track = "";
            var currentTrackId = "";
            var image = "";
            int lapNumber = 0;
            int totalLaps = 0;
            int formulaType = 0;
            string sessionType = "";
            int playerIndex = 0;
            int currentPosition = 0;
            int totalParticipants = 0;
            double raceCompletion = 0.0;
            int lobbyPlayerCount = 0;
            int finalPosition = 0;
            int finalGridPosition = 0;
            int finalPoints = 0;
            int finalResultStatus = 0;
            int weatherId = 0;
            string weatherConditions = "";

            // Event hookers (funny name eh?)
            client.OnLapDataReceive += (packet) => Client_OnLapDataReceive(packet, discord);
            client.OnSessionDataReceive += (packet) => Client_OnSessionDataReceive(packet, discord, teamName, image);
            client.OnParticipantsDataReceive += (packet) => Client_OnParticipantsDataReceive(packet, discord);
            client.OnLobbyInfoDataReceive += (packet) => Client_OnLobbyInfoDataReceive(packet, discord);
            client.OnFinalClassificationDataReceive += (packet) => Client_OnFinalClassificationDataReceiveAsync(packet, discord);

            // When first booting system, reset the status by showing a "in menu" presence
            resetStatus(client, discord);

            // Method for when receiving lap data - used for getting lap number
            void Client_OnLapDataReceive(LapDataPacket packet, DiscordRPC discord)
            {
                playerIndex = packet.header.playerCarIndex;
                lapNumber = packet.lapData[playerIndex].currentLapNum;
                currentPosition = packet.lapData[playerIndex].carPosition;
                
                //Percentage for race completion - treat lap 1 as 0% and last lap as 100%
                if (lapNumber == 1) { raceCompletion = 0.0; }
                else { raceCompletion = Math.Round((double)(lapNumber - 1) / (double)(totalLaps - 1) * 100, 2); }
            }

            // Method for when recieving lobby info
            // Lobby data is received twice a second, until the game begins and the lobby is destroyed.
            void Client_OnLobbyInfoDataReceive(LobbyInfoPacket packet, DiscordRPC discord)
            {
                lobbyPlayerCount = packet.numPlayers;

                // Variable to change "player" to "players" if more than 1 player is in the lobby - no need to while loop this as it's only modified each time the data is received
                var playerText = "";
                if (lobbyPlayerCount > 1)
                {
                    playerText = "players";
                }
                if (lobbyPlayerCount < 1)
                {
                    playerText = "player";
                }

                // Set presence to "Waiting in the lobby with x other players", -1 because the player itself is not counted
                discord.SetPresence(new RichPresence
                {
                    Details = "In the menus",
                    State = $"Waiting in the lobby with {lobbyPlayerCount - 1} other {playerText}",
                    Assets = new Assets
                    {
                        LargeImageKey = $"f1_23_logo",
                        LargeImageText = $"F1 23"
                    }
                });
            }

            // Method for when recieving final classification data - used for getting final position
            // Final Classification data is only received once once the final scoreboard is shown to the player.
            // Method is async to allow for a Task.Delay at the end of the method as we need to assume the user is in the menus - we don't have a way of knowing if they are or not.
            async Task Client_OnFinalClassificationDataReceiveAsync(FinalClassificationPacket packet, DiscordRPC discord)
            {
                finalPosition = packet.classificationData[playerIndex].position;
                finalGridPosition = packet.classificationData[playerIndex].gridPosition;
                finalPoints = packet.classificationData[playerIndex].points;
                finalResultStatus = (int)packet.classificationData[playerIndex].resultStatus;

                // To-Do: Add session best lap time to presence
                // If user finished a session, but it's not a race
                if (sessionType != "Race" || sessionType != "Race 2" || sessionType != "Race 3") 
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"Session Completed | Track: {track}",
                        State = $"Racing for {teamName}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }

                // If user finished the race
                if (finalResultStatus == 3)
                {
                    if (sessionType != "Race" || sessionType != "Race 2" || sessionType != "Race 3")
                    {
                        discord.SetPresence(new RichPresence
                        {
                            Details = $"Session Completed | Track: {track}",
                            State = $"Racing for {teamName}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            }
                        });
                    }
                    else
                    {
                        discord.SetPresence(new RichPresence
                        {
                            Details = $"Finished: P{finalPosition} / P{totalParticipants} | Started: P{finalGridPosition} | Track: {track}",
                            State = $"Racing for {teamName} | {finalPoints} points earned",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            }
                        });
                    }
                }

                // If user did not finish the race
                if (finalResultStatus == 4)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"DNF | Started: P{finalGridPosition} | Track: {track}",
                        State = $"Racing for {teamName}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }

                // If user was disqualified
                if (finalResultStatus == 5)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"Disqualified | Started: P{finalGridPosition} | Track: {track}",
                        State = $"Racing for {teamName}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }

                // If user was not classified
                if (finalResultStatus == 6)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"Not Classified | Track: {track}",
                        State = $"Racing for {teamName}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }
                
                // If user retired
                if (finalResultStatus == 7)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"Retired | Started: P{finalGridPosition} | Track: {track}",
                        State = $"Racing for {teamName}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }

                // Wait 15 seconds
                await Task.Delay(15000);
                // Assume the player is in the menus - no way of actually knowing if they are or not
                resetStatus(client, discord);
            }

            // Method for when recieving participants data - used for getting team name
            // Participant data is received once every 5 seconds.
            void Client_OnParticipantsDataReceive(ParticipantsPacket packet, DiscordRPC discord)
            {
                playerIndex = packet.header.playerCarIndex;
                totalParticipants = packet.numActiveCars;
                teamId = (int)packet.participants[playerIndex].teamId;

                teamName = GetTeamNameFromId(teamId);
            }

            // Method for getting team name from team id (as F1 uses integers)
            string GetTeamNameFromId(int teamId)
            {
                var teamlist = new List<dynamic>()
                {
                    new { GameId = 0, Name = "Mercedes-AMG Petronas F1 Team" },
                    new { GameId = 1, Name = "Scuderia Ferrari" },
                    new { GameId = 2, Name = "Oracle Red Bull Racing" },
                    new { GameId = 3, Name = "Williams Racing" },
                    new { GameId = 4, Name = "Aston Martin Aramco Cognizant F1 Team" },
                    new { GameId = 5, Name = "BWT Alpine F1 Team" },
                    new { GameId = 6, Name = "Scuderia AlphaTauri" },
                    new { GameId = 7, Name = "Haas F1 Team" },
                    new { GameId = 8, Name = "McLaren F1 Team" },
                    new { GameId = 9, Name = "Alfa Romeo F1 Team ORLEN" },
                    new { GameId = 85, Name = "Mercedes (2020)" },
                    new { GameId = 86, Name = "Ferrari (2020)" },
                    new { GameId = 87, Name = "Red Bull (2020)" },
                    new { GameId = 88, Name = "Williams (2020)" },
                    new { GameId = 89, Name = "Racing Point (2020)" },
                    new { GameId = 90, Name = "Renault (2020)" },
                    new { GameId = 91, Name = "AlphaTauri (2020)" },
                    new { GameId = 92, Name = "Haas (2020)" },
                    new { GameId = 93, Name = "McLaren (2020)" },
                    new { GameId = 94, Name = "Alfa Romeo (2020)" },
                    new { GameId = 95, Name = "Aston Martin DB11 V12" },
                    new { GameId = 96, Name = "Aston Martin Vantage F1 Edition" },
                    new { GameId = 97, Name = "Aston Martin Vantage Safety Car" },
                    new { GameId = 98, Name = "Ferrari F8 Tributo" },
                    new { GameId = 99, Name = "Ferrari Roma" },
                    new { GameId = 100, Name = "McLaren 7205" },
                    new { GameId = 101, Name = "McLaren Artura" },
                    new { GameId = 102, Name = "Mercedes AMG GT Black Series Safety Car" },
                    new { GameId = 103, Name = "Mercedes AMG GTR Pro" },
                    new { GameId = 104, Name = "F1 Custom Team" },
                    new { GameId = 106, Name = "Prema (2021)" },
                    new { GameId = 107, Name = "Uni-Virtuosi (2021)" },
                    new { GameId = 108, Name = "Carlin (2021)" },
                    new { GameId = 109, Name = "Hitech (2021)" },
                    new { GameId = 110, Name = "Art GP (2021)" },
                    new { GameId = 111, Name = "MP Motorsport (2021)" },
                    new { GameId = 112, Name = "Charouz (2021)" },
                    new { GameId = 113, Name = "Dams (2021)" },
                    new { GameId = 114, Name = "Campos (2021)" },
                    new { GameId = 115, Name = "BWT (2021)" },
                    new { GameId = 116, Name = "Trident (2021)" },
                    new { GameId = 117, Name = "Mercedes AMG GT Black Series" },
                    new { GameId = 118, Name = "Mercedes (2022)" },
                    new { GameId = 119, Name = "Ferrari (2022)" },
                    new { GameId = 120, Name = "Red Bull Racing (2022)" },
                    new { GameId = 121, Name = "Williams (2022)" },
                    new { GameId = 122, Name = "Aston Martin (2022)" },
                    new { GameId = 123, Name = "Alpine (2022)" },
                    new { GameId = 124, Name = "AlphaTauri (2022)"},
                    new { GameId = 125, Name = "Haas (2022)"},
                    new { GameId = 126, Name = "McLaren (2022)"},
                    new { GameId = 127, Name = "Alfa Romeo (2022)"},
                    new { GameId = 128, Name = "Konnersport (2022)" },
                    new { GameId = 129, Name = "Konnersport" },
                    new { GameId = 130, Name = "Prema (2022)" },
                    new { GameId = 131, Name = "Uni-Virtuosi (2022)" },
                    new { GameId = 132, Name = "Carlin (2022)" },
                    new { GameId = 133, Name = "MP Motorsport (2022)" },
                    new { GameId = 134, Name = "Charouz (2022)" },
                    new { GameId = 135, Name = "Dams (2022)" },
                    new { GameId = 136, Name = "Campos (2022)" },
                    new { GameId = 137, Name = "Van Amersfoort Racing (2022)" },
                    new { GameId = 138, Name = "Trident (2022)" },
                    new { GameId = 139, Name = "Hitech (2022)" },
                    new { GameId = 140, Name = "Art GP (2022)" }
                };

                var team = teamlist.FirstOrDefault(t => t.GameId == teamId);
                if (team != null)
                {
                    return team.Name;
                }
                return "";
            }

            // Method for getting weather condition from weather id (as F1 uses integers)
            string GetWeatherConditions(int weatherId)
            {
                switch (weatherId)
                {
                    case 0:
                        weatherConditions = "Clear";
                        break;
                    case 1:
                        weatherConditions = "Light Cloud";
                        break;
                    case 2:
                        weatherConditions = "Overcast";
                        break;
                    case 3:
                        weatherConditions = "Light Rain";
                        break;
                    case 4:
                        weatherConditions = "Heavy Rain";
                        break;
                    case 5:
                        weatherConditions = "Storm";
                        break;
                }
                return weatherConditions;
            }

            // Method for when receiving session data
            // Session data is received twice a second until the session is destroyed. It only contains data about the ongoing session.
            async void Client_OnSessionDataReceive(SessionPacket packet, DiscordRPC discord, string teamName, string image)
            {
                formulaType = (int)packet.formula;
                totalLaps = packet.totalLaps;
                weatherId = (int)packet.weather;
                currentTrackId = packet.trackId.ToString().ToLower();

                weatherConditions = GetWeatherConditions(weatherId);

                // Case switch for checking track id, setting track name and setting track image
                switch ((int)packet.trackId)
                {
                    case 0:
                        track = "Australia: Melbourne";
                        break;
                    case 1:
                        track = "France: Le Castellet";
                        break;
                    case 2:
                        track = "China: Shanghai";
                        break;
                    case 3:
                        track = "Bahrain: Sakhir";
                        break;
                    case 4:
                        track = "Spain: Barcelona-Catalunya";
                        break;
                    case 5:
                        track = "Monaco";
                        break;
                    case 6:
                        track = "Canada: Montreal";
                        break;
                    case 7:
                        track = "UK: Silverstone";
                        break;
                    case 8:
                        track = "Germany: Hockenheim";
                        break;
                    case 9:
                        track = "Hungary: Budapest";
                        break;
                    case 10:
                        track = "Belgium: Spa-Francorchamps";
                        break;
                    case 11:
                        track = "Italy: Monza";
                        break;
                    case 12:
                        track = "Singapore";
                        break;
                    case 13:
                        track = "Japan: Suzuka";
                        break;
                    case 14:
                        track = "Abu Dhabi: Yas Marina";
                        break;
                    case 15:
                        track = "USA (Texas): COTA";
                        break;
                    case 16:
                        track = "Brazil: Sao Paolo";
                        break;
                    case 17:
                        track = "Austria: Spielberg";
                        break;
                    case 18:
                        track = "Russia: Sochi";
                        break;
                    case 19:
                        track = "Mexico";
                        break;
                    case 20:
                        track = "Azerbaijan: Baku";
                        break;
                    case 21:
                        track = "Bahrain: Sakhir (Short)";
                        break;
                    case 22:
                        track = "UK: Silverstone (Short)";
                        break;
                    case 23:
                        track = "USA (Texas): COTA (Short)";
                        break;
                    case 24:
                        track = "Japan: Suzuka (Short)";
                        break;
                    case 25:
                        track = "Vietnam: Hanoi";
                        break;
                    case 26:
                        track = "Netherlands: Zandvoort";
                        break;
                    case 27:
                        track = "Italy: Imola";
                        break;
                    case 28:
                        track = "Portugal: Portimao";
                        break;
                    case 29:
                        track = "Saudi Arabia: Jeddah";
                        break;
                    case 30:
                        track = "USA (Florida): Miami";
                        break;
                    case 31:
                        track = "USA (Nevada): Las Vegas";
                        break;
                    case 32:
                        track = "Qatar: Losail";
                        break;
                }

                // Case Switch for Packet Session Type
                switch ((int)packet.sessionType)
                {
                    case 0:
                        sessionType = "Unknown";
                        break;
                    case 1:
                        sessionType = "Practice 1";
                        break;
                    case 2:
                        sessionType = "Practice 2";
                        break;
                    case 3:
                        sessionType = "Practice 3";
                        break;
                    case 4:
                        sessionType = "Short Practice";
                        break;
                    case 5:
                        sessionType = "Qualifying 1";
                        break;
                    case 6:
                        sessionType = "Qualifying 2";
                        break;
                    case 7:
                        sessionType = "Qualifying 3";
                        break;
                    case 8:
                        sessionType = "Short Qualifying";
                        break;
                    case 9:
                        sessionType = "One-Shot Qualifying";
                        break;
                    case 10:
                        sessionType = "Race";
                        break;
                    case 11:
                        sessionType = "Race 2";
                        break;
                    case 12:
                        sessionType = "Race 3";
                        break;
                    case 13:
                        sessionType = "Time Trial";
                        break;
                }

                // Practice / Qualifying
                if ((int)packet.sessionType >= 1 && (int)packet.sessionType <= 9)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"{sessionType} - {track}",
                        State = $"Racing for {teamName} | Conditions: {weatherConditions}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }

                // Race
                if ((int)packet.sessionType >= 10 && (int)packet.sessionType <= 12)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"{sessionType} - {track} | Lap {lapNumber} / {totalLaps} | {raceCompletion}% complete",
                        State = $"Racing for {teamName} | P{currentPosition} / P{totalParticipants} | Conditions: {weatherConditions}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                    Log.Information($"assets/images/{packet.trackId.ToString().ToLower()}.png");
                }

                // Time Trial
                if ((int)packet.sessionType == 13)
                {
                    discord.SetPresence(new RichPresence
                    {
                        Details = $"{sessionType} - {track}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"{currentTrackId}",
                            LargeImageText = $"{track}"
                        }
                    });
                }
            }

            // Method for resetting status - checks if F1 23 is open before executing SetPresence
            async void resetStatus(TelemetryClient client, DiscordRPC discord)
            {
                Log.Information("F1 23 detected. Connecting..");

                while (true) { if (isF1Running) { break; } }

                while (isF1Running)
                {
                    Log.Information($"Connected. Updating status..");
                    discord.SetPresence(new RichPresence
                    {
                        Details = "Idle",
                        Assets = new Assets
                        {
                            LargeImageKey = "f1_23_logo",
                            LargeImageText = "F1 23"
                        },
                        Timestamps = new Timestamps(DateTime.UtcNow)
                    });
                    Log.Information($"Updated Discord Status: {discord.CurrentPresence.Details}");
                    break;
                }
            }
            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}