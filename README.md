# F1RPC

A simple, but effective, Discord RPC program for EA's F1 23.

#### Images
**Idle**

![image](https://github.com/xKaelyn/F1RPC/assets/20905508/70b8ef9f-d09d-46f3-a63d-9bdaf34743d9)

**In Race**

![image](https://github.com/xKaelyn/F1RPC/assets/20905508/7cd153dc-9d35-4d0e-830c-45b9de31d362)

**Final Classification**

![image](https://github.com/xKaelyn/F1RPC/assets/20905508/c462ef47-cad0-4f26-b58b-7bdb32e8c2ba)


#### Features
- Displays when the player is waiting in a lobby, and shows how many additional players are in the lobby.
- Displays the player's current session in game (Practice 1, Qualifying 1, Race, Time Trial, etc).
- Displays the player's current position in game (P1, P6, P8 etc) along with the amount of active players in the lobby.
- Displays the player's current team (Red Bull, Alpine, Trident, etc).
- Displays the current lap and the amount of laps remaining, along with an approximation of race completion.
- Displays the track the player is at, along with icons for the country the track is in.
- (Optional) Send all data from "Final Classification" packet to a Discord Webhook.

#### Using the program
You have two choices to using the program. You can either:
- Use a pre-compiled exe file by clicking 'Releases' on the right and selecting the latest, or
- Downloading the entire project, running the .sln file and building it yourself from source.

#### Using the pre-compiled files
Download the latest version from the **Releases** section on the right hand side and download the zip file.
Create a Discord Application by going [**here**](https://discord.com/developers/applications) and getting the application ID.
**Note: You will also need to upload the files from assets/images to your Discord Application to get the track icons on the RPC.**
When downloaded, extract to a location of your choice and run the exe file. It can be ran either before or after booting F1, it will detect the process and initialize.
Simple as that; enjoy the RPC!

#### Using multiple programs at the same time
If you're using multiple programs for the UDP telemetry at the same time (e.g SimHub, F1Laps etc), you can use SimHub to forward the UDP telemetry to allow it to be broadcast to separate ports at the same time.
- In Simhub select your game and press "Game Config".
- Set UDP forwarding to a different UDP port (like 20780).
- Set this UDP port in the Configuration.json file.
- Restart F1RPC.

**Do not change the names of the image files unless you also modify the code to reflect.**

#### Contributing
You are more than welcome to contribute to the code, just download **[Visual Studio](https://visualstudio.microsoft.com/downloads/)** and you're ready to go.

#### Built using:
- Microsoft Visual Studio 2022
- [**f1-sharp**](https://github.com/gvescu/f1-sharp) by gvescu
- [**net-discord-rpc**](https://github.com/HeroesReplay/net-discord-rpc) by HeroesReplay
- [**CSharpDiscordWebhook**](https://github.com/N4T4NM/CSharpDiscordWebhook) by N4T4NM
- [**iso-country-flags-svg-collection**](https://github.com/joielechong/iso-country-flags-svg-collection?tab=readme-ov-file) by joielechong
