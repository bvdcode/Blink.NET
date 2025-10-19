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

Simple scenario: login, complete 2FA, then download clips from a single Sync Module.

```csharp
using Blink;

var client = new BlinkClient();

// 1) Login with email/password
bool okLogin = await client.TryLoginAsync("you@example.com", "YourPassword");
if (!okLogin)
{
  throw new Exception("Wrong email or password");
}

// 2) Enter and verify 2FA code
Console.Write("Enter 2FA code: ");
var code = Console.ReadLine() ?? string.Empty;
bool ok2FA = await client.TryVerifyPinAsync(code);
if (!ok2FA)
{
  throw new Exception("Invalid 2FA code");
}

// 3) Get clips from a single Sync Module (throws if more than one module exists)
var videos = await client.GetVideosFromSingleModuleAsync();

// 4) Download the first clip as bytes
var first = videos.First();
byte[] bytes = await client.GetVideoBytesAsync(first);
File.WriteAllBytes($"{first.Id}.mp4", bytes);
```

### Quick start (with Serilog and refresh token)

Use a two-stage flow: first login with email/password and complete 2FA to obtain a refresh token; then reuse the refresh token on subsequent runs to skip 2FA.

```csharp
using Serilog;
using Blink;

Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console()
  .CreateLogger();

var client = new BlinkClient();

// 1) First-time login + 2FA to get refresh token
bool okLogin = await client.TryLoginAsync(email, password);
if (!okLogin)
{
  Log.Error("Wrong email or password");
  return;
}

Log.Information("Enter 2FA code:");
while (true)
{
  string code = Console.ReadLine() ?? string.Empty;
  if (string.IsNullOrWhiteSpace(code))
  {
    Log.Warning("Code cannot be empty. Please enter the 2FA code:");
    continue;
  }
  bool ok2FA = await client.TryVerifyPinAsync(code);
  if (ok2FA)
  {
    Log.Information("2FA verification successful.");
    break;
  }
  Log.Error("Invalid 2FA code. Please try again:");
}

Log.Information("Save this refresh token for future use: {RefreshToken}", client.RefreshToken);

// 2) Subsequent runs — login with refresh token
bool okRefresh = await client.TryLoginWithRefreshTokenAsync(refreshTokenFromStore);
if (!okRefresh)
{
  Log.Error("Failed to refresh token");
  return;
}

var dashboard = await client.GetDashboardAsync();
Log.Information("Dashboard retrieved. Modules count: {Count}", dashboard.SyncModules.Length);
```

## Step-by-step usage

1. Login and, if necessary, PIN confirmation:

```csharp
bool okLogin = await client.TryLoginAsync(email, password);
if (!okLogin)
{
  // invalid credentials
  return;
}
bool ok2FA = await client.TryVerifyPinAsync(pinFromSms);
if (!ok2FA)
{
  // invalid/expired code
  return;
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
  Small delay between requests. Without it the server may sometimes return an empty response. You can reduce or disable it if your environment is stable. For background jobs, consider higher values (e.g., 5–10 seconds) to improve reliability.

Token handling:

- RefreshToken (string?) — populated after successful 2FA. Store it securely and use `TryLoginWithRefreshTokenAsync` to skip 2FA on subsequent runs.

## Brief API overview

- Task<Dashboard> GetDashboardAsync()
- Task<IEnumerable<BlinkVideoInfo>> GetVideosFromModuleAsync(SyncModule module)
- Task<IEnumerable<BlinkVideoInfo>> GetVideosFromSingleModuleAsync()
- Task<byte[]> GetVideoBytesAsync(BlinkVideoInfo video, int tryCount = 3)
- Task DeleteVideoAsync(BlinkVideoInfo video)

Login/token flows:

- Task<bool> TryLoginAsync(string email, string password)
- Task<bool> TryVerifyPinAsync(string code)
- Task<bool> TryLoginWithRefreshTokenAsync(string refreshToken)
- string? RefreshToken { get; }

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
