![License](https://img.shields.io/github/license/bvdcode/Blink.NET)
[![NuGet](https://img.shields.io/nuget/dt/Blink.NET?color=%239100ff)](https://www.nuget.org/packages/Blink.NET/)
[![NuGet version](https://img.shields.io/nuget/v/Blink.NET.svg?label=nuget)](https://www.nuget.org/packages/Blink.NET/)
[![FuGet](https://img.shields.io/badge/fuget-f88445)](https://www.fuget.org/packages/Blink.NET)
[![Build](https://img.shields.io/github/actions/workflow/status/bvdcode/Blink.NET/.github%2Fworkflows%2Fpublish-release.yml?label=build)](https://github.com/bvdcode/Blink.NET/actions)
[![CodeFactor](https://www.codefactor.io/repository/github/bvdcode/Blink.NET/badge)](https://www.codefactor.io/repository/github/bvdcode/Blink.NET)
![Repo size](https://img.shields.io/github/repo-size/bvdcode/Blink.NET)

# Blink.NET

A .NET library (netstandard2.1) for accessing local Blink camera storage: fetching the list of clips, downloading and deleting videos. Works on runtimes that support .NET Standard 2.1 (for example .NET 6/7/8/9, .NET Core 3.0+).

## Features

- Login/password authorization and PIN confirmation (2FA).
- Fetching dashboard data and the list of Sync Modules.
- Getting the list of clips from a module's local storage.
- Download a clip as a byte array.
- Delete a clip from the device.
- Configurable delay between requests to stabilize the API.

## Installation

Via .NET CLI:

```powershell
dotnet add package Blink.NET
```

Via PackageReference:

```xml
<ItemGroup>
    <PackageReference Include="Blink.NET" Version="x.y.z" />
    <!-- see the latest version on https://www.nuget.org/packages/Blink.NET/ -->
</ItemGroup>
```

## Quick start

Simple scenario: authorize, confirm PIN (if required), download clips from a single Sync Module.

```csharp
using Blink;

var client = new BlinkClient();

// Authorization (reauth:true emulates Blink app behavior)
var auth = await client.AuthorizeAsync(email: "you@example.com", password: "YourPassword", reauth: true);

// If the server requires client verification — enter PIN from SMS and confirm
if (auth.Account.IsClientVerificationRequired)
{
        Console.Write("Enter PIN: ");
        var code = Console.ReadLine();
        await client.VerifyPinAsync(code);
}

// Get clips from a single Sync Module (throws if there are multiple modules)
var videos = await client.GetVideosFromSingleModuleAsync();

// Download the first clip as bytes
var first = videos.First();
byte[] bytes = await client.GetVideoBytesAsync(first);
File.WriteAllBytes($"{first.Id}.mp4", bytes);
```

## Step-by-step usage

1. Authorization and, if necessary, PIN confirmation:

```csharp
var auth = await client.AuthorizeAsync(email, password, reauth: true);
if (auth.Account.IsClientVerificationRequired)
{
        await client.VerifyPinAsync(pinFromSms);
}
```

2. Get Sync Modules and clips:

```csharp
var dashboard = await client.GetDashboardAsync();
var module = dashboard.SyncModules.Single(); // choose the desired module
var videos = await client.GetVideosFromModuleAsync(module);
```

3. Download a clip and (optionally) delete it:

```csharp
var data = await client.GetVideoBytesAsync(video);
await File.WriteAllBytesAsync($"{video.Id}.mp4", data);

// if needed — delete the clip from the device
// await client.DeleteVideoAsync(video);
```

## Client settings

- GeneralSleepTime (int, default 3500 ms)
  Small delay between requests. Without it the server may sometimes return an empty response. You can reduce or disable it if your environment is stable.

- UniqueId (string)
  A unique identifier for the device/client that helps avoid repeated PIN verification. By default generated as a Guid.

## Brief API overview

- Task<LoginResult> AuthorizeAsync(string email, string password, bool reauth = true)
- Task VerifyPinAsync(string code)
- Task<Dashboard> GetDashboardAsync()
- Task<IEnumerable<BlinkVideoInfo>> GetVideosFromModuleAsync(SyncModule module)
- Task<IEnumerable<BlinkVideoInfo>> GetVideosFromSingleModuleAsync()
- Task<byte[]> GetVideoBytesAsync(BlinkVideoInfo video, int tryCount = 3)
- Task DeleteVideoAsync(BlinkVideoInfo video)

See models and exceptions in `Sources/Blink/Models` and `Sources/Blink/Exceptions`.

## Sample console application from the repository

There is a small example in `Sources/Blink.ConsoleTest`:

1. Create a `secrets.json` file next to `Program.cs` with your login/password:

```json
{
  "email": "you@example.com",
  "password": "YourPassword"
}
```

2. Build and run:

```powershell
cd Sources/Blink.ConsoleTest
dotnet build
dotnet run
```

## Requirements and limitations

- A Blink account and at least one Sync Module with local storage are required.
- Client verification (PIN via SMS) is often enabled. This is normal behavior.
- The Blink API can be unstable without pauses between requests — use `GeneralSleepTime`.

## Security

- Do not store login/password in the repository. Use user secrets, environment variables, or encrypted stores.
- Remove tokens and personal data from logs before publishing.

## Building from source

```powershell
dotnet build Sources/Blink/Blink.csproj
```

## Disclaimer

This project is not affiliated with Blink, Amazon, or any other companies. Use at your own risk and in accordance with Blink's terms of service.

## License

MIT — see [LICENSE.md](LICENSE.md).
