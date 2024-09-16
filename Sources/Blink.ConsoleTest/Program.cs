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
            BlinkClient client = new(secrets.Email, secrets.Password);

            var authData = await client.AuthorizeAsync();
            string code = Console.ReadLine() ?? throw new Exception("No code entered");
            await client.VerifyPinAsync(code);


        }
    }
}