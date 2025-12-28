# Steam Achievement Manager

Steam Achievement Manager (SAM) is a lightweight, portable application used to manage achievements and statistics in the popular PC gaming platform Steam. This application requires the [Steam client](https://store.steampowered.com/about/), a Steam account and network access. Steam must be running and the user must be logged in.

This is the code for SAM. The closed-source version originally released in 2008, last major release in 2011, and last updated in 2013 (a hotfix).

The code is being made available so that those interested can do as they like with it.

## Installation

1. **Install Visual Studio 2022** (Community edition is free) with .NET Framework 4.8 support
2. **Clone this repository** using GitHub Desktop or `git clone`
3. **Double-click `run.bat`** - automatically restores NuGet packages and builds
4. **Run `SAM.Picker.exe`** from the `upload/` folder

**Requirements**: Windows 10/11, Steam running, .NET Framework 4.8

## New Features (Scheduler Edition)

- **Achievement Scheduler**: Schedule unlocks for specific date/time with queue system
- **Background Execution**: Runs minimized in Windows system tray (NotifyIcon)
- **JSON Persistence**: Saves scheduled achievements to `%APPDATA%/SAM/ScheduledAchievements.json`
- **Auto-Unlock Timer**: Checks every 30s and unlocks via SteamUserStats at scheduled time
- **Tray Notifications**: Balloon tips when achievements unlock automatically
- **New UI Tab**: Dedicated "Scheduler" tab with Add/Remove/Clear buttons + DataGridView list

There are some changes to the code since the last closed-source release:
- General code maintenance to bring it into a more modern state.
- Icons have been replaced with ones from the Fugue Icons set.
- Version has been bumped to 7.0.x.x to indicate the open-source release.
- **Achievement scheduling system with background execution**

## Attribution

Most (if not all) icons are from the [Fugue Icons](https://p.yusukekamiyamane.com/) set.
