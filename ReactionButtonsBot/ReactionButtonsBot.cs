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
        private readonly User botUser;
        private readonly string botMention;
        private Database.DatabaseManager dbManager;

        static ReactionButtonsBot()
        {
            string[] defaultReactions = { "like", "ok", "dislike" };
            defaultReactionsKeyboard = InlineKeyboardFactory.ReactionsKeyboard(defaultReactions);
        }

        public ReactionButtonsBot(string token) : base(token)
        {
            botUser = GetMeAsync().Result;
            botMention = '@' + botUser.Username;

            dbManager = new Database.DatabaseManager();

            OnMessage += ProcessMessage;
            OnCallbackQuery += ProcessCallbackQuery;
        }

        async void ProcessMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Message.MessageId + "@" + e.Message.Chat.Id + " : " + e.Message.Type);
            switch (e.Message.Type)
            {
                case MessageType.Text:
                    if(isMentioned(e.Message))
                    {
                        ProcessCommands(e.Message);
                    }
                    break;
                    
                case MessageType.Photo:
                case MessageType.Document:
                    AddReactionsToMessage(e.Message);
                    break;
                
            }
        }

        private bool isMentioned(Message message)
        {
            if(message.Chat.Type == ChatType.Private) { return true; }

            if (message.EntityValues != null)
            {
                if(message.EntityValues.Contains(botMention)) { return true; }
                
                //in case of /command@bot
                foreach (string value in message.EntityValues)
                {
                    if (value[0]=='/' && value.Contains(botMention)) { return true; }
                }
            }

            return false;
        }

        private void ProcessCommands(Message message)
        {
            if (message.Entities == null) { return; }

            int i = 0; string entityValue;
            foreach (var entity in message.Entities)
            {
                entityValue = message.EntityValues.ElementAt(i);
                switch (entity.Type)
                {
                    case MessageEntityType.BotCommand:
                        ExecuteBotCommand(entityValue, message);
                        break;
                }
                ++i;
            }
        }

        private void ExecuteBotCommand(string commandEntityValue, Message message)
        {
            Console.WriteLine(commandEntityValue);
            string command = commandEntityValue.Substring(1).Replace(botMention, "").ToLower();
            Console.WriteLine(command);
            switch(command)
            {
                case "setkeyboard":
                    try {
                        string reactionsString = message.Text.Replace(commandEntityValue, "");
                        SetReactionsKeyboard(message.Chat, reactionsString);
                    }
                    catch(Exception ex) { Console.WriteLine(ex.Message); }
                    break;
            }
        }

        private void SetReactionsKeyboard(Chat chat, string reactionsString)
        {
            int keyboard = dbManager.SaveKeyboard(InlineKeyboardFactory.ReactionsKeyboard(reactionsString));
            dbManager.SaveChat(chat, keyboard);
        }

        async Task<Message> ReplaceMessage(Message message, InlineKeyboardMarkup markup=null)
        {
            int replyTomessageId = (message.ReplyToMessage != null) ? message.ReplyToMessage.MessageId : 0;
            Message replacementMessage = null;
            switch (message.Type)
            {
                case MessageType.Text:
                    replacementMessage = await SendTextMessageAsync(message.Chat.Id, message.Text, ParseMode.Default, false, true, replyTomessageId, markup);
                    break;
                case MessageType.Photo:
                    replacementMessage = await SendPhotoAsync(message.Chat.Id, GetPhotoFileId(message.Photo), message.Caption, ParseMode.Default, true, replyTomessageId, markup);
                    break;
                case MessageType.Document:
                    replacementMessage = await SendDocumentAsync(message.Chat.Id, message.Document.FileId, message.Caption, ParseMode.Default, true, replyTomessageId, markup);
                    break;
                /*
            case MessageType.Animation:
                return await SendAnimationAsync(message.Chat.Id, message.Animation.FileId);
                */
                default: return null;
            }
            try { await DeleteMessageAsync(message.Chat.Id, message.MessageId); }
            catch (Telegram.Bot.Exceptions.BadRequestException)
            {
                await SendTextMessageAsync(message.Chat.Id, 
                    "Only messages sent by a bot can have buttons attached to them.\n" +
                    "So I actually replace them with my own (preserving the content).\n" +
                    "To avoid duplicates, I need a permission to delete (original) messages.");
            }

            return replacementMessage;
        }

        async Task AddReactionsToMessage(Message message)
        {
            try
            {
                int? keyboard = dbManager.GetKeyboardId(message.Chat);
                if (keyboard == null)
                {
                    keyboard = dbManager.SaveKeyboard(defaultReactionsKeyboard);
                    dbManager.SaveChat(message.Chat, keyboard);
                    return;
                }
                dbManager.SaveMessage(message, keyboard);
                var markup = InlineKeyboardFactory.ReactionsKeyboard(dbManager.GetKeyboardButtons((int)keyboard));

                await ReplaceMessage(message, markup);
            }
            catch(Exception ex) { Console.WriteLine(ex.Message); }
        }

        async Task AddReactionsToPhotoMessage(Message message)
        {
            var photo = GetPhotoFileId(message.Photo);
            try
            {
                int? keyboard = dbManager.GetKeyboardId(message.Chat);
                if(keyboard==null)
                {
                    keyboard = dbManager.SaveKeyboard(defaultReactionsKeyboard);
                    dbManager.SaveChat(message.Chat, keyboard);
                    return;
                }
                dbManager.SaveMessage(message, keyboard);
                var markup = InlineKeyboardFactory.ReactionsKeyboard(dbManager.GetKeyboardButtons((int)keyboard));

                var replacementMessage = await SendPhotoAsync(message.Chat.Id, photo, message.Caption);
                await EditMessageReplyMarkupAsync(replacementMessage.Chat.Id, replacementMessage.MessageId, markup);
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

            
            Message message = e.CallbackQuery.Message;

            try
            {
                if (!Byte.TryParse(e.CallbackQuery.Data, out byte buttonNumber))
                {
                    throw new Exception("Invalid callback data!");
                }

                int? dbMessageId = dbManager.GetMessageId(message);
                if (dbMessageId == null) throw new Exception("message is not in DB");
                
                int? keyboard = dbManager.GetKeyboardId(message);
                if (keyboard == null) throw new Exception("Inline keyboard not found in DB");

                int? button = dbManager.GetButtonId((int)keyboard, buttonNumber);
                if (button == null) { throw new Exception("Invalid callback data!"); }

                dbManager.SaveReaction(e.CallbackQuery.From.Id, (int)dbMessageId, (int)button);
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                await AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Sorry, there appears to be some bug. Try again.", true);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try { await AnswerCallbackQueryAsync(e.CallbackQuery.Id); }
            catch(Telegram.Bot.Exceptions.InvalidQueryIdException) { /*It's OK*/ }
        }
        
        string GetPhotoFileId(PhotoSize[] photoSizes)
        {
            //TODO actually select the largest file (and closest to original)
            return photoSizes[0].FileId;
        }

        public void Test(string s)
        {
            switch (s)
            {
                
            }
        }
    }
}
