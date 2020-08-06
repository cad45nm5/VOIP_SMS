using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;// for check file exist
using System.Data.SQLite;//for SQLite

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;

namespace SeverTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Createdatabase();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
                    
        }

        private SQLiteConnection sqlite_connect; 
        private SQLiteCommand sqlite_cmd;

        void Createdatabase()
        {   
            if (!File.Exists(Application.StartupPath + @"\database1.db3"))
            {
                SQLiteConnection.CreateFile("database1.db3");
            }


            sqlite_connect = new SQLiteConnection("Data source=database1.db3");//建立資料庫連線            

            sqlite_connect.Open();// Open
            sqlite_cmd = sqlite_connect.CreateCommand();//create command

            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS LINEIOT (NO INTEGER PRIMARY KEY AUTOINCREMENT, LINEUSERID TEXT, MAC TEXT,HIGHVALUE TEXT, LOWVALUE TEXT, ONLINETIME TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS MACVALUE (NO INTEGER PRIMARY KEY AUTOINCREMENT,LINEUSERID TEXT,MAC TEXT,MQTTUSERACCOUNT TEXT,MQTTUUSERPASSWORD TEXT,MQTTTOPIC TEXT,SERVERPORT TEXT,SERVERIP TEXT,WIFIPASSWORD TEXT,WIFISSID TEXT,TEMPERATURE TEXT,HUMIDITY TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_connect.Close();
        }
        private void EMAILVALUE()
        {
            SQLiteConnection e_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            e_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From MACVALUE Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", m_dbConnection);
            SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "''", e_dbConnection);
            DataSet eset = new DataSet();
            eadapter.Fill(eset);
            string ex = eset.Tables[0].Rows[0]["EMAIL"].ToString();
          

            e_dbConnection.Close();
        }
    }
}
