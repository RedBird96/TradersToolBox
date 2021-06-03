using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Dapper;
using System.Globalization;
using System.Management;
using System.IO;
using TradersToolbox.Views;
using TradersToolbox.Core;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Threading;

namespace TradersToolbox.Data
{
    public class DBManager
    {
        private static Mutex mutex = new Mutex();
        private string LoadConnectionString(string id = "Default")
        {
            //return ConfigurationManager.ConnectionStrings[id].ConnectionString;
            return @"Data Source=.\\" + TradersToolbox.Properties.Resources.DB_NAME +".db;Version=3;";
        }

        private bool checkExistTable(string table)
        {
            using (var cnn = new SQLiteConnection(LoadConnectionString()))
            {
                using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS "+ table + " (id INTEGER NOT NULL, Symbol VARCHAR(100), OpenTime DATETIME, OpenPrice DOUBLE, ClosePrice DOUBLE, Shares INTEGER, Profit DOUBLE, OrderType VARCHAR(100));"))
                {
                    cmd.Connection = cnn;
                    cmd.Connection.Open();
                    try {
                        cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        Logger.Current.Trace(ex.StackTrace);
                    }

                }
            }
            return true;
        }

        private bool checkExistSummaryTable(string table)
        {
            using (var cnn = new SQLiteConnection(LoadConnectionString()))
            {
                using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS " + table + " (Date DATETIME, Symbol VARCHAR(100), Unrealized DOUBLE, Realized DOUBLE, Total DOUBLE);"))
                {
                    cmd.Connection = cnn;
                    cmd.Connection.Open();
                    try
                    {
                        cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        Logger.Current.Trace(ex.StackTrace);
                    }

                }
            }
            return true;
        }

