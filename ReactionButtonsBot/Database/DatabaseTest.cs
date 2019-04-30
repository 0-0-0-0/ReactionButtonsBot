//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MySql.Data.MySqlClient;

//namespace ReactionButtonsBot.Database
//{
//    static class DatabaseTest
//    {
//        public static bool Test()
//        {
//            MySqlConnection connection = new MySqlConnection
//            {
//                ConnectionString = System.Configuration.ConfigurationManager.AppSettings.Get("mysql")
//            };
//            try
//            {
//                connection.Open();
//                DisplayData(connection.GetSchema());
//                connection.Close();
//                return true;
//            }
//            catch(MySql.Data.MySqlClient.MySqlException ex)
//            {
//                Console.WriteLine(ex.Message);
//                return false;
//            }
            
//        }

//        private static void DisplayData(System.Data.DataTable table)
//        {
//            foreach (System.Data.DataRow row in table.Rows)
//            {
//                foreach (System.Data.DataColumn col in table.Columns)
//                {
//                    Console.WriteLine("{0} = {1}", col.ColumnName, row[col]);
//                }
//                Console.WriteLine("============================");
//            }
//        }

//    }
//}
