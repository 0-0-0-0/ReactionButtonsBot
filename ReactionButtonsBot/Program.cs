using System;

namespace ReactionButtonsBot
{
    class Program
    {
        private static ReactionButtonsBot bot = new ReactionButtonsBot(
            System.Configuration.ConfigurationManager.AppSettings.Get("token")
            );
        
        static void Main(string[] args)
        {
            //while (!Database.DatabaseTest.Test()) Console.WriteLine("...");
            //Console.WriteLine("Database connection OK");

            bot.StartReceiving();
            Console.WriteLine("Started receiving");
            Console.ReadKey();
            bot.StopReceiving();
        }
        
    }
}