        public void emptyCookieDatabase(int profileNo)
        {
            try
            {
                var pathWithEnv = $"%USERPROFILE%\\AppData\\Local\\BraveSoftware\\Brave-Browser\\User Data\\Profile {profileNo}\\Cookies";
                var cookieFilePath = Environment.ExpandEnvironmentVariables(pathWithEnv);

                if (File.Exists(cookieFilePath) == false)
                    return;

                string dbConnectionString = $"Data Source={cookieFilePath};Version=3;";
                using (IDbConnection cnn = new SQLiteConnection(dbConnectionString))
                {
                    var result = cnn.Execute("delete from cookies");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("emptyCookieDatabase " + e.StackTrace);
            }
        }

        public void initializeDatabase()
        {
            checkExistTable("positionhistory");
            checkExistTable("transactionhistory");
            checkExistSummaryTable("summaryhistory");
            checkExistTable("openordershistory");
        }

        public void addPositionRecord(OrderData oData)
        {
            checkExistTable("positionhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "insert into positionhistory (id, Symbol, OpenTime, OpenPrice, ClosePrice, Shares, Profit, OrderType) values (@id,@Symbol, @OpenTime, @OpenPrice, @ClosePrice, @Shares, @Profit, @OrderType)";
                    command.Parameters.AddWithValue("@id", oData.Id);
                    command.Parameters.AddWithValue("@Symbol", oData.Symbol);
                    command.Parameters.AddWithValue("@OpenTime", oData.Open_Time);
                    command.Parameters.AddWithValue("@OpenPrice", oData.Open_Price);
                    command.Parameters.AddWithValue("@ClosePrice", oData.Close_Price);
                    command.Parameters.AddWithValue("@Shares", oData.Shares);
                    command.Parameters.AddWithValue("@Profit", oData.Profit);
                    command.Parameters.AddWithValue("@OrderType", oData.Type);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public ObservableCollection<OrderData> getPositionRecordsAll()
        {
            checkExistTable("positionhistory");
            ObservableCollection<OrderData> output = new ObservableCollection<OrderData>();
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from positionhistory";
                    SQLiteDataReader oReader =  command.ExecuteReader();
                    while (oReader.Read())
                    {
                        OrderData od = new OrderData();
                        od.Id = oReader.GetInt32(0);
                        od.Symbol = oReader.GetString(1);
                        od.Open_Time = oReader.GetDateTime(2);
                        od.Open_Price = oReader.GetDouble(3);
                        od.Close_Price = oReader.GetDouble(4);
                        od.Shares = oReader.GetInt32(5);
                        od.Profit = oReader.GetDouble(6);
                        od.Type = oReader.GetString(7);
                        output.Add(od);
                    }
                }
                return output;
            }
        }

        public void removePositionRecord(OrderData oData)
        {
            checkExistTable("positionhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "delete from positionhistory where id = @id";
                    command.Parameters.AddWithValue("@id", oData.Id);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public void updatePositionRecord(OrderData oData, string Key, string Value)
        {
            checkExistTable("positionhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "update positionhistory set " +  Key +"='" + Value + "' where id=" + oData.Id.ToString();
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public void addTransactionRecord(TransactionData oData)
        {
            checkExistTable("transactionhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "insert into transactionhistory (id, Symbol, OpenTime, OpenPrice, Shares, Profit, OrderType) values (@id,@Symbol, @OpenTime, @OpenPrice, @Shares, @Profit, @OrderType)";
                    command.Parameters.AddWithValue("@id", oData.Id);
                    command.Parameters.AddWithValue("@Symbol", oData.Symbol);
                    command.Parameters.AddWithValue("@OpenTime", oData.Open_Time);
                    command.Parameters.AddWithValue("@OpenPrice", oData.Open_Price);
                    command.Parameters.AddWithValue("@Shares", oData.Shares);
                    command.Parameters.AddWithValue("@Profit", oData.Profit);
                    command.Parameters.AddWithValue("@OrderType", oData.Type);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public ObservableCollection<TransactionData> getTransactionRecordsAll()
        {
            checkExistTable("transactionhistory");

            ObservableCollection<TransactionData> output = new ObservableCollection<TransactionData>();
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from transactionhistory";
                    SQLiteDataReader oReader = command.ExecuteReader();
                    while (oReader.Read())
                    {
                        TransactionData od = new TransactionData();
                        od.Id = oReader.GetInt32(0);
                        od.Symbol = oReader.GetString(1);
                        od.Open_Time = oReader.GetDateTime(2);
                        od.Open_Price = oReader.GetDouble(3);
                        od.Shares = oReader.GetInt32(5);
                        od.Profit = oReader.GetDouble(6);
                        od.Type = oReader.GetString(7);
                        output.Add(od);
                    }
                }

                return output;
            }
        }

        public void addSummaryRecord(SummaryData sd)
        {
            checkExistSummaryTable("summaryhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "insert into summaryhistory (Date, Symbol, Unrealized, Realized, Total) values (@Date, @Symbol, @Unrealized, @Realized, @Total)";
                    command.Parameters.AddWithValue("@Date", sd.Date);
                    command.Parameters.AddWithValue("@Symbol", sd.Symbol);
                    command.Parameters.AddWithValue("@Unrealized", sd.Unrealized);
                    command.Parameters.AddWithValue("@Realized", sd.Realized);
                    command.Parameters.AddWithValue("@Total", sd.Total);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public ObservableCollection<SummaryData> getSummaryRecordsAll()
        {
            checkExistSummaryTable("summaryhistory");

            ObservableCollection<SummaryData> output = new ObservableCollection<SummaryData>();
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from summaryhistory";
                    SQLiteDataReader oReader = command.ExecuteReader();
                    while (oReader.Read())
                    {
                        SummaryData sd = new SummaryData();
                        sd.Date = oReader.GetDateTime(0);
                        sd.Symbol = oReader.GetString(1);
                        sd.Unrealized = oReader.GetDouble(2);
                        sd.Realized = oReader.GetDouble(3);
                        sd.Total = oReader.GetDouble(4);
                        output.Add(sd);
                    }
                }
                return output;
            }
        }

        public void addOpenOrderRecord(OrderData oData)
        {
            checkExistTable("openordershistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "insert into openordershistory (id, Symbol, OpenTime, OpenPrice, ClosePrice, Shares, Profit, OrderType) values (@id,@Symbol, @OpenTime, @OpenPrice, @ClosePrice, @Shares, @Profit, @OrderType)";
                    command.Parameters.AddWithValue("@id", oData.Id);
                    command.Parameters.AddWithValue("@Symbol", oData.Symbol);
                    command.Parameters.AddWithValue("@OpenTime", oData.Open_Time);
                    command.Parameters.AddWithValue("@OpenPrice", oData.Open_Price);
                    command.Parameters.AddWithValue("@ClosePrice", oData.Close_Price);
                    command.Parameters.AddWithValue("@Shares", oData.Shares);
                    command.Parameters.AddWithValue("@Profit", oData.Profit);
                    command.Parameters.AddWithValue("@OrderType", oData.Type);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public ObservableCollection<OrderData> getOpenOrderRecordsAll()
        {
            checkExistTable("openordershistory");

            ObservableCollection<OrderData> output = new ObservableCollection<OrderData>();
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from openordershistory";
                    SQLiteDataReader oReader = command.ExecuteReader();
                    while (oReader.Read())
                    {
                        OrderData od = new OrderData();
                        od.Id = oReader.GetInt32(0);
                        od.Symbol = oReader.GetString(1);
                        od.Open_Time = oReader.GetDateTime(2);
                        od.Open_Price = oReader.GetDouble(3);
                        od.Close_Price = oReader.GetDouble(4);
                        od.Shares = oReader.GetInt32(5);
                        od.Profit = oReader.GetDouble(6);
                        od.Type = oReader.GetString(7);
                        output.Add(od);
                    }
                }
                return output;
            }
        }

        public void removeOpenOrderRecord(OrderData oData)
        {
            checkExistTable("openordershistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "delete from openordershistory where id = @id";
                    command.Parameters.AddWithValue("@id", oData.Id);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

        public void updateSummaryRecord(SummaryData oData, string Key, string Value)
        {
            checkExistSummaryTable("summaryhistory");
            using (var connection = new SQLiteConnection(LoadConnectionString()))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "update summaryhistory set " + Key + "='" + Value + "' where Date='" + oData.Date.ToString("yyyy-MM-dd HH:mm:ss") + "'and Symbol='"+ oData.Symbol + "'";
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        Logger.Current.Trace(e.ToString());
                    }
                }
            }
        }

    }
}
