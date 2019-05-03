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
        

        //DRAFT
        public void SaveButtons(IEnumerable<InlineKeyboardButton> buttons, int keyboard)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "insert into buttons (keyboard, number, text) " +
                    "values (@keyboard, @number, @text)";
                foreach(var button in buttons)
                {
                    command.Parameters.AddWithValue("keyboard", keyboard);
                    command.Parameters.AddWithValue("number", button.CallbackData);
                    command.Parameters.AddWithValue("text", button.Text);
                    command.ExecuteNonQuery();
                }
            }
        }

        //DRAFT
        public int SaveKeyboard(InlineKeyboardMarkup markup)
        {
            int id;
            using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.CommandText = "insert into keyboards (id) values (null)";
                insertCommand.ExecuteNonQuery();
                id = (int)insertCommand.LastInsertedId;
            }
            return id;
        }

        //DRAFT
        public void SaveMessage(Message apiMessage)
        {
            var dbMessage = new {
                chat_id = apiMessage.Chat.Id,
                message_id = apiMessage.MessageId
            };
        }

        //DRAFT (refactored the db, change this accordingly)
        void SaveReaction(Message message, int buttonNumber)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "insert into reactions (chat_id, message_id, user_id, button_number) " +
                    "values (@chat_id, @message_id, @user_id, @button_number) " +
                    "on duplicate key update button_number=@button_number";
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                command.Parameters.AddWithValue("message_id", message.MessageId);
                command.Parameters.AddWithValue("user_id", message.From.Id);
                command.Parameters.AddWithValue("button_number", buttonNumber);
                command.ExecuteNonQuery();
            }
        }

        //DRAFT
        void UpdateReactionsCount(Message message)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select button_number, count(id) from buttons " +
                    "where chat_id=@chat_id and message_id=@message_id " +
                    "group by user_id order by button_number asc";
                command.Parameters.AddWithValue("chat_id", message.Chat.Id);
                command.Parameters.AddWithValue("message_id", message.MessageId);
                var reader = command.ExecuteReader();

                List<int> reactionsCount = new List<int>();
                while (reader.Read())
                {
                    Console.WriteLine((int)reader[1] + " : " + (int)reader[1]);
                    reactionsCount.Add((int)reader[1]);
                }

            }
        }

    }
}
