# F1RPC

A simple, but effective, Discord RPC program for EA's F1 23.

#### Features
- Displays when the player is waiting in a lobby, and shows how many additional players are in the lobby.
- Displays the player's current session in game (Practice 1, Qualifying 1, Race, Time Trial, etc).
- Displays the player's current position in game (P1, P6, P8 etc) along with the amount of active players in the lobby.
- Displays the player's current team (Red Bull, Alpine, Trident, etc).
- Displays the current lap and the amount of laps remaining, along with an approximation of race completion.
- Displays the track the player is at, along with icons for the country the track is in.

#### Using the program
You have two choices to using the program. You can either:
- Use a pre-compiled exe file by clicking 'Releases' on the right and selecting the latest, or
- Downloading the entire project, running the .sln file and building it yourself from source.
Once you've decided which method you wish to take, you then have to create a Discord Application by going [**here**](https://discord.com/developers/applications) and getting the application ID.
**Note**: You will also need to upload the files from **assets/images** to your Discord Application to get the track icons on the RPC.
**Do not change the names of the image files unless you also modify the code to reflect.**

#### Contributing
You are more than welcome to contribute to the code, just download **[Visual Studio](https://visualstudio.microsoft.com/downloads/)** and you're ready to go.

#### Built using:
- **Microsoft Visual Studio 2022**
- [**f1-sharp**](https://github.com/gvescu/f1-sharp) by gvescu
- [**net-discord-rpc**](https://github.com/HeroesReplay/net-discord-rpc) by HeroesReplay
