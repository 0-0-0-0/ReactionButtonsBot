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
using MySql.Data.MySqlClient;

namespace ReactionButtonsBot
{
    class ReactionButtonsBot : TelegramBotClient
    {
        private static readonly InlineKeyboardMarkup defaultReactionsKeyboard;
        private Database.DatabaseManager databaseManager;

        static ReactionButtonsBot()
        {
            string[] defaultReactions = { "like", "ok", "dislike" };
            defaultReactionsKeyboard = InlineKeyboardFactory.ReactionsKeyboard(defaultReactions);
        }

        public ReactionButtonsBot(string token) : base(token)
        {
            databaseManager = new Database.DatabaseManager();

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

            byte buttonNumber = Byte.Parse(e.CallbackQuery.Data);
            Message message = e.CallbackQuery.Message;

            try
            {
                //SaveReaction(message, buttonNumber);
                //UpdateReactionsCount(...);
            }
            catch(MySqlException ex)
            {
                await AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Sorry, there appears to be some bug. Try again.", true);
                Console.WriteLine(ex.Message);
            }

            try
            {
                //doesn't really matter if this fails
                await AnswerCallbackQueryAsync(e.CallbackQuery.Id);
            }
            catch(Telegram.Bot.Exceptions.InvalidQueryIdException) { }
        }
        
        PhotoSize LargestPhotoSize(PhotoSize[] photoSizes)
        {
            //TODO actually select the largest file (and closest to original)
            return photoSizes[0];
        }

    }
}
