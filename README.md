# Steam Achievement Manager - Scheduler Edition

**Fork of [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager)** with **Achievement Scheduler** feature.

Steam Achievement Manager (SAM) is a lightweight, portable application used to manage achievements and statistics in the popular PC gaming platform Steam. This application requires the [Steam client](https://store.steampowered.com/about/), a Steam account and network access. Steam must be running and the user must be logged in.

## Installation

1. **Install Visual Studio 2022** (Community edition is free) with .NET Framework 4.8 support
2. **Clone this repository** using GitHub Desktop or `git clone`
3. **Double-click `run.bat`** - automatically restores NuGet packages and builds
4. **Run `SAM.Picker.exe`** from the `upload/` folder **(Steam must be running and logged in)**

**Requirements**: Windows 10/11, Steam running, .NET Framework 4.8

## New Features (Scheduler Edition)

- **Achievement Scheduler**: Schedule unlocks for specific date/time with queue system
- **Import/Export Schedules**: Backup your schedule lists to `.sam` files (JSON format) for easy sharing or migration.
- **JSON Persistence**: Saves scheduled achievements to `%APPDATA%/SAM/ScheduledAchievements.json`
- **Auto-Unlock Timer**: Checks every 30s and unlocks via SteamUserStats at scheduled time
- **Tray Notifications**: Balloon tips when achievements unlock automatically
- **New UI Tab**: Dedicated "Scheduler" tab with Add/Remove/Clear buttons + DataGridView list

## Changes from Original

There are some changes to the code since the last closed-source release:
- General code maintenance to bring it into a more modern state.
- Icons have been replaced with ones from the Fugue Icons set.
- Version has been bumped to 7.0.x.x to indicate the open-source release.

## Attribution

Most (if not all) icons are from the [Fugue Icons](https://p.yusukekamiyamane.com/) set.
