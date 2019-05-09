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

        //

        #region [DRAFT] globally default keyboards

        /* a more complex variant to change them over time */
        //private void SetGlobalDefaultKeyboard(int keyboard, MySqlTransaction transaction)
        //{
        //    throw new NotImplementedException();
        //    using (var command = connection.CreateCommand())
        //    {
        //        command.Transaction = transaction;
        //        command.CommandText = "insert into global_default_keyboards (keyboard) values (@keyboard)";
        //        command.Parameters.AddWithValue("keyboard", keyboard);
        //        command.ExecuteNonQuery();
        //    }
        //}

        //public object GetGlobalDefaultKeyboard()
        //{
        //    throw new NotImplementedException();
        //    using (var command = connection.CreateCommand())
        //    {
        //        command.CommandText = "select keyboard from global_default_keyboards order by id limit 1";
        //        return command.ExecuteScalar();
        //    }
        //}

        #endregion

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

        /// <summary>
        /// Saves a message with a new inline keyboard
        /// </summary>
        /// <param name="apiMessage"></param>
        /// <param name="markup"></param>
        public void SaveMessage(Message message, InlineKeyboardMarkup markup)
        {
            SaveMessage(message, SaveKeyboard(markup));
        }

        /// <summary>
        /// Saves a message with an existing inline keyboard
        /// </summary>
        /// <param name="apiMessage"></param>
        /// <param name="keyboard"></param>
        public void SaveMessage(Message message, int keyboard)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "insert into messages (chat_id, message_id, keyboard) " +
                "values (@chat_id, @message_id, @keyboard) " +
                "on duplicate key update keyboard=@keyboard";
                command.Parameters.AddWithValue("keyboard", keyboard);
                
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                command.Parameters.AddWithValue("message_id", message.MessageId);
                command.ExecuteNonQuery();
            }
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
                if(reader.HasRows)
                {
                    while (reader.Read())
                    {
                        reactions.Add((string)reader[0]);
                    }
                }
                reader.Close();

                return reactions;
            }
        }

        public Dictionary<byte, int> GetReactionsCount(Message message)
        {
            var result = new Dictionary<byte,int>();
            byte number;
            int count;
            using(var command = connection.CreateCommand())
            {
                command.CommandText = "select number, count(*) from reactions " +
                    "where chat_id=@chat_id and message_id=@message_id " +
                    "group by number";
                command.Parameters.AddWithValue("message_id", message.MessageId);
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        number = reader.GetByte(0);
                        count = reader.GetInt32(1);
                        result[number] = count;
                    }
                }
                reader.Close();
            }
            return result;
        }

        public object GetKeyboard(Chat chat)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select keyboard from chats where chat_id=@chat_id";
                command.Parameters.AddWithValue("chat_id", chat.Id);
                return command.ExecuteScalar();
            }
        }

        public int GetKeyboard(Message message)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select keyboard from messages where chat_id=@chat_id and message_id=@message_id";
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                command.Parameters.AddWithValue("message_id", message.MessageId);
                return (int)command.ExecuteScalar();
            }
        }
        
        public void SaveReaction(int user_id, long chat_id, int message_id, byte number)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select count(*) from reactions " +
                    "where chat_id=@chat_id and message_id=@message_id and user_id=@user_id and number=@number";
                command.Parameters.AddWithValue("chat_id", chat_id);
                command.Parameters.AddWithValue("message_id", message_id);
                command.Parameters.AddWithValue("user_id", user_id);
                command.Parameters.AddWithValue("number", number);
                int count = Convert.ToInt32(command.ExecuteScalar());
                if(count != 0)
                {
                    command.CommandText = "delete from reactions " +
                        "where chat_id=@chat_id and message_id=@message_id and user_id=@user_id";
                    command.ExecuteNonQuery();
                    Console.WriteLine("deleted");
                }
                else
                {
                    command.CommandText = "insert into reactions (chat_id, message_id, user_id, number)" +
                    "values (@chat_id, @message_id, @user_id, @number) " +
                    "on duplicate key update number=@number";
                    command.ExecuteNonQuery();
                    Console.WriteLine("upserted");
                }
            }
        }
        
    }
}
