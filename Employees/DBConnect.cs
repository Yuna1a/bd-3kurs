using System;
using System.Data;
using Npgsql;
using System.Windows;

namespace DBConnect
{
    public class DB
    {
        private string host, port, dbName, username, password, connString;
        private NpgsqlConnection sc;
        private NpgsqlDataAdapter sda;
        public DB(string _dbName, string _username, string _password)
        {
            host = "localhost";
            port = "5432";
            dbName = _dbName;
            username = _username;
            password = _password;
            connString = "Server=" + host + ";Port=" + port + ";Database=" + dbName + ";User ID=" + username + ";Password=" + password + ";";
        }

        public void DbConnect()
        {
            sc = new NpgsqlConnection(connString);
            sda = new NpgsqlDataAdapter();

            sc.Open();
            MessageBox.Show("Подключено.");
        }
        // запросы
        public DataTable execute(string request)
        {
            DataTable dt = new DataTable();
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(request, sc);

                sda = new NpgsqlDataAdapter(command);
                sda.Fill(dt);

                return dt;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }
        }

        public void Disconnect()
        {
            sc.Close();
        }

    };

}
