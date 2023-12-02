using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using CSharpDiscordWebhook.NET.Discord;
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
        public DiscordRPC? discord { get; private set; }
        public DiscordWebhook? webhook { get; private set; } = new DiscordWebhook();
        public static ConfigJson Config { get; private set; } = new ConfigJson();
        public string? projectDirectory { get; set; }
        public bool? isRunningOnMacOS { get; set; }
        public Version? versionId { get; set; } = Assembly.GetExecutingAssembly().GetName().Version;

        static void Main(string[] args)
        {
            F1RPC f1 = new() { isRunningOnMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) };

            f1.projectDirectory =
                f1.isRunningOnMacOS == true
                    ? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    : Directory.GetCurrentDirectory();

            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .Console(
                    theme: SystemConsoleTheme.Literate,
                    restrictedToMinimumLevel: LogEventLevel.Information
                )
                .WriteTo
                .File(
                    $"{f1.projectDirectory}/logs/F1RPC.log",
                    outputTemplate: "{Timestamp:dd MMM yyyy - hh:mm:ss tt} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information
                )
                .MinimumLevel
                .Information()
                .CreateLogger();

            Log.Information($"F1RPC | Version {f1.versionId}");
            Log.Information("Program booting..");

            try
            {
                f1.Initialize().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                if (ex.Message == "Address already in use")
                {
                    Log.Fatal(
                        $"Port {Config.Port} is already in use. Please either close the program using it and try again or choose another port in the Configuration.json file."
                    );
                }
                else
                {
                    Log.Fatal($"Exception occured: {ex.Message}");
                }
            }
        }

        public async Task Initialize()
        {
            ConfigJson configJson = new();
            int port = 0;
            string json = "";
            bool webhookEnabled = false;

            // Check to see if it's running on MacOS as the path is handled differently.
            // For some reason, when running a MacOS program, the working directory is the root of the drive.
            // Not the program directory.
            if (isRunningOnMacOS == true)
            {
                string? projectDirectory = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location
                );

                json = await File.ReadAllTextAsync(
                        $"{projectDirectory}/assets/config/Configuration.json"
                    )
                    .ConfigureAwait(false);
                string configPath = string.Format(
                    $"{projectDirectory}/assets/config/Configuration.json"
                );

                using FileStream fs = File.OpenRead(configPath);
                Config = JsonConvert.DeserializeObject<ConfigJson>(json);
            }
            // Otherwise, use the previous code.
            else
            {
                json = await File.ReadAllTextAsync("assets/config/Configuration.json")
                    .ConfigureAwait(false);
                using FileStream fs = File.OpenRead("assets/config/Configuration.json");
                Config = JsonConvert.DeserializeObject<ConfigJson>(json);
            }

            configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            port = configJson.Port;

            Log.Information("Logger initialized.");
            Log.Information(
                "If you have any problems, please raise a issue on GitHub and upload your log file found in the logs folder."
            );

            // Check if the Discord App ID is set in the configuration file
            // If not, throw an error and exit the program as it's required.
            if (configJson.AppId == "YOUR_APP_ID_HERE")
            {
                Log.Error("Please set your Discord App ID in assets/config/Configuration.json");
                return;
            }

            // Check if the Discord Webhook URL is set in the configuration file
            // If not, throw a warning and disable the webhook feature.
            // Optional feature, so it's not required.
            if (configJson.WebhookUrl == "YOUR_WEBHOOK_URL_HERE")
            {
                Log.Warning(
                    "If you wish to use the Discord Webhook feature, make sure to set your webhook URL in assets/config/Configuration.json."
                );
                webhookEnabled = false;
            }
            else
            {
                Log.Information(
                    "Discord Webhook feature enabled - final race classification data will be sent to the webhook."
                );
                webhook.Uri = new Uri(configJson.WebhookUrl);
                webhookEnabled = true;
            }

            // Create a new instance of DiscordRPC with the Discord App ID from the configuration file
            discord = new DiscordRPC(configJson.AppId);

            // Let's actually bring the Discord client online
            discord.Initialize();

            Log.Information("DiscordRPC initialized.");

            Log.Information("Program initialized. Setting up client..");

            // Create a new instance of the TelemetryClient class with the specified port
            TelemetryClient client = new(port);

            Log.Information($"Client initialized. Listening on port {port}.");
            Log.Information("Waiting for data..");

            // Variables for storing various data
            int teamId = 0;
            string playerName = "";
            string teamName = "";
            var track = "";
            var currentTrackId = "";
            int lapNumber = 0;
            int totalLaps = 0;
            int formulaType = 0;
            string sessionType = "";
            int penalties = 0;
            int totalWarnings = 0;
            int cornerCuttingWarnings = 0;
            int safetyCars = 0;
            int virtualSafetyCars = 0;
            int redFlags = 0;
            int playerIndex = 0;
            int currentPosition = 0;
            int totalParticipants = 0;
            string playerPlatform = "";
            double raceCompletion = 0.0;
            int lobbyPlayerCount = 0;
            int finalPosition = 0;
            int gridPosition = 0;
            int finalPoints = 0;
            int finalResultStatus = 0;
            int weatherId = 0;
            string weatherConditions = "";
            int numPitStops = 0;
            int speedTrapFastestDriverIdx = 0;
            float speedTrapFastestSpeedKmh = 0.0f;
            List<dynamic> penaltiesList = new();
            float fastestLapTime = 0.0f;
            int fastestLapDriverIdx = 0;
            string fastestLapDriver = "";
            int penaltyDriverIdx = 0;
            string penaltyDriverName = "";
            bool networkGame = false;
            var button = new NetDiscordRpc.RPC.Button[]
            {
                new() { Label = "Powered by F1RPC", Url = "https://github.com/xKaelyn/F1RPC" }
            };

            // When first booting system, reset the status by showing a "in menu" presence
            resetStatus(discord);

            // Subscribe to the events for receiving data
            client.OnLapDataReceive += Client_OnLapDataReceive;
            client.OnSessionDataReceive += (packet) =>
                Client_OnSessionDataReceive(packet, discord, teamName);
            client.OnParticipantsDataReceive += Client_OnParticipantsDataReceive;
            client.OnLobbyInfoDataReceive += (packet) =>
                Client_OnLobbyInfoDataReceive(packet, discord);
            client.OnFinalClassificationDataReceive += async (packet) =>
                await Client_OnFinalClassificationDataReceiveAsync(packet, discord);
            client.OnEventDetailsReceive += Client_OnEventDetailsReceive;

            // Method for when receiving event details - used for getting fastest lap and speed trap data
            void Client_OnEventDetailsReceive(EventPacket packet)
            {
                fastestLapTime = packet.eventDetails.fastestLap.lapTime;
                fastestLapDriverIdx = packet.eventDetails.fastestLap.vehicleIdx;
                speedTrapFastestDriverIdx = packet.eventDetails.sppedTrap.vehicleIdx;
                speedTrapFastestSpeedKmh = packet.eventDetails.sppedTrap.speed;
            }

            // Method for when receiving lap data
            // This method is triggered when lap data is received from the telemetry client.
            void Client_OnLapDataReceive(LapDataPacket packet)
            {
                playerIndex = packet.header.playerCarIndex;
                lapNumber = packet.lapData[playerIndex].currentLapNum;
                currentPosition = packet.lapData[playerIndex].carPosition;
                totalWarnings = packet.lapData[playerIndex].totalWarnings;
                numPitStops = packet.lapData[playerIndex].numPitStops;

                // Calculate the race completion percentage based on the current lap number and total laps
                raceCompletion =
                    lapNumber == 1
                        ? 0.0
                        : Math.Round((lapNumber - 1) / (double)(totalLaps - 1) * 100, 2);
            }

            // Method for when recieving lobby info
            // Lobby data is received twice a second, until the game begins and the lobby is destroyed.
            void Client_OnLobbyInfoDataReceive(LobbyInfoPacket packet, DiscordRPC discord)
            {
                lobbyPlayerCount = packet.numPlayers;

                // Variable to change "player" to "players" if more than 1 player is in the lobby - no need to while loop this as it's only modified each time the data is received
                string playerText = "";
                if (lobbyPlayerCount > 1)
                {
                    playerText = "players";
                }
                if (lobbyPlayerCount < 1)
                {
                    playerText = "player";
                }

                // Set presence to "Waiting in the lobby with x other players", -1 because the player itself is not counted
                discord.SetPresence(
                    new RichPresence
                    {
                        Details = $"In a multiplayer lobby | Using {playerPlatform}",
                        State = $"With {lobbyPlayerCount - 1} other {playerText}",
                        Assets = new Assets
                        {
                            LargeImageKey = $"f1_23_logo",
                            LargeImageText = $"F1 23"
                        },
                        Buttons = button
                    }
                );
            }

            // Method for when recieving final classification data - used for getting final position
            // Final Classification data is only received once once the final scoreboard is shown to the player.
            // Method is async to allow for a Task.Delay at the end of the method as we need to assume the user is in the menus - we don't have a way of knowing if they are or not.
            async Task Client_OnFinalClassificationDataReceiveAsync(
                FinalClassificationPacket packet,
                DiscordRPC discord
            )
            {
                finalPosition = packet.classificationData[playerIndex].position;
                gridPosition = packet.classificationData[playerIndex].gridPosition;
                finalPoints = packet.classificationData[playerIndex].points;
                finalResultStatus = (int)packet.classificationData[playerIndex].resultStatus;

                // To-Do: Add session best lap time to presence
                // If user finished a session, but it's not a race
                if (sessionType is not "Race" and not "Race 2" and not "Race 3")
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"Session Completed | Track: {track}",
                            State = $"Racing for {teamName} | Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // If user finished the race
                if (finalResultStatus == 3)
                {
                    if (sessionType is not "Race" and not "Race 2" and not "Race 3")
                    {
                        discord.SetPresence(
                            new RichPresence
                            {
                                Details = $"Session Completed | Track: {track}",
                                State = $"Racing for {teamName} | Using {playerPlatform}",
                                Assets = new Assets
                                {
                                    LargeImageKey = $"{currentTrackId}",
                                    LargeImageText = $"{track}"
                                },
                                Buttons = button
                            }
                        );
                    }
                    else
                    {
                        discord.SetPresence(
                            new RichPresence
                            {
                                Details =
                                    $"Finished: P{finalPosition} / P{totalParticipants} | Started: P{gridPosition} | Track: {track}",
                                State =
                                    $"Racing for {teamName} | {finalPoints} points earned | Using {playerPlatform}",
                                Assets = new Assets
                                {
                                    LargeImageKey = $"{currentTrackId}",
                                    LargeImageText = $"{track}"
                                },
                                Buttons = button
                            }
                        );
                        // Check if webhook feature is enabled
                        if (webhookEnabled == true)
                        {
                            // Capitalize the first letter of the player name if it's not a network game
                            // As otherwise, driver names are all uppercase - e.g RICCIARDO, VERSTAPPEN etc.
                            string playerNameValue = "";
                            if (!networkGame)
                            {
                                playerNameValue =
                                    char.ToUpperInvariant(playerName[0])
                                    + playerName.Substring(1).ToLowerInvariant();
                            }
                            else
                            {
                                playerNameValue = playerName;
                            }
                            DiscordMessage message = new();
                            DiscordEmbed embed =
                                new()
                                {
                                    Author = new EmbedAuthor
                                    {
                                        Name = "F1RPC",
                                        Url = new Uri("https://github.com/xkaelyn/f1rpc"),
                                    },
                                    Title = "Race Finished",
                                    Thumbnail = new EmbedMedia
                                    {
                                        Url = new Uri(
                                            $"https://raw.githubusercontent.com/xKaelyn/F1RPC/master/assets/images/{currentTrackId}.png"
                                        )
                                    },
                                    Url = new Uri("https://github.com/xkaelyn/f1rpc"),
                                    Color = GetEmbedColorByPosition(finalPosition),
                                    Fields = new List<EmbedField>()
                                    {
                                        new() { Name = "Date & Time", Value = $"{DateTime.Now}", },
                                        new() { Name = "Driver", Value = playerNameValue, },
                                        new() { Name = "Track", Value = track },
                                        new() { Name = "Team", Value = teamName },
                                        new()
                                        {
                                            Name = "Virtual Safety Cars",
                                            Value = $"{virtualSafetyCars}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Safety Cars",
                                            Value = $"{safetyCars}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Red Flags",
                                            Value = $"{redFlags}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Penalties",
                                            Value = $"{penalties}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Warnings",
                                            Value = $"{totalWarnings}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Corner Cut Warnings",
                                            Value = $"{cornerCuttingWarnings}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Starting Position",
                                            Value = $"P{gridPosition}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Final Position",
                                            Value = $"P{finalPosition}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Position Change",
                                            Value =
                                                $"{(gridPosition < finalPosition ? "-" : "+ ")}{Math.Abs(gridPosition - finalPosition)}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Number of Pit Stops",
                                            Value = $"{numPitStops}",
                                            Inline = true
                                        },
                                        new()
                                        {
                                            Name = "Championship Points Earned",
                                            Value = $"{finalPoints}",
                                            Inline = true
                                        }
                                    },
                                    Footer = new EmbedFooter
                                    {
                                        Text = $"xKaelyn/F1RPC ~ Version {versionId}"
                                    }
                                };
                            message.Embeds.Add(embed);
                            try
                            {
                                if (webhook != null)
                                {
                                    await webhook.SendAsync(message);
                                }
                                else
                                {
                                    Log.Fatal("Webhook is null.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Fatal($"Error sending webhook: {ex}");
                            }
                        }
                    }
                }

                // If user did not finish the race
                if (finalResultStatus == 4)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"DNF | Started: P{gridPosition} | Track: {track}",
                            State = $"Racing for {teamName} | Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // If user was disqualified
                if (finalResultStatus == 5)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"Disqualified | Started: P{gridPosition} | Track: {track}",
                            State = $"Racing for {teamName} | Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // If user was not classified
                if (finalResultStatus == 6)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"Not Classified | Track: {track}",
                            State = $"Racing for {teamName} | Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // If user retired
                if (finalResultStatus == 7)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"Retired | Started: P{gridPosition} | Track: {track}",
                            State = $"Racing for {teamName} | Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // Wait 15 seconds
                await Task.Delay(15000);
                // Assume the player is in the menus - no way of actually knowing if they are or not
                resetStatus(discord);
            }

            // Method for when recieving participants data - used for getting team name
            // Participant data is received once every 5 seconds.
            void Client_OnParticipantsDataReceive(ParticipantsPacket packet)
            {
                playerIndex = packet.header.playerCarIndex;
                totalParticipants = packet.numActiveCars;
                teamId = (int)packet.participants[playerIndex].teamId;

                penaltyDriverName = new string(packet.participants[penaltyDriverIdx].name);

                playerName =
                    playerIndex >= 0 && playerIndex < packet.participants.Length
                        ? new string(packet.participants[playerIndex].name)
                        : "Unknown";

                fastestLapDriver =
                    fastestLapDriverIdx >= 0 && fastestLapDriverIdx < packet.participants.Length
                        ? new string(packet.participants[fastestLapDriverIdx].name)
                        : "Unknown";

                var playerPlatformInt = (int)packet.participants[playerIndex].platform;

                playerPlatform = playerPlatformInt switch
                {
                    1 => "PC (Steam)",
                    6 => "PC (Origin/EA)",
                    3 => "PlayStation",
                    4 => "Xbox",
                    _ => "Unknown",
                };
                teamName = GetTeamNameFromId(teamId);
            }

            // Method for getting team name from team id (as F1 uses integers)
            string GetTeamNameFromId(int teamId)
            {
                List<dynamic> teamlist =
                    new()
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
                        new { GameId = 124, Name = "AlphaTauri (2022)" },
                        new { GameId = 125, Name = "Haas (2022)" },
                        new { GameId = 126, Name = "McLaren (2022)" },
                        new { GameId = 127, Name = "Alfa Romeo (2022)" },
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
                        new { GameId = 140, Name = "Art GP (2022)" },
                        new { GameId = 143, Name = "ART Grand Prix (2023)" },
                        new { GameId = 144, Name = "Campos Racing (2023)" },
                        new { GameId = 145, Name = "Carlin (2023)" },
                        new { GameId = 146, Name = "PHM Racing (2023)" },
                        new { GameId = 147, Name = "DAMS (2023)" },
                        new { GameId = 148, Name = "Hitech (2023)" },
                        new { GameId = 149, Name = "MP Motorsport (2023)" },
                        new { GameId = 150, Name = "Prema (2023)" },
                        new { GameId = 151, Name = "Trident (2023)" },
                        new { GameId = 152, Name = "Van Amersfoort Racing (2023)" },
                        new { GameId = 153, Name = "Uni-Virtuosi (2023)" }
                    };

                var team = teamlist.FirstOrDefault(t => t.GameId == teamId);
                return team != null ? (string)team.Name : "Unknown";
            }

            // Method for getting weather condition from weather id (as F1 uses integers)
            string GetWeatherConditions(int weatherId) =>
                weatherId switch
                {
                    0 => "Clear",
                    1 => "Light Cloud",
                    2 => "Overcast",
                    3 => "Light Rain",
                    4 => "Heavy Rain",
                    5 => "Storm",
                    _ => throw new Exception("Unknown weather ID")
                };

            // Method for when receiving session data
            // Session data is received twice a second until the session is destroyed. It only contains data about the ongoing session.
            void Client_OnSessionDataReceive(
                SessionPacket packet,
                DiscordRPC discord,
                string teamName
            )
            {
                formulaType = (int)packet.formula;
                totalLaps = packet.totalLaps;
                weatherId = (int)packet.weather;
                currentTrackId = packet.trackId.ToString().ToLower();
                virtualSafetyCars = packet.numVirtualSafetyCarPeriods;
                safetyCars = packet.numSafetyCarPeriods;
                redFlags = packet.numRedFlagPeriods;

                networkGame = packet.networkGame != 0;

                weatherConditions = GetWeatherConditions(weatherId);

                // Case switch for checking track id, setting track name and setting track image
                track = (int)packet.trackId switch
                {
                    0 => "Australia: Melbourne",
                    1 => "France: Le Castellet",
                    2 => "China: Shanghai",
                    3 => "Bahrain: Sakhir",
                    4 => "Spain: Barcelona-Catalunya",
                    5 => "Monaco",
                    6 => "Canada: Montreal",
                    7 => "UK: Silverstone",
                    8 => "Germany: Hockenheim",
                    9 => "Hungary: Budapest",
                    10 => "Belgium: Spa-Francorchamps",
                    11 => "Italy: Monza",
                    12 => "Singapore",
                    13 => "Japan: Suzuka",
                    14 => "Abu Dhabi: Yas Marina",
                    15 => "USA (Texas): COTA",
                    16 => "Brazil: Sao Paolo",
                    17 => "Austria: Spielberg",
                    18 => "Russia: Sochi",
                    19 => "Mexico",
                    20 => "Azerbaijan: Baku",
                    21 => "Bahrain: Sakhir (Short)",
                    22 => "UK: Silverstone (Short)",
                    23 => "USA (Texas): COTA (Short)",
                    24 => "Japan: Suzuka (Short)",
                    25 => "Vietnam: Hanoi",
                    26 => "Netherlands: Zandvoort",
                    27 => "Italy: Imola",
                    28 => "Portugal: Portimao",
                    29 => "Saudi Arabia: Jeddah",
                    30 => "USA (Florida): Miami",
                    31 => "USA (Nevada): Las Vegas",
                    32 => "Qatar: Losail",
                    _ => throw new Exception("Unknown track ID")
                };

                // Case Switch for Packet Session Type
                sessionType = (int)packet.sessionType switch
                {
                    0 => "Unknown",
                    1 => "Practice 1",
                    2 => "Practice 2",
                    3 => "Practice 3",
                    4 => "Short Practice",
                    5 => "Qualifying 1",
                    6 => "Qualifying 2",
                    7 => "Qualifying 3",
                    8 => "Short Qualifying",
                    9 => "One-Shot Qualifying",
                    10 => "Race",
                    11 => "Race 2",
                    12 => "Race 3",
                    13 => "Time Trial",
                    _ => throw new Exception("Unknown session type")
                };

                // Practice / Qualifying
                if ((int)packet.sessionType is >= 1 and <= 9)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"{sessionType} - {track}",
                            State = $"Racing for {teamName} | Conditions: {weatherConditions}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // Race
                if ((int)packet.sessionType is >= 10 and <= 12)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details =
                                $"{sessionType} - {track} | Lap {lapNumber} / {totalLaps} | {raceCompletion}% complete",
                            State =
                                $"Racing for {teamName} | P{currentPosition} / P{totalParticipants} | Conditions: {weatherConditions}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }

                // Time Trial
                if ((int)packet.sessionType == 13)
                {
                    discord.SetPresence(
                        new RichPresence
                        {
                            Details = $"{sessionType} - {track}",
                            State = $"Using {playerPlatform}",
                            Assets = new Assets
                            {
                                LargeImageKey = $"{currentTrackId}",
                                LargeImageText = $"{track}"
                            },
                            Buttons = button
                        }
                    );
                }
            }

            // Method for resetting status - checks if F1 23 is open before executing SetPresence
            void resetStatus(DiscordRPC discord)
            {
                Log.Information($"Connected. Updating status..");
                discord.SetPresence(
                    new RichPresence
                    {
                        Details = "Idle",
                        Assets = new Assets
                        {
                            LargeImageKey = "f1_23_logo",
                            LargeImageText = "F1 23"
                        },
                        Timestamps = new Timestamps(DateTime.UtcNow),
                        Buttons = button
                    }
                );
                Log.Information($"Updated Discord Status: {discord.CurrentPresence.Details}");
            }
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static DiscordColor GetEmbedColorByPosition(int finalPosition)
        {
            if (finalPosition == 1)
            {
                DiscordColor color = new(Color.Gold);
                return color;
            }
            if (finalPosition == 2)
            {
                DiscordColor color = new(Color.Silver);
                return color;
            }
            if (finalPosition == 3)
            {
                DiscordColor color = new(Color.SaddleBrown);
                return color;
            }
            if (finalPosition is >= 4 and <= 10)
            {
                var color = new DiscordColor(Color.Cyan);
                return color;
            }
            else
            {
                var color = new DiscordColor(Color.White);
                return color;
            }
        }
    }
}
