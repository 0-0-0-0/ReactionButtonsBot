using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReactionButtonsBot
{
    class ReactionButtonsBot : TelegramBotClient
    {
        private static readonly InlineKeyboardMarkup defaultReactionsKeyboard;
        private readonly User botUser;

        static ReactionButtonsBot()
        {
            string[] defaultReactions = { "like", "ok", "dislike" };

            defaultReactionsKeyboard = InlineKeyboardFactory.ReactionsKeyboard(defaultReactions);
        }

        public ReactionButtonsBot(string token) : base(token)
        {
            botUser = GetMeAsync().Result;

            OnMessage += ProcessMessage;
            OnCallbackQuery += ProcessCallbackQuery;
        }

        async void ProcessMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Message.MessageId + "@" + e.Message.Chat.Id + " : " + e.Message.Type);
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    await SendTextMessageAsync(e.Message.Chat.Id, e.Message.Text);
                    break;
                    
                case MessageType.Photo:
                    AddReactionsToPhotoMessage(e.Message);
                    break;
                    
            }
        }

        async void AddReactionsToPhotoMessage(Message message)
        {
            var photo = LargestPhotoSize(message.Photo).FileId;
            try
            {
                var replacementMessage = await SendPhotoAsync(message.Chat.Id, photo, message.Caption);
                await EditMessageReplyMarkupAsync(replacementMessage.Chat.Id, replacementMessage.MessageId, defaultReactionsKeyboard);
                await DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.Message.Equals("Bad Request: message can't be deleted"))
                {
                    //ask to grant admin rights?
                }
                Console.WriteLine(message);

            }
        }

        async void ProcessCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Console.WriteLine("Pressed button " + e.CallbackQuery.Data);
            //Console.WriteLine($"User {e.CallbackQuery.From}, chat {e.CallbackQuery.ChatInstance}, message {e.CallbackQuery.Message.MessageId}");

            int buttonNumber = Int32.Parse(e.CallbackQuery.Data);
            Message message = e.CallbackQuery.Message;
            SaveReaction(message, buttonNumber);


            await AnswerCallbackQueryAsync(e.CallbackQuery.Id);
        }

        async void SaveReaction(Message message, int buttonNumber)
        {

        }

        private PhotoSize LargestPhotoSize(PhotoSize[] photoSizes)
        {
            //TODO actually select the largest file (and closest to original)
            return photoSizes[0];
        }
    }
}
