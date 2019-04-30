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
            Int32 i = 1;
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

        /// <summary>
        /// Updates the keyboard markup according to a user's reaction
        /// </summary>
        /// <param name="reactions"></param>
        /// <param name="buttonNumber"></param>
        /// <param name="oldReaction"></param>
        /// <returns></returns>
        public static InlineKeyboardMarkup Reaction(IEnumerable<string> reactions, int? buttonNumber, int? oldReaction)
        {
            

            return ReactionsKeyboard(reactions);
        }
    }
}
