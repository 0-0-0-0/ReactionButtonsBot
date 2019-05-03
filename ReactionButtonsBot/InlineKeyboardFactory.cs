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
        public static InlineKeyboardMarkup ReactionsKeyboard(IEnumerable<string> reactions)
        {
            var markup = new List<InlineKeyboardButton>();
            Int32 i = 0;
            foreach(string reaction in reactions)
            {
                var button = new InlineKeyboardButton();
                button.Text = reaction;
                button.CallbackData = i.ToString();
                markup.Add(button);

                ++i;
            }
            
            return new InlineKeyboardMarkup(markup);
        }
        
        public static InlineKeyboardMarkup SetReactionsCount(InlineKeyboardMarkup markup, IEnumerable<int> reactionsCount)
        {
            int i = 0;
            foreach(var row in markup.InlineKeyboard)
            {
                foreach (InlineKeyboardButton button in row)
                {
                    if(i < reactionsCount.Count()) { button.SetButtonCount(reactionsCount.ElementAt(i)); }
                }
            }

            return markup;
        }

        private static void SetButtonCount(this InlineKeyboardButton button, int count)
        {
            var lastWordIndex = button.Text.Trim().LastIndexOf(' ') + 1;
            if(lastWordIndex == -1 || !Int32.TryParse(button.Text.Substring(lastWordIndex), out int oldCount))
            {
                button.Text += ' ';
            }
            else
            {
                button.Text = button.Text.Substring(0, lastWordIndex);
            }
            button.Text += count;
        }
    }
}
