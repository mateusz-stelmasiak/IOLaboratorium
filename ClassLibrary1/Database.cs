using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLibrary
{
    class DatabaseConector {
        #region mysql_Fields

        MySqlDataReader dataReader;
        MySqlDataAdapter adapter;
        MySqlCommand command;
        MySqlConnection connection;
        String query;

        public MySqlConnection Connection{ get => connection; set { connection = value; } }
        public MySqlCommand Command { get => command; set { command = value; } }
        public MySqlDataReader DataReader {  get => dataReader;  set { dataReader = value; } }
        public MySqlDataAdapter Adapter { get => adapter; set { adapter = value; } }
        public String Query { get => query; set { query = value; } }
        #endregion

        #region login_info
        string server = "mariadb105.alhambra.nazwa.pl";
        string database = "alhambra_PPDatabase";
        string uid = "alhambra_PPDatabase";
        string password = "Pudzian123";
        #endregion

        public DatabaseConector(){
            SetUpDatabase();
            adapter = new MySqlDataAdapter();
        }


        #region functions

        /// <summary>
        /// Connects to the database with provided information.
        /// </summary>
        private void SetUpDatabase()
        {
            try
            {
                string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
                database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
                Connection = new MySqlConnection(connectionString);
            }
            catch (Exception e)
            {
                Console.Write("Database error: " + e);
            }
        }


        public void DisconectFromDatabase()
        {
            Connection.Close();
        }
        public void ConnectToDatabase()
        {
            Connection.Open();
        }
        #endregion
    }

}
