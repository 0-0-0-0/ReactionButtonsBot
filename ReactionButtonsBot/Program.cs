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
            bot.StartReceiving();
            Console.WriteLine("Started receiving");

            while(Hotkey(Console.ReadKey(true)));

            bot.StopReceiving();
        }

        static bool Hotkey(ConsoleKeyInfo hotkey)
        {
            switch (hotkey.KeyChar)
            {
                case 'm':
                    bot.Test("GetMessageId");
                    break;
                default: return false;
            }
            return true;
        }
        
    }
}
