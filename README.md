# Vibely

A Windows desktop conversation-practice studio inspired by modern chat rehearsal apps.

## What this build includes

- Starts completely empty: no demo chats or pre-created contacts.
- Create reusable contacts and personas.
- Create one-to-one or group conversations.
- Write both sides by switching the Send as participant.
- Bubble colors, timestamps, typing indicators and read receipts.
- Practice call simulator with timer and recap.
- PNG conversation export.
- Offline-first JSON persistence under %LOCALAPPDATA%\Vibely.

## Stack

- C#
- WPF
- .NET 10
- No third-party runtime packages required; the app uses only the .NET/WPF platform.

## Run

dotnet restore
dotnet build
dotnet run

The UI and feature set are based on the current public App Store description/version history of Text Simulator, while the implementation here is an independent Windows desktop app.