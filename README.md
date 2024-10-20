
<p align="center">
  <a href="https://github.com/xKaelyn/F1RPC">
    <img src="https://upload.wikimedia.org/wikipedia/commons/3/33/F1.svg" alt="F1RPC Logo" width="300">
  </a>
</p>

<h3 align="center"><b>F1RPC</b></h3>
<p align="center"><b>A simple, effective Discord RPC program for EA's F1 23</b></p>

<p align="center">
<a href="https://app.codacy.com/gh/xKaelyn/F1RPC/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade">
<img alt="Codacy Grade" src="https://img.shields.io/codacy/grade/2a839beeb5744eb99d05d22e54b2f6ce?style=for-the-badge&logo=codacy">
</a>
<img alt=".NET" src="https://img.shields.io/badge/built%20with%20.NET-5C2D91?style=for-the-badge&logo=dotnet">
</p>

<p align="center">
    <a href="#-features"><b>Features</b></a> •
    <a href="#-installation"><b>Installation</b></a> •
    <a href="#screenshots"><b>Screenshots</b></a> •
    <a href="#%EF%B8%8F-contribute"><b>Contribute</b></a>
</p>

---

## 🚀 Features

- Displays the player's current session (Practice, Qualifying, Race, etc.).
- Shows the player's team, current position, and active players in the lobby.
- Displays the current lap, laps remaining, and race completion percentage.
- Shows the track, including icons for the country the track is in.
- (Optional) Submit race results via a Discord Webhook.

## ⚡ Installation

You can use F1RPC by either downloading the pre-compiled executable or building from source.

### Pre-compiled executable
1. Download the latest version from the **Releases** section.
2. Create a Discord Application [here](https://discord.com/developers/applications) and get the Application ID.
3. Upload assets/images to your Discord Application for track icons.
4. Extract the downloaded zip and run the exe file.

### Building from source
1. Clone the repository:
   ```
   git clone https://github.com/xKaelyn/F1RPC.git
   ```
2. Open the `.sln` file in Visual Studio and build the project.

## 💻 Using the Discord Webhook feature

1. Create a Webhook in your Discord channel (Integrations → Webhooks).
2. Copy the Webhook URL.
3. Replace `YOUR_WEBHOOK_URL_HERE` in `assets/config/Configuration.json` with the copied URL.
4. Run F1RPC, and if configured correctly, the feature will enable itself.

## 🛠️ Using multiple UDP telemetry programs

If using multiple telemetry programs like SimHub:
- In SimHub, enable UDP forwarding to a different port (e.g., 20780).
- Set this port in `Configuration.json`.
- Restart F1RPC.

## Screenshots

<p align="center">
    <img src="https://github.com/xKaelyn/F1RPC/assets/20905508/70b8ef9f-d09d-46f3-a63d-9bdaf34743d9" alt="Idle" width="30%">
    <br><br>
    <img src="https://github.com/xKaelyn/F1RPC/assets/20905508/7cd153dc-9d35-4d0e-830c-45b9de31d362" alt="In Race" width="50%">
    <br><br>
    <img src="https://github.com/xKaelyn/F1RPC/assets/20905508/c462ef47-cad0-4f26-b58b-7bdb32e8c2ba" alt="Final Classification" width="50%">
    <br><br>
    <img src="https://github.com/xKaelyn/F1RPC/assets/20905508/ee8ab758-613b-4da0-9673-28b5431e2e8f" alt="Discord Embed" width="80%">
</p>

## ❤️ Contribute

Contributions are welcome! Clone the repo, and feel free to submit pull requests, fix bugs, or improve the code. Ensure you use **Visual Studio** for development.

---

**Built with:**
- Microsoft Visual Studio 2022
- [f1-sharp](https://github.com/gvescu/f1-sharp) by gvescu
- [net-discord-rpc](https://github.com/HeroesReplay/net-discord-rpc) by HeroesReplay
- [CSharpDiscordWebhook](https://github.com/N4T4NM/CSharpDiscordWebhook) by N4T4NM
- [iso-country-flags-svg-collection](https://github.com/joielechong/iso-country-flags-svg-collection?tab=readme-ov-file) by joielechong
