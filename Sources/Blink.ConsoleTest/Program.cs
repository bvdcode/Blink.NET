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
            BlinkClient client = string.IsNullOrEmpty(secrets.Token) ?
                new BlinkClient(secrets.Email, secrets.Password) :
                new(secrets.Email, secrets.Password, secrets.Token);

            var authData = await client.AuthorizeAsync(reauth: string.IsNullOrEmpty(secrets.Token));

            if (string.IsNullOrEmpty(secrets.Token))
            {
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
                // await client.DeleteVideoAsync(video);
            }
        }
    }
}