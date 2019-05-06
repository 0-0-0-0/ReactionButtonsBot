using MySql.Data.MySqlClient;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactionButtonsBot.Database
{
    class DatabaseManager
    {
        private MySqlConnection connection;

        public DatabaseManager()
        {
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;
            connection = new MySqlConnection(connString);
            connection.Open();
        }

        ~DatabaseManager()
        {
            if (connection != null) { connection.Dispose(); }
        }
        
        private void SaveButton(InlineKeyboardButton button, int keyboard, MySqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "insert into buttons (keyboard, number, text) " +
                    "values (@keyboard, @number, @text)";
                
                command.Parameters.AddWithValue("keyboard", keyboard);
                command.Parameters.AddWithValue("number", button.CallbackData);
                command.Parameters.AddWithValue("text", button.Text);
                command.ExecuteNonQuery();
            }
        }

        public int SaveKeyboard(InlineKeyboardMarkup markup)
        {
            int id;
            MySqlTransaction transaction = connection.BeginTransaction();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "insert into keyboards (id) values (null)";
                    command.ExecuteNonQuery();
                    id = (int)command.LastInsertedId;
                }

                foreach (var row in markup.InlineKeyboard)
                {
                    foreach (var button in row)
                    {
                        SaveButton(button, id, transaction);
                    }
                }

                transaction.Commit();
                return id;
            }
            catch(Exception)
            {
                transaction.Rollback();
                throw new Exception("Transaction failed, succesfully rolled back.");
            }
        }

        public void SaveChat(Chat chat, int? keyboard)
        {
            using (var command = connection.CreateCommand())
            {
                if(keyboard!=null)
                {
                    command.CommandText = "insert into chats values(@chat_id, @keyboard) " +
                        "on duplicate key update keyboard=@keyboard";
                    command.Parameters.AddWithValue("keyboard", keyboard);
                }
                else
                {
                    command.CommandText = "insert into chats values(@chat_id)";
                }
                command.Parameters.AddWithValue("chat_id", chat.Id);
                command.ExecuteNonQuery();
            }
        }

        public int SaveMessage(Message apiMessage, int? keyboard)
        {
            int id;
            var dbMessage = new {
                chat_id = apiMessage.Chat.Id,
                message_id = apiMessage.MessageId,
                keyboard
            };
            using (var command = connection.CreateCommand())
            {
                if (keyboard != null)
                {
                    command.CommandText = "insert into messages (chat_id, message_id, keyboard) " +
                    "values (@chat_id, @message_id, @keyboard) " +
                    "on duplicate key update keyboard=@keyboard";
                    command.Parameters.AddWithValue("keyboard", dbMessage.keyboard);
                }
                else
                {
                    command.CommandText = "insert into messages (chat_id, message_id, keyboard) " +
                    "values (@chat_id, @message_id)";
                }
                command.Parameters.AddWithValue("chat_id", dbMessage.chat_id);
                command.Parameters.AddWithValue("message_id", dbMessage.message_id);
                command.ExecuteNonQuery();
                id = (int)command.LastInsertedId;
            }
            return id;
        }

        public List<string> GetKeyboardButtons(int keyboard)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select text from buttons " +
                    "where keyboard=@keyboard " +
                    "order by number asc";
                command.Parameters.AddWithValue("keyboard", keyboard);
                var reader = command.ExecuteReader();

                var reactions = new List<string>();
                while (reader.Read())
                {
                    Console.WriteLine((string)reader[0]);
                    reactions.Add((string)reader[0]);
                }

                return reactions;
            }
        }

        public int? GetKeyboardId(Chat chat)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select keyboard from chats where chat_id=@chat_id";
                command.Parameters.AddWithValue("chat_id", chat.Id);
                return (int?)command.ExecuteScalar();
            }
        }

        public int? GetKeyboardId(Message message)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select keyboard from messages where chat_id=@chat_id and message_id=@message_id";
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                command.Parameters.AddWithValue("message_id", message.MessageId);
                return (int?)command.ExecuteScalar();
            }
        }
        
        public int? GetMessageId(long chat_id, int message_id)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select id from messages where chat_id=@chat_id and message_id=@message_id";
                command.Parameters.AddWithValue("chat_id", chat_id);
                command.Parameters.AddWithValue("message_id", message_id);
                return (int?)command.ExecuteScalar();
            }
        }

        public int? GetMessageId(Message message)
        {
            return GetMessageId(message.Chat.Id, message.MessageId);
        }

        public int? GetButtonId(int keyboard, byte number)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select id from buttons where keyboard=@keyboard and number=@number";
                command.Parameters.AddWithValue("keyboard", keyboard);
                command.Parameters.AddWithValue("number", number);
                return (int?)command.ExecuteScalar();
            }
        }

        public void SaveReaction(int user_id, int message, int button)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "insert into reactions (user_id, message, button)" +
                    "values (@user_id, @message, @button) " +
                    "on duplicate key update button=@button";
                command.Parameters.AddWithValue("user_id", user_id);
                command.Parameters.AddWithValue("message", message);
                command.Parameters.AddWithValue("button", button);
                command.ExecuteNonQuery();
            }
        }
        
    }
}
