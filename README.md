[![GitHub](https://img.shields.io/github/license/bvdcode/Blink.NET)](https://github.com/bvdcode/Blink.NET/blob/main/LICENSE.md)
[![Nuget](https://img.shields.io/nuget/dt/Blink.NET?color=%239100ff)](https://www.nuget.org/packages/Blink.NET/)
[![Static Badge](https://img.shields.io/badge/fuget-f88445?logo=readme&logoColor=white)](https://www.fuget.org/packages/Blink.NET)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/bvdcode/Blink.NET/.github%2Fworkflows%2Fpublish-release.yml)](https://github.com/bvdcode/Blink.NET/actions)
[![NuGet version (Blink.NET)](https://img.shields.io/nuget/v/Blink.NET.svg?label=stable)](https://www.nuget.org/packages/Blink.NET/)
[![CodeFactor](https://www.codefactor.io/repository/github/bvdcode/Blink.NET/badge)](https://www.codefactor.io/repository/github/bvdcode/Blink.NET)
![GitHub repo size](https://img.shields.io/github/repo-size/bvdcode/Blink.NET)


# Blink.NET

.NET Standard library for downloading video from local storage of Blink cameras


# Code Example

```csharp
BlinkClient client = new(secrets.Email, secrets.Password);

var authData = await client.AuthorizeAsync();
string code = Console.ReadLine() ?? throw new Exception("No code entered");
await client.VerifyPinAsync(code);

var videos = await client.GetVideosAsync();
int count = videos.Count();
Console.WriteLine("Videos count: " + count);
```