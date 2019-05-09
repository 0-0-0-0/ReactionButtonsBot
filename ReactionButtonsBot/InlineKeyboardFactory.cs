using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReactionButtonsBot
{
    static class InlineKeyboardFactory
    {
        public static InlineKeyboardMarkup ReactionsKeyboard(string reactionsString, char separator=' ')
        {
            var reactions = reactionsString.Split(separator);

            return ReactionsKeyboard(reactions);
        }

        public static InlineKeyboardMarkup ReactionsKeyboard(IEnumerable<string> reactions)
        {
            var markup = new List<InlineKeyboardButton>();
            Int32 i = 0;
            foreach (string reaction in reactions)
            {
                if (reaction.Length == 0) { continue; }
                var button = new InlineKeyboardButton();
                button.Text = reaction;
                button.CallbackData = i.ToString();
                markup.Add(button);

                ++i;
            }
            if (i == 0) { throw new Exception("Empty inline keyboard!"); }
            return new InlineKeyboardMarkup(markup);
        }
        
        public static InlineKeyboardMarkup SetReactionsCount(InlineKeyboardMarkup markup, Dictionary<byte,int> reactionsCount)
        {
            byte i = 0;
            
            foreach(var row in markup.InlineKeyboard)
            {
                foreach (InlineKeyboardButton button in row)
                {
                    button.SetButtonCount(reactionsCount.ContainsKey(i) ? reactionsCount[i] : 0);
                    ++i;
                }
            }

            return markup;
        }

        private static void SetButtonCount(this InlineKeyboardButton button, int count)
        {
            var lastWhiteSpace = button.Text.Trim().LastIndexOf(' ');
            if(lastWhiteSpace != -1 && Int32.TryParse(button.Text.Substring(lastWhiteSpace + 1), out int oldCount))
            {
                button.Text = button.Text.Substring(0, lastWhiteSpace);
            }
            if(count > 0)
            {
                button.Text += ' ';
                button.Text += count;
            }
        }
    }
}
