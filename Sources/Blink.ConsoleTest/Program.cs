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
            BlinkClient client = new();
            var authData = await client.AuthorizeAsync(secrets.Email, secrets.Password, reauth: true);

            if (authData.Account.IsClientVerificationRequired)
            {
                string code = Console.ReadLine() ?? throw new Exception("No code entered");
                await client.VerifyPinAsync(code);
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