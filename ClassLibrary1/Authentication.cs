using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

namespace ServerLibrary
{
    public class Authentication
    {
        DatabaseConector database;

        #region main_functions
        /// <summary>
        /// Simple constructor, connects to the database upon creation of object.
        /// </summary>
        public Authentication()
        {
            database = new DatabaseConector();
        }

        /// <summary>
        /// Allows creation of new users.
        /// </summary>
        /// <param username="username"></param>
        /// <param password="pass"></param>
        /// <returns></returns>
        public void CreateUser(String username,String pass)
        {
            if (database.Connection == null) { throw new AuthenticationException("Database Error",-1);};
            database.ConnectToDatabase();

            //check if username and password satisfy requirements
            CheckForIllegalParameters(username, pass);
            //check if user is not already defined
            if (checkIfUserExists(username)) { throw new AuthenticationException("User already exists",1); }

            //hash the password before inserting it into the database
            String hashedPassword =  ComputeSha256Hash(pass);

            //insert user into database
            database.Query = "Insert into users (username,password) values(\"" + username + "\",\"" + hashedPassword + "\")";
            database.Command = new MySqlCommand(database.Query, database.Connection);
            database.Adapter.InsertCommand = database.Command;
            database.Adapter.InsertCommand.ExecuteNonQuery();
            database.Command.Dispose();
            database.DisconectFromDatabase();
        }




        /// <summary>
        /// Checks if given username and password combination exists in the database.
        /// Returns true when it exists.
        /// </summary>
        /// <param username="username"></param>
        /// <param password="pass"></param>
        /// <returns>Returns true when it exists.</returns>
        public bool AuthorizeUser(String username, String pass)
        {
            //connect to the database
            if (database.Connection == null) { throw new AuthenticationException("Database Error",-1); };
            try{ database.ConnectToDatabase();}
            catch(Exception) { throw new AuthenticationException("Database Error", -1); }
            
            //hash the password for safety reasons
            String hashedPassword = ComputeSha256Hash(pass);

            //selects the of a record with given username
            database.Query = "SELECT password FROM users WHERE username= \"" + username + "\"";
            database.Command = new MySqlCommand(database.Query, database.Connection);
            database.DataReader = database.Command.ExecuteReader();

            //read the record, store it in variable so the reader can be closed
            String retrivedPassword = "";
            while (database.DataReader.Read()) { retrivedPassword = (string)database.DataReader.GetValue(0); }
            database.DataReader.Close();

            if (retrivedPassword == ""){  throw new AuthenticationException("User not found", 1);}
            if (retrivedPassword == hashedPassword){ return true; }
            throw new AuthenticationException("Invalid password", 1);
            }

        #endregion

        #region helper_funtions

        /// <summary>
        /// Checks if both password and username pass requirements.
        /// Username must be 1-10 characters long, and cannot contain whitespaces.
        /// Password must conatin at least one number and upper case letter.
        /// </summary>
        /// <param username="username"></param>
        /// <param password="pass"></param>
        private void CheckForIllegalParameters(String username, String pass)
        {
            if (username.Length < 1) { throw new AuthenticationException("Username must contain at least one character",1); }
            if (username.Length > 10) { throw new AuthenticationException("Username too long (max 10 chars)",1); }

            bool spaceFlag = false;
            //username cannot consist of white spaces exclusively
            foreach (char c in username)
            {
                if (c==32) { spaceFlag = true;break; }
            }
            if (spaceFlag) { throw new AuthenticationException("Username cannot contain whitespaces", 1); }

            bool upperLetterFlag = false;
            bool numberFlag = false;
            //contains at least one number and at least one uppercase letter
            foreach (char c in pass)
            {
                if(c>64 && c < 91) { upperLetterFlag = true;}
                if(c>47 && c < 57) { numberFlag = true; }
            }

            if (!upperLetterFlag) { throw new AuthenticationException("Pasword must contain at least one upper case letter",1); }
            if (!numberFlag) { throw new AuthenticationException("Pasword must contain at least one number", 1); }
        }

        /// <summary>
        /// Checks if a user of given username exists in database.
        /// Returns true if the user exists, false if they don't.
        /// </summary>
        /// <param username="username"></param>
        /// <returns> Returns true if the user exists, false if they don't.</returns>
        private bool checkIfUserExists(string username)
        {

            //selects a list of all curently registered users
            database.Query = "Select username FROM users WHERE username= \""+username+"\"";
            database.Command = new MySqlCommand(database.Query, database.Connection);
            database.DataReader = database.Command.ExecuteReader();
            String retrivedUser = "";

            while (database.DataReader.Read())
            {
               retrivedUser = (string)database.DataReader.GetValue(0);
            }
            database.DataReader.Close();

            if (retrivedUser == "") { return false;}
            return true;
        }


        /// <summary>
        /// Hashes a given string.
        /// Returns a hashed string.
        /// Taken from: https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
        /// </summary>
        /// <param data to be encoded="rawData"></param>
        /// <returns>Returns a hashed string</returns>
        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        #endregion

    }
}
