using System.Text.Json;

namespace Blink.ConsoleTest
{
    public class Program
    {
        public static void Main()
        {
            string json = File.ReadAllText("secrets.json");
            var secrets = JsonSerializer.Deserialize<Secrets>(json)!;
        }
    }
}