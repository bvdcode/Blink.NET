using System.Text.Json;

namespace Blink.ConsoleTest
{
    public class Program
    {
        public static void Main()
        {
            Start().Wait();
        }

        private static async Task Start()
        {
            string json = File.ReadAllText("secrets.json");
            var secrets = JsonSerializer.Deserialize<Secrets>(json)!;

            // There is two ways to authorize with Blink API: with credentials or with already obtained token.
            const bool authorizeWithCredentials = false;

            BlinkClient client = authorizeWithCredentials ?
                new BlinkClient(secrets.Email, secrets.Password) :
                new(secrets.Token, secrets.Tier, secrets.ClientId, secrets.AccountId);

            if (authorizeWithCredentials)
            {
                var authData = await client.AuthorizeAsync();
                string code = Console.ReadLine() ?? throw new Exception("No code entered");
                await client.VerifyPinAsync(code);

                // Just save the authorization data and use it later to authorize.
                Console.WriteLine("Auth data: " + authData.ToJson());
            }

            var videos = await client.GetVideosAsync();
            int count = videos.Count();

            Console.WriteLine("Videos count: " + count);

            foreach (var video in videos)
            {
                byte[] bytes = await client.GetVideoAsync(video);
                Console.WriteLine($"Video {video.Id} bytes: {bytes.Length}");
            }
        }
    }
}