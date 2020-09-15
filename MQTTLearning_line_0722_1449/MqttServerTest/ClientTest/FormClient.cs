
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;//for SQLite
using System.IO;// for check file exist
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MqttServerTest
{
    public partial class lineiot_0424_1630 : Form
    {
        //20200722 14:46 目前程式碼已可自動判斷裝置Y/N準備完成，群發與單筆都會新增至SQLite本機資料庫，再用Timer去0.3秒判斷是否有簡訊訊息需要傳送，並存進雲端資料庫，接收回傳封包改變狀態
        //thread--------------------------------------------------------------------
        public string[,] SendQueue = new string[1000, 6];//waiting to send queue
        public int Que_now = 0;
        public int Que_Sended = 0;
        //public int DB_now = 0;
        public const int DEVICE_MAXIMUM = 500;
        public string[] Device = new string[DEVICE_MAXIMUM];//waiting to send queue

        //thread--------------------------------------------------------------------
        private IMqttClient mqttClient = null;
        private bool isReconnect = true;

        public String aa { get; private set; }
        public string LINEUSERID { get; private set; }
        public string EMAIL { get; private set; }
        public string MAC { get; private set; }
        public string HIGHVALUE { get; private set; }
        public string LOWVALUE { get; private set; }
        public string ONLINETIME { get; private set; }

        private string SENDTIME;

        public SQLiteCommand sqlite_cmd_msg { get; private set; }
        public SQLiteCommand sqlite_cmd_del { get; private set; }
        public string puberr { get; private set; }
        public string pubsuc { get; private set; }
        public string MQTTUSERACCOUNT { get; private set; }
        public string MQTTUUSERPASSWORD { get; private set; }
        public string MQTTTOPIC { get; private set; }
        public string SERVERPORT { get; private set; }
        public string SERVERIP { get; private set; }
        public string WIFIPASSWORD { get; private set; }
        public string WIFISSID { get; private set; }
        public string TEMPERATURE { get; private set; }
        public string HUMIDITY { get; private set; }
        public string SLINEUSERID { get; private set; }
        public string Emailto { get; private set; }
        public string high { get; private set; }
        public string low { get; private set; }
        public static string macc { get; private set; }
        public static SQLiteConnection cosqlite_connect { get; private set; }
        public static SQLiteCommand cosqlite_cmd { get; private set; }
        public SQLiteConnection sqlite_connect_del { get; private set; }
        public SQLiteConnection sqlite_connect_msg { get; private set; }
        public string PHONE { get; private set; }
        public string MSGVALUE { get; private set; }
        public string macAddresses { get; private set; }
        public string TRANSMITMODE { get; private set; }
        public int pv { get; private set; }
        public int maccount { get; private set; }
        public int account { get; private set; }
        public string ISSUCCES { get; private set; }
        public string MSG_LOG { get; private set; }
        public string tt { get; private set; }
        //txtReceiveMessage.AppendText($"test{Environment.NewLine}");
        const string dbHost = "119.8.101.21";//資料庫位址
        const string dbUser = "1234";//資料庫使用者帳號
        const string dbPass = "1234";//資料庫使用者密碼
        const string dbName = "sms";//資料庫名稱
        const string connStr = "server=" + dbHost + ";uid=" + dbUser + ";pwd=" + dbPass + ";database=" + dbName;

        public lineiot_0424_1630()
        {
            InitializeComponent();
            Createdatabase();
            account = 0;
        }
        void DeviceCheck()
        {






            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlCommand command = conn.CreateCommand();
            conn.Open();
            Thread.Sleep(10);
            command.CommandText = "SELECT COUNT(*) FROM device";
            int NUM_DEVICE = Convert.ToInt32(command.ExecuteScalar());

            for (int i = 0; i < DEVICE_MAXIMUM; i++)
            {
                Device[i] = "";
            }

            for (int i = 0; i <= DEVICE_MAXIMUM - 1; i++)
            {
                command.CommandText = "SELECT mac FROM device WHERE active_status= 'Y' and device_id = " + i.ToString();//Is MAC avalible?
                command.ExecuteNonQuery();
                Device[i] = Convert.ToString(command.ExecuteScalar());

                //txtReceiveMessage.AppendText(Device[i] + Environment.NewLine);

            }



            conn.Close();

        }



        void SendFromQueue()
        {
            string MSG, SEND_TIME, Que_No, STATUS, PHONE_NUMBER, Rec, TRANSMODE, ORDERID;
            string Packet;
            string MESSAGE = "";
            PHONE_NUMBER = "";
            Rec = "";
            TRANSMODE = "";
            ORDERID = "";

            Thread.Sleep(100);

            SQLiteConnection loadConnection = new SQLiteConnection("Data Source = database1.db3");
            loadConnection.Open();

            SQLiteDataAdapter hadapter = new SQLiteDataAdapter("Select * From RECORD Where STATUS='1'", loadConnection);

            DataSet loadset = new DataSet();
            hadapter.Fill(loadset);
            loadConnection.Close(); ;
            for (int i = 0; i < loadset.Tables[0].Rows.Count; i++)
            {

                SEND_TIME = loadset.Tables[0].Rows[i]["SENDTIME"].ToString();
                MSG = loadset.Tables[0].Rows[i]["MESSAGE"].ToString();
                PHONE_NUMBER = loadset.Tables[0].Rows[i]["PHONE"].ToString();
                Rec = loadset.Tables[0].Rows[i]["ID"].ToString();
                Que_No = loadset.Tables[0].Rows[i]["NO"].ToString();
                STATUS = loadset.Tables[0].Rows[i]["STATUS"].ToString();
                TRANSMODE = loadset.Tables[0].Rows[i]["TRANSMITMODE"].ToString();
                ORDERID = loadset.Tables[0].Rows[i]["ORDER_ID"].ToString();


                var TIME_CONV = DateTime.Parse(SEND_TIME);
                int TIME_CMOP = DateTime.Compare(TIME_CONV, DateTime.Now);

                if (TIME_CMOP <= 0)
                {

                    //-----
                    //------------------------------
                    string MAC_Ready = "";
                    DeviceCheck();
                    //Check which Device is availible-----------------------------------
                    for (int k = 0; k < DEVICE_MAXIMUM; k++)
                    {

                        if (Device[k] != "" && Device[k] != null)
                        {
                            MAC_Ready = Device[k];
                            break;

                        }

                    }
                    //Check which Device is availible-----------------------------------

                    //variable anoucement------------------------------------------------
                    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

                    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    //Select availible MAC address-------------------------------------- 




                    if (MAC_Ready != "" && PHONE_NUMBER != "" && MAC_Ready != null && PHONE_NUMBER != null)
                    {

                        sqlite_connect = new SQLiteConnection("Data source=database1.db3");//建立資料庫連線
                        sqlite_connect.Open(); //Open
                        sqlite_cmd = sqlite_connect.CreateCommand();//create command
                        SQLiteCommand cmd = new SQLiteCommand(sqlite_connect);

                        sqlite_cmd.CommandText = "UPDATE RECORD SET STATUS='" + "2" + "' WHERE NO='" + Que_No + "'";
                        sqlite_cmd.ExecuteNonQuery();
                        sqlite_connect.Close();
                        Thread.Sleep(50);
                        //Pack the sending packet-------------------------------------------
                        UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        Packet = "{\"ID\":\"" + Rec + "\",\"ORDER_ID\":\"" + ORDERID + "\",\"TRANSMITMODE\":\"" + TRANSMODE + "\",\"PHONE\":\"" + PHONE_NUMBER + "\",\"MESSAGE\":\"" + MSG + "\",\"SENDTIME\":\"" + UPDATETIME + "\",\"MSG_LOG\":\"" + Que_No + "\"}";

                        //Pack the sending packet-------------------------------------------
                        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        //sending message---------------------------------------------------
                        var message = new MqttApplicationMessageBuilder()
                                   .WithTopic(MAC_Ready)
                                   .WithPayload(Packet)
                                   .WithAtLeastOnceQoS()
                                   .WithRetainFlag(false)
                                   .Build();
                        Task.Run(async () =>
                        {
                            await mqttClient.PublishAsync(message);//MESSAGE改成message
                        });
                        Console.WriteLine(Que_No + "已傳送\n");
                        //sending message---------------------------------------------------
                        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        //Update active status of device---------------------------------------------------

                        MySqlConnection conn = new MySqlConnection(connStr);
                        MySqlCommand command = conn.CreateCommand();
                        conn.Open();
                        command.CommandText = "UPDATE device SET active_status = 'N' WHERE mac = '" + MAC_Ready + "'";
                        command.ExecuteNonQuery();
                        conn.Close();


                    }


                }

            }



        }
























        void LoadInToQueue()
        {

            string MSG, SEND_TIME, Que_No, STATUS, PHONE_NUMBER, Rec, TRANSMODE, ORDERID;




            try
            {

                Thread.Sleep(100);

                SQLiteConnection loadConnection = new SQLiteConnection("Data Source = database1.db3");
                loadConnection.Open();

                SQLiteDataAdapter hadapter = new SQLiteDataAdapter("Select * From RECORD Where STATUS='1'", loadConnection);

                DataSet loadset = new DataSet();
                hadapter.Fill(loadset);
                for (int i = 0; i < loadset.Tables[0].Rows.Count; i++)
                {

                    SEND_TIME = loadset.Tables[0].Rows[i]["SENDTIME"].ToString();
                    MSG = loadset.Tables[0].Rows[i]["MESSAGE"].ToString();
                    PHONE_NUMBER = loadset.Tables[0].Rows[i]["PHONE"].ToString();
                    Rec = loadset.Tables[0].Rows[i]["ID"].ToString();
                    Que_No = loadset.Tables[0].Rows[i]["NO"].ToString();
                    STATUS = loadset.Tables[0].Rows[i]["STATUS"].ToString();
                    TRANSMODE = loadset.Tables[0].Rows[i]["TRANSMITMODE"].ToString();
                    ORDERID = loadset.Tables[0].Rows[i]["ORDER_ID"].ToString();


                    var TIME_CONV = DateTime.Parse(SEND_TIME);
                    int TIME_CMOP = DateTime.Compare(TIME_CONV, DateTime.Now);

                    if (TIME_CMOP <= 0)
                    {
                        SendQueue[Que_now, 0] = MSG;
                        SendQueue[Que_now, 1] = PHONE_NUMBER;
                        SendQueue[Que_now, 2] = Que_No;
                        SendQueue[Que_now, 3] = Rec;
                        SendQueue[Que_now, 4] = TRANSMODE;
                        SendQueue[Que_now, 5] = ORDERID;
                        if (Que_now < 999) Que_now++;
                        else Que_now = 0;
                        //----------------------------------------------------------------------------
                        sqlite_connect = new SQLiteConnection("Data source=database1.db3");//建立資料庫連線
                        sqlite_connect.Open(); //Open
                        sqlite_cmd = sqlite_connect.CreateCommand();//create command
                        SQLiteCommand cmd = new SQLiteCommand(sqlite_connect);


                        sqlite_cmd.CommandText = "UPDATE RECORD SET STATUS='" + "2" + "' WHERE NO='" + Que_No + "'";




                        sqlite_cmd.ExecuteNonQuery();
                        sqlite_connect.Close();
                        //-----------------------------------------------------------------
                    }

                }









                loadConnection.Close(); ;
            }
            catch
            {


            }


        }
        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer2.Enabled = true;
            textBox7.Text = "123";
            txtIp.Text = "119.8.112.176";
            CheckMSG();

        }

        private async void BtnPublish_Click(object sender, EventArgs e)
        {
            await Publish();
        }

        private async void BtnSubscribe_ClickAsync(object sender, EventArgs e)
        {
            await Subscribe();
        }

        private async Task Publish()
        {
            string topic = txtPubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = txtSendMessage.Text.Trim();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
        }

        private async Task Subscribe()
        {
            string topic = txtSubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("订阅主题不能为空！");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show("MQTT客户端尚未连接！");
                return;
            }

            // Subscribe to a topic
            await mqttClient.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(topic)
                .WithAtLeastOnceQoS()
                .Build()
                );

            //2.4.0
            //await mqttClient.SubscribeAsync(new List<TopicFilter> {
            //    new TopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce)
            //});

            Invoke((new Action(() =>
            {
                txtReceiveMessage.AppendText($"已订阅[{topic}]主题{Environment.NewLine}");
            })));
            //txtSubTopic.Enabled = false;
            //btnSubscribe.Enabled = false;
        }

        private async Task ConnectMqttServerAsync()
        {
            // Create a new MQTT client.

            if (mqttClient == null)
            {
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();

                mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;
                mqttClient.Connected += MqttClient_Connected;
                mqttClient.Disconnected += MqttClient_Disconnected;
            }

            //非托管客户端
            try
            {
                ////Create TCP based options using the builder.
                //var options1 = new MqttClientOptionsBuilder()
                //    .WithClientId("client001")
                //    .WithTcpServer("192.168.88.3")
                //    .WithCredentials("bud", "%spencer%")
                //    .WithTls()
                //    .WithCleanSession()
                //    .Build();

                //// Use TCP connection.
                //var options2 = new MqttClientOptionsBuilder()
                //    .WithTcpServer("192.168.88.3", 8222) // Port is optional
                //    .Build();

                //// Use secure TCP connection.
                //var options3 = new MqttClientOptionsBuilder()
                //    .WithTcpServer("192.168.88.3")
                //    .WithTls()
                //    .Build();

                //Create TCP based options using the builder.
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(txtClientId.Text)
                    .WithTcpServer(txtIp.Text, Convert.ToInt32(txtPort.Text))
                    .WithCredentials(txtUsername.Text, txtPsw.Text)
                    //.WithTls()//服务器端没有启用加密协议，这里用tls的会提示协议异常
                    .WithCleanSession()
                    .Build();

                //// For .NET Framwork & netstandard apps:
                //MqttTcpChannel.CustomCertificateValidationCallback = (x509Certificate, x509Chain, sslPolicyErrors, mqttClientTcpOptions) =>
                //{
                //    if (mqttClientTcpOptions.Server == "server_with_revoked_cert")
                //    {
                //        return true;
                //    }

                //    return false;
                //};

                //2.4.0版本的
                //var options0 = new MqttClientTcpOptions
                //{
                //    Server = "127.0.0.1",
                //    ClientId = Guid.NewGuid().ToString().Substring(0, 5),
                //    UserName = "u001",
                //    Password = "p001",
                //    CleanSession = true
                //};

                await mqttClient.ConnectAsync(options);

                await mqttClient.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic("pub")
                .WithAtLeastOnceQoS()
                .Build()
                );
                await mqttClient.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic("pubb")
                .WithAtLeastOnceQoS()
                .Build()
                );
                await mqttClient.SubscribeAsync(new TopicFilterBuilder()
               .WithTopic("pu")
               .WithAtLeastOnceQoS()
               .Build()
               );
                //2.4.0
                //await mqttClient.SubscribeAsync(new List<TopicFilter> {
                //    new TopicFilter(topic, MqttQualityOfServiceLevel.AtMostOnce)
                //});

                Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText($"已订阅[pub]主题{Environment.NewLine}");
                })));
            }
            catch (Exception ex)
            {
                Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText($"连接到MQTT服务器失败！" + Environment.NewLine + ex.Message + Environment.NewLine);
                })));
            }

            //托管客户端
            //try
            //{
            //// Setup and start a managed MQTT client.
            //var options = new ManagedMqttClientOptionsBuilder()
            //    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            //    .WithClientOptions(new MqttClientOptionsBuilder()
            //        .WithClientId("Client_managed")
            //        .WithTcpServer("192.168.88.3", 8223)
            //        .WithTls()
            //        .Build())
            //    .Build();

            //var mqttClient = new MqttFactory().CreateManagedMqttClient();
            //await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("my/topic").Build());
            //await mqttClient.StartAsync(options);
            //}
            //catch (Exception)
            //{

            //}
        }
        //有檔案的寄信
        public static bool CreateMessageWithAttachmentfile(string from, string to, string subject, string body, string file)
        {
            //要把"寄件人"，"收件人"，"主旨"，"內文"寫在上方那句CreateMessageWithAttachment()裡面


            MailMessage message = new MailMessage(
                from,
                to,
                subject,
                body);

            // Create  the file attachment for this email message.
            Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
            // Add time stamp information for the file.
            ContentDisposition disposition = data.ContentDisposition;
            disposition.CreationDate = System.IO.File.GetCreationTime(file);
            disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
            disposition.ReadDate = System.IO.File.GetLastAccessTime(file);
            // Add the file attachment to this email message.
            message.Attachments.Add(data);

            //Send the message.
            /* SmtpClient client = new SmtpClient("mail.rsx.com.tw", 587);
             // Add credentials if the SMTP server requires them.
             client.Credentials = CredentialCache.DefaultNetworkCredentials;
             //這邊要用公用的帳號跟密碼
             client.Credentials = new System.Net.NetworkCredential("linebot001@rsx.com.tw", "linebot123456");
           */
            SmtpClient client = new SmtpClient("mail.rsx.com.tw", 587);
            // Add credentials if the SMTP server requires them.
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
            //這邊要用公用的帳號跟密碼
            client.Credentials = new System.Net.NetworkCredential("linebot-rsx@rsx.com.tw", "linebot123456");
            //Gmial 的 smtp 使用 SSL

            client.EnableSsl = true;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateMessageWithAttachment(): {0}",
                    ex.ToString());
                return false;
            }
            // Display the values in the ContentDisposition for the attachment.
            //ContentDisposition cd = data.ContentDisposition;
            //Console.WriteLine("Content disposition");
            //Console.WriteLine(cd.ToString());
            //Console.WriteLine("File {0}", cd.FileName);
            //Console.WriteLine("Size {0}", cd.Size);
            //Console.WriteLine("Creation {0}", cd.CreationDate);
            //Console.WriteLine("Modification {0}", cd.ModificationDate);
            //Console.WriteLine("Read {0}", cd.ReadDate);
            //Console.WriteLine("Inline {0}", cd.Inline);
            //Console.WriteLine("Parameters: {0}", cd.Parameters.Count);
            //foreach (DictionaryEntry d in cd.Parameters)
            //{
            //    Console.WriteLine("{0} = {1}", d.Key, d.Value);
            //}
            data.Dispose();

            return true;
        }
        //沒檔案的寄信
        public static bool CreateMessageWithAttachment(string from, string to, string subject, string body)
        {
            //要把"寄件人"，"收件人"，"主旨"，"內文"寫在上方那句CreateMessageWithAttachment()裡面


            MailMessage message = new MailMessage(
                from,
                to,
                subject,
                body);


            /*
                        //Send the message.
                        SmtpClient client = new SmtpClient("mail.rsx.com.tw", 587);
                        // Add credentials if the SMTP server requires them.
                        client.Credentials = CredentialCache.DefaultNetworkCredentials;
                        //這邊要用公用的帳號跟密碼
                        client.Credentials = new System.Net.NetworkCredential("linebot001@rsx.com.tw", "123r456s");
             */
            SmtpClient client = new SmtpClient("mail.rsx.com.tw", 587);
            // Add credentials if the SMTP server requires them.
            client.Credentials = CredentialCache.DefaultNetworkCredentials;
            //這邊要用公用的帳號跟密碼
            client.Credentials = new System.Net.NetworkCredential("linebot-rsx@rsx.com.tw", "linebot123456");
            //Gmial 的 smtp 使用 SSL

            client.EnableSsl = true;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateMessageWithAttachment(): {0}",
                    ex.ToString());
                return false;
            }


            return true;
        }
        private void MqttClient_Connected(object sender, EventArgs e)
        {
            Invoke((new Action(() =>
            {
                txtReceiveMessage.Clear();
                txtReceiveMessage.AppendText("已连接到MQTT服务器！" + Environment.NewLine);
            })));
        }

        private void MqttClient_Disconnected(object sender, EventArgs e)
        {
            Invoke((new Action(() =>
            {
                txtReceiveMessage.Clear();
                DateTime curTime = new DateTime();
                curTime = DateTime.UtcNow;
                txtReceiveMessage.AppendText($">> [{curTime.ToLongTimeString()}]");
                txtReceiveMessage.AppendText("已断开MQTT连接！" + Environment.NewLine);

            })));

            //Reconnecting
            if (isReconnect)
            {
                Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText("正在尝试重新连接" + Environment.NewLine);
                    Task.Run(async () => { await ConnectMqttServerAsync(); });
                })));

                var options = new MqttClientOptionsBuilder()
                    .WithClientId(txtClientId.Text)
                    .WithTcpServer(txtIp.Text, Convert.ToInt32(txtPort.Text))
                    .WithCredentials(txtUsername.Text, txtPsw.Text)
                    //.WithTls()
                    .WithCleanSession()
                    .Build();
                Invoke((new Action(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        await mqttClient.ConnectAsync(options);
                    }
                    catch
                    {
                        txtReceiveMessage.AppendText("### RECONNECTING FAILED ###" + Environment.NewLine);
                    }
                })));
            }
            else
            {
                Invoke((new Action(() =>
                {
                    txtReceiveMessage.AppendText("已下线！" + Environment.NewLine);
                    Task.Run(async () => { await ConnectMqttServerAsync(); });
                })));
            }
        }
        private SQLiteConnection sqlite_connect;
        private SQLiteConnection sqlite_connect_high;
        private SQLiteConnection sqlite_connect_low;
        private SQLiteConnection sqlite_connect_mac;
        private SQLiteConnection conn;
        private SQLiteCommand sqlite_cmd;
        private SQLiteCommand sqlite_cmd_high;
        private SQLiteCommand sqlite_cmd_low;
        private SQLiteCommand sqlite_cmd_mac;
        private SQLiteCommand cmdd;


        public class Data
        {
            public string LINEUSERID { get; set; }
            public string MAC { get; set; }
            public string HIGHVALUE { get; set; }
            public string LOWVALUE { get; set; }
            public string EMAIL { get; set; }
            public string ONLINETIME { get; set; }
            public string TRANSMITMODE { get; set; }
            public string STRANSMITMODE { get; set; }
            public string PHONE { get; set; }
            public string MESSAGE { get; set; }
            public string SENDTIME { get; set; }
            public string ISSUCCESS { get; set; }
            public string RECORD_ID { get; set; }
            public string ID { get; set; }
            public string ORDER_ID { get; set; }
            public string STATUS { get; set; }
            public string MSG_LOG { get; set; }
            public string CREATEDATE { get; set; }
            public string UPDATEDATE { get; set; }
            public string SID { get; set; }
            public string READY { get; set; }
        }
        public class MacData
        {
            public string MQTTUSERACCOUNT { get; set; }
            public string MQTTUUSERPASSWORD { get; set; }
            public string MQTTTOPIC { get; set; }
            public string SERVERPORT { get; set; }
            public string SERVERIP { get; set; }
            public string WIFIPASSWORD { get; set; }
            public string WIFISSID { get; set; }
            public string TEMPERATURE { get; set; }
            public string HUMIDITY { get; set; }
            public string UPPERLIMIT { get; set; }
            public string LOWERLIMIT { get; set; }

        }
        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {

            /* Invoke((new Action(() =>
               {
                   txtReceiveMessage.AppendText($">> {"### RECEIVED APPLICATION MESSAGE ###"}{Environment.NewLine}");
               })));
               Invoke((new Action(() =>
               {
                   txtReceiveMessage.AppendText($">> Topic = {e.ApplicationMessage.Topic}{Environment.NewLine}");
               })));
            */
            Invoke((new Action(() =>
            {
                //txtReceiveMessage.AppendText($">> {"簡訊傳送成功"}{Environment.NewLine}");
                txtReceiveMessage.AppendText($">> Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}{Environment.NewLine}");
                aa = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                txtReceiveMessage.AppendText("新訊息:");
                txtReceiveMessage.AppendText(aa);

                if (aa.Substring(2, 2).Equals("ID"))
                {
                    //txtReceiveMessage.AppendText("client傳來:" + aa);

                    if (!IsValidJson(aa))
                        return;
                    Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                    Console.WriteLine("TRANSMITMODE : " + mydata.STRANSMITMODE + ", PHONE : " + mydata.PHONE + ", MESSAGE : " + mydata.MESSAGE + ", SENDTIME : " + mydata.SENDTIME);
                    TRANSMITMODE = mydata.TRANSMITMODE;


                    if (TRANSMITMODE == "A")
                    {

                        TPHONE = mydata.PHONE;
                        sqlite_connect = new SQLiteConnection("Data source=database1.db3");
                        //建立資料庫連線
                        sqlite_connect.Open(); //Open
                        sqlite_cmd = sqlite_connect.CreateCommand();//create command
                        SQLiteCommand recmd = new SQLiteCommand(sqlite_connect);
                        Thread.Sleep(50);
                        UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        sqlite_cmd.CommandText = "INSERT INTO RECORD VALUES (null,'" + mydata.RECORD_ID + "','" + mydata.ID + "','" + mydata.ORDER_ID + "','" + mydata.TRANSMITMODE + "','" + TPHONE + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "1" + "','" + "" + "','" + "" + "','" + mydata.CREATEDATE + "','" + UPDATETIME + "');";
                        sqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                        txtReceiveMessage.AppendText("模式 : A " + "單筆號碼 : " + TPHONE + "已儲存");
                        sqlite_cmd.CommandText = "SELECT NO FROM RECORD ORDER BY NO DESC";
                        sqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                        string MSG_LOG = sqlite_cmd.ExecuteScalar().ToString();
                        sqlite_connect.Close();


                        MySqlConnection conn = new MySqlConnection(connStr);
                        MySqlCommand command = conn.CreateCommand();
                        conn.Open();

                        UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        //command.CommandText = "UPDATE record SET status = '2' WHERE msg_log = '" + mydata.MSG_LOG + "'";
                        command.CommandText = "Insert into record(record_id,id,order_id,transmitmode,phone,message,sendtime,status,msg_log,mac,updatedate) values(null,'" + mydata.ID + "','" + mydata.ORDER_ID + "','" + mydata.TRANSMITMODE + "','" + mydata.PHONE + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "1" + "','" + MSG_LOG + "','" + mydata.MAC + "','" + UPDATETIME + "')";
                        command.ExecuteNonQuery();

                        conn.Close();
                    }
                    if (TRANSMITMODE == "F")
                    {

                        string dbHost = "119.8.101.21";//資料庫位址
                        string dbUser = "1234";//資料庫使用者帳號
                        string dbPass = "1234";//資料庫使用者密碼
                        string dbName = "sms";//資料庫名稱
                        string connStr = "server=" + dbHost + ";uid=" + dbUser + ";pwd=" + dbPass + ";database=" + dbName;
                        MySqlConnection conn = new MySqlConnection(connStr);
                        MySqlCommand command = conn.CreateCommand();
                        conn.Open();
                        Thread.Sleep(50);
                        TPHONE = mydata.PHONE;
                        string[] strArr = TPHONE.Split(',');
                        sqlite_connect = new SQLiteConnection("Data source=database1.db3");
                        //建立資料庫連線
                        sqlite_connect.Open(); //Open
                        sqlite_cmd = sqlite_connect.CreateCommand();//create command
                        SQLiteCommand recmd = new SQLiteCommand(sqlite_connect);
                        UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string result = string.Empty;
                        int a = Int32.Parse(strArr.Length.ToString());

                        for (int i = 0; i < a; i++)
                        {
                            Thread.Sleep(50);
                            Console.WriteLine(strArr[i]);
                            sqlite_cmd.CommandText = "INSERT INTO RECORD VALUES (null,'" + mydata.RECORD_ID + "','" + mydata.ID + "','" + mydata.ORDER_ID + "','" + mydata.TRANSMITMODE + "','" + strArr[i] + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "1" + "','" + "" + "','" + mydata.MAC + "','" + mydata.CREATEDATE + "','" + UPDATETIME + "');";
                            sqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                            txtReceiveMessage.AppendText("模式 : F " + "群發號碼 : " + result + "已儲存");

                            //sqlite_cmd.CommandText = "SELECT * FROM RECORD ORDER BY NO ";
                            // sqlite_cmd.ExecuteNonQuery();//using behind every write cmd

                            sqlite_cmd.CommandText = "SELECT NO FROM RECORD ORDER BY NO DESC";
                            sqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                            string MSG_LOG = sqlite_cmd.ExecuteScalar().ToString();

                            UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            //command.CommandText = "UPDATE record SET status = '2' WHERE msg_log = '" + mydata.MSG_LOG + "'";
                            command.CommandText = "Insert into record(record_id,id,order_id,transmitmode,phone,message,sendtime,status,msg_log,mac,updatedate) values(null,'" + mydata.ID + "','" + mydata.ORDER_ID + "','" + mydata.TRANSMITMODE + "','" + strArr[i] + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "1" + "','" + MSG_LOG + "','" + mydata.MAC + "','" + UPDATETIME + "')";
                            command.ExecuteNonQuery();
                        }

                        sqlite_connect.Close();





                        conn.Close();
                    }

                }


                if (aa.Substring(2, 3).Equals("SID"))
                {
                    if (!IsValidJson(aa))
                        return;
                    Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                    ISSUCCES = mydata.ISSUCCESS;
                    if (ISSUCCES == "0")
                    {

                        txtReceiveMessage.AppendText($"{Environment.NewLine}簡訊傳送失敗!{Environment.NewLine}");

                    }
                    else if (ISSUCCES == "1")
                    {

                        txtReceiveMessage.AppendText($"{Environment.NewLine}簡訊傳送成功!{Environment.NewLine}");


                        MySqlConnection conn = new MySqlConnection(connStr);
                        MySqlCommand command = conn.CreateCommand();
                        conn.Open();

                        UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        command.CommandText = "UPDATE record SET status = '2' WHERE msg_log = '" + mydata.MSG_LOG + "'";
                        //command.CommandText = "Insert into record(record_id,id,order_id,transmitmode,phone,message,sendtime,status,msg_log,mac,updatedate) values(null,'" + mydata.SID + "','" + mydata.ORDER_ID + "','" + mydata.STRANSMITMODE + "','" + mydata.PHONE + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "2" + "','" + mydata.MSG_LOG + "','" + mydata.MAC + "','" + UPDATETIME + "')";
                        command.ExecuteNonQuery();
                        command.CommandText = "UPDATE record SET mac = '" + mydata.MAC + "' WHERE msg_log = '" + mydata.MSG_LOG + "'";
                        command.ExecuteNonQuery();
                        command.CommandText = "UPDATE record SET updatedate = '" + UPDATETIME + "' WHERE msg_log = '" + mydata.MSG_LOG + "'";
                        command.ExecuteNonQuery();
                        // bool insert = true; 
                        /*  while (insert)
                          {

                              command.CommandText = "SELECT COUNT(*) FROM record WHERE msg_log = '" + mydata.MSG_LOG + "'";
                              int N_comp = Convert.ToInt32(command.ExecuteScalar());
                              if (N_comp == 0) insert = true;
                              else insert = false;
                          }*/
                        // command.CommandText = "Insert into record(record_id,id,order_id,transmitmode,phone,message,sendtime,status,msg_log,mac,updatedate) values(null,'" + mydata.SID + "','" + mydata.ORDER_ID + "','" + mydata.STRANSMITMODE + "','" + mydata.PHONE + "','" + mydata.MESSAGE + "','" + mydata.SENDTIME + "','" + "2" + "','" + mydata.MSG_LOG + "','" + mydata.MAC + "','" + UPDATETIME + "')";


                        conn.Close();
                    }

                }

                if ((aa.Substring(2, 5).Equals("READY")))
                {

                    if (!IsValidJson(aa))
                        return;
                    Data mydata = JsonConvert.DeserializeObject<Data>(aa);

                    MySqlConnection conn = new MySqlConnection(connStr);
                    MySqlCommand command = conn.CreateCommand();
                    conn.Open();

                    UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    command.CommandText = "UPDATE device SET active_status = 'Y' WHERE device.mac ='" + mydata.READY + "'";
                    command.ExecuteNonQuery();
                    command.ExecuteReader();

                    conn.Close();
                    txtReceiveMessage.AppendText($"{ Environment.NewLine}" + UPDATETIME + "已更新裝置:" + mydata.READY);

                }
                /*if (aa.Substring(0, 6).Equals("UserId"))
                {
                    txtReceiveMessage.AppendText(aa);

                    Task.Run(async () => {
                        await Publishline();
                    });
                }*/
                /*   if (aa.Length >= 10)
                   {
                       if (aa.Substring(2, 10).Equals("LINEUSERID"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                           LINEUSERID = mydata.LINEUSERID;
                           MAC = mydata.MAC;
                           txtReceiveMessage.AppendText("LINEUSERID" + LINEUSERID + "MAC" + MAC);
                           //MessageBox.Show("LINEUSERID" + LINEUSERID + "MAC" + MAC);

                           insertPayload();
                       }
                       else if (aa.Substring(2, 9).Equals("HIGHVALUE"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                           LINEUSERID = mydata.LINEUSERID;
                           MAC = mydata.MAC;
                           HIGHVALUE = mydata.HIGHVALUE;
                           txtReceiveMessage.AppendText("LINEUSERID:" + LINEUSERID + ",MAC:" + MAC + ",HIGHVALUE:" + HIGHVALUE);
                           HIGHVALUEPayload();
                       }
                       else if (aa.Substring(2, 8).Equals("LOWVALUE"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                           LINEUSERID = mydata.LINEUSERID;
                           MAC = mydata.MAC;
                           LOWVALUE = mydata.LOWVALUE;
                           txtReceiveMessage.AppendText("LINEUSERID:" + LINEUSERID + ",MAC:" + MAC + ",LOWVALUE:" + LOWVALUE);
                           LOWVALUEPayload();
                       }
                       else if (aa.Substring(2, 3).Equals("MAC"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                           LINEUSERID = mydata.LINEUSERID;
                           MAC = mydata.MAC;
                           txtReceiveMessage.AppendText("SELECTLINEUSERID:" + LINEUSERID + ",MAC:" + MAC);
                           write2csv();
                           EMAILVALUE();

                           CreateMessageWithAttachmentfile("linebot-rsx@rsx.com.tw", Emailto, MAC + " iot檔案", "您已查詢成功!\n裝置名稱:" + MAC + "\n檔案如附件\n謝謝您!", MAC + ".csv");

                       }
                       else if (aa.Substring(2, 11).Equals("TEMPERATURE"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           MacData macdata = JsonConvert.DeserializeObject<MacData>(aa);
                           MQTTUSERACCOUNT = macdata.MQTTUSERACCOUNT;
                           MQTTUUSERPASSWORD = macdata.MQTTUUSERPASSWORD;
                           MQTTTOPIC = macdata.MQTTTOPIC;
                           SERVERPORT = macdata.SERVERPORT;
                           SERVERIP = macdata.SERVERIP;
                           WIFIPASSWORD = macdata.WIFIPASSWORD;
                           WIFISSID = macdata.WIFISSID;
                           TEMPERATURE = macdata.TEMPERATURE;
                           HUMIDITY = macdata.HUMIDITY;
                           MACVALUEPayload();

                           HMV();
                           LMV();
                           if (Convert.ToDouble(TEMPERATURE) > Convert.ToDouble(high))
                           {
                               EMAILMAC();

                               CreateMessageWithAttachment("linebot-rsx@rsx.com.tw", Emailto, "警告提醒!By IoT智能語音小助手", "您的裝置 " + MAC + "\n在" + ONLINETIME + "\n數值為" + TEMPERATURE + "\n超過上限值 : " + high + "!");

                           }
                           else if (Convert.ToDouble(TEMPERATURE) < Convert.ToDouble(low))
                           {
                               EMAILMAC();
                               CreateMessageWithAttachment("linebot-rsx@rsx.com.tw", Emailto, "警告提醒!By IoT智能語音小助手", "您的裝置 " + MAC + "\n在" + ONLINETIME + "\n數值為" + TEMPERATURE + "\n超過下限值 : " + low + "!");

                           }


                       }
                       else if (aa.Substring(2, 5).Equals("EMAIL"))
                       {
                           txtReceiveMessage.AppendText(aa);

                           if (!IsValidJson(aa))
                               return;
                           Data mydata = JsonConvert.DeserializeObject<Data>(aa);
                           LINEUSERID = mydata.LINEUSERID;
                           EMAIL = mydata.EMAIL;
                           EMAILPayload();
                           SQLiteConnection c_dbConnection = new SQLiteConnection("Data Source = database1.db3");
                           c_dbConnection.Open();
                           //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
                           SQLiteDataAdapter cadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID = " + "'" + LINEUSERID + "' ORDER BY NO DESC;", c_dbConnection);
                           //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
                           DataSet cset = new DataSet();
                           cadapter.Fill(cset);
                           MAC = cset.Tables[0].Rows[0]["MAC"].ToString();

                           CreateMessageWithAttachment("linebot-rsx@rsx.com.tw", EMAIL, "IoT智能語音小助手", "您已註冊成功!\n帳號:" + MAC + "\n密碼:" + MAC + "\n" + "\n謝謝您!");
                       }


                   }
               */
            })));
            /* 
               Invoke((new Action(() =>
               {
                   txtReceiveMessage.AppendText($">> QoS = {e.ApplicationMessage.QualityOfServiceLevel}{Environment.NewLine}");
               })));
               Invoke((new Action(() =>
               {
                   txtReceiveMessage.AppendText($">> Retain = {e.ApplicationMessage.Retain}{Environment.NewLine}");
               })));
            */
            /* if (aa!= null)
             {
                 string ChannelAccessToken = "OzoeHFeqM23uMJoNP1LYBIALAaWlS5+6VIlcwAjOoqmbWVxvjtz4ThDd/c4iYKR2PCDOO3iktsvKd+4EI0FNFONGQ7LAhDzg8LnrDE3AfR2p96op3QQeupSYSfferNXpGSl86nkiXFBpOOHV9T2QtwdB04t89/1O/w1cDnyilFU=";
                 isRock.LineBot.Utility.PushMessage("U1d8f66d1f8efa6d24fc2e218fd0a9172", aa.ToString(), ChannelAccessToken);
             }*/

        }
        void Createdatabase()
        {
            if (!File.Exists(Application.StartupPath + @"\database1.db3"))
            {
                SQLiteConnection.CreateFile("database1.db3");
            }


            sqlite_connect = new SQLiteConnection("Data source=database1.db3");//建立資料庫連線            

            sqlite_connect.Open();// Open
            sqlite_cmd = sqlite_connect.CreateCommand();//create command

            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS RECORD (NO INTEGER PRIMARY KEY AUTOINCREMENT,RECORD_ID TEXT,ID TEXT,ORDER_ID TEXT,TRANSMITMODE TEXT,PHONE TEXT,MESSAGE TEXT,SENDTIME TEXT,STATUS TEXT,MSG_LOG TEXT,MAC TEXT,CREATEDATE TEXT,UPDATEDATE TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS LINEIOT (NO INTEGER PRIMARY KEY AUTOINCREMENT,LINEUSERID TEXT, MAC TEXT,HIGHVALUE TEXT, LOWVALUE TEXT, ONLINETIME TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS MACVALUE (NO INTEGER PRIMARY KEY AUTOINCREMENT,LINEUSERID TEXT,MAC TEXT,MQTTUSERACCOUNT TEXT,MQTTUUSERPASSWORD TEXT,MQTTTOPIC TEXT,SERVERPORT TEXT,SERVERIP TEXT,WIFIPASSWORD TEXT,WIFISSID TEXT,TEMPERATURE TEXT,HUMIDITY TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_cmd.CommandText = @"CREATE TABLE IF NOT EXISTS MSG (NO INTEGER PRIMARY KEY AUTOINCREMENT,TRANSMITMODE TEXT,MACMSG TEXT,PHONE TEXT,MSGVALUE TEXT,SENDTIME TEXT,SUC TEXT)";
            sqlite_cmd.ExecuteNonQuery(); //using behind every write cmd
            sqlite_connect.Close();
        }

        void insertPayload()
        {
            if (LINEUSERID == "" || MAC == "")
            {
                return;
            }
            else
            {
                sqlite_connect = new SQLiteConnection("Data source=database1.db3");
                //建立資料庫連線
                sqlite_connect.Open(); //Open
                ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sqlite_cmd = sqlite_connect.CreateCommand();//create command
                SQLiteCommand cmd = new SQLiteCommand(sqlite_connect);
                cmd.CommandText = "SELECT * FROM LINEIOT WHERE LINEIOT.LINEUSERID = " + "'" + LINEUSERID + "'" + " and LINEIOT.MAC = " + "'" + MAC + "'";
                int LINEUSERID_count = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.CommandText = "SELECT * FROM LINEIOT WHERE LINEIOT.MAC = " + "'" + MAC + "'";
                int MAC_count = Convert.ToInt32(cmd.ExecuteScalar());
                if (HIGHVALUE == "" || HIGHVALUE == null)
                {
                    HIGHVALUE = "0";
                }
                if (LOWVALUE == "" || LOWVALUE == null)
                {
                    LOWVALUE = "0";
                }
                if (EMAIL == "" || EMAIL == null)
                {
                    EMAIL = "linebot-rsx@rsx.com.tw";
                }


                if (LINEUSERID_count > 0)
                {
                    txtReceiveMessage.AppendText($">> {LINEUSERID + "使用者:已註冊此序號:" + MAC}{Environment.NewLine}");
                    puberr = "eor:" + LINEUSERID + "使用者:已註冊此序號:" + MAC;
                    // CreateMessageWithAttachment("rsx-linebot@rsx.com.tw", EMAIL, "iot測試", "您註冊失敗!\n您已註冊此序號:" + MAC + "\n請再輸入其他有效序號\n謝謝您!");
                    Task.Run(async () =>
                    {
                        await Publisherr();
                    });
                    return;
                }
                else if (MAC_count > 0)
                {
                    txtReceiveMessage.AppendText($">> {LINEUSERID + "使用者:此序號" + MAC + "已被其他使用者註冊"}{Environment.NewLine}");
                    puberr = "err:" + LINEUSERID + "使用者:此序號" + MAC + "已被其他使用者註冊";
                    // CreateMessageWithAttachment("rsx-linebot@rsx.com.tw", EMAIL, "iot測試", "您註冊失敗!\n此序號:" + MAC + "已被其他使用者註冊\n請再輸入其他有效序號\n謝謝您!");
                    Task.Run(async () =>
                    {
                        await Publisherr();
                    });
                    return;
                }
                else
                {

                    LOWVALUE = "0";
                    HIGHVALUE = "65535";
                    sqlite_cmd.CommandText = "INSERT INTO LINEIOT VALUES (null, '" + LINEUSERID + "', '" + MAC + "', '" + HIGHVALUE + "', '" + LOWVALUE + "', '" + "" + "', '" + ONLINETIME + "');";

                    sqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                    sqlite_connect.Close();

                    txtReceiveMessage.AppendText($">> {LINEUSERID + "使用者:此序號" + MAC + "註冊成功"}{Environment.NewLine}");
                    pubsuc = "succeeded:" + LINEUSERID + "使用者:此序號" + MAC + "註冊成功";
                    // CreateMessageWithAttachment("rsx-linebot@rsx.com.tw", EMAIL, "iot測試", "您已註冊成功!\n裝置帳號:" + MAC + "\n裝置密碼:" + MAC + "\n謝謝您!");

                    Task.Run(async () =>
                    {
                        await Publishline();
                    });
                    return;
                }


            }


        }
        void MACVALUEPayload()
        {
            if (MQTTUUSERPASSWORD == "" || MQTTUSERACCOUNT == "")
            {
                return;
            }
            else
            {
                sqlite_connect_mac = new SQLiteConnection("Data source=database1.db3");
                //建立資料庫連線
                sqlite_connect_mac.Open(); //Open
                ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sqlite_cmd_mac = sqlite_connect_mac.CreateCommand();//create command
                SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_mac);
                if (HIGHVALUE == "" || HIGHVALUE == null)
                {
                    HIGHVALUE = "0";
                }

                MAC = MQTTUSERACCOUNT;
                sqlite_cmd_mac.CommandText = "INSERT INTO MACVALUE VALUES (null, '" + "" + "', '" + MAC + "', '" + MQTTUSERACCOUNT + "', '" + MQTTUUSERPASSWORD + "', '" + MQTTTOPIC + "', '" + SERVERPORT + "', '" + SERVERIP + "', '" + WIFIPASSWORD + "', '" + WIFISSID + "', '" + TEMPERATURE + "', '" + HUMIDITY + "', '" + ONLINETIME + "');";
                sqlite_cmd_mac.ExecuteNonQuery();
                sqlite_connect_mac.Close();
                return;
            }


        }
        void HIGHVALUEPayload()
        {
            if (LINEUSERID == "" || MAC == "")
            {
                return;
            }
            else
            {
                sqlite_connect_high = new SQLiteConnection("Data source=database1.db3");
                //建立資料庫連線
                sqlite_connect_high.Open(); //Open
                ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sqlite_cmd_high = sqlite_connect_high.CreateCommand();//create command
                SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_high);
                if (HIGHVALUE == "" || HIGHVALUE == null)
                {
                    HIGHVALUE = "0";
                }

                sqlite_cmd_high.CommandText = "UPDATE LINEIOT SET HIGHVALUE='" + HIGHVALUE + "' WHERE LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'";
                sqlite_cmd_high.ExecuteNonQuery();
                sqlite_connect_high.Close();
                return;
            }


        }
        void LOWVALUEPayload()
        {
            if (LINEUSERID == "" || MAC == "")
            {
                return;
            }
            else
            {
                sqlite_connect_low = new SQLiteConnection("Data source=database1.db3");
                //建立資料庫連線
                sqlite_connect_low.Open(); //Open
                ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                sqlite_cmd_low = sqlite_connect_low.CreateCommand();//create command
                SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_low);

                if (LOWVALUE == "" || LOWVALUE == null)
                {
                    LOWVALUE = "0";
                }

                sqlite_cmd_low.CommandText = "UPDATE LINEIOT SET LOWVALUE='" + LOWVALUE + "' WHERE LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'";
                sqlite_cmd_low.ExecuteNonQuery();
                sqlite_connect_low.Close();
                return;

            }


        }
        void EMAILPayload()
        {
            if (LINEUSERID == "" || MAC == "")
            {
                return;
            }
            else
            {
                conn = new SQLiteConnection("Data source=database1.db3");
                //建立資料庫連線
                conn.Open(); //Open
                cmdd = conn.CreateCommand();//create command
                SQLiteCommand cmd = new SQLiteCommand(conn);
                if (EMAIL == "" || EMAIL == null)
                {
                    EMAIL = "";
                }

                cmdd.CommandText = "UPDATE LINEIOT SET EMAIL='" + EMAIL + "' WHERE LINEUSERID='" + LINEUSERID + "'";
                cmdd.ExecuteNonQuery();
                conn.Close();
                return;
            }


        }
        private void HMV()
        {
            SQLiteConnection h_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            h_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            SQLiteDataAdapter hadapter = new SQLiteDataAdapter("Select * From LINEIOT Where MAC=" + "'" + MAC + "'", h_dbConnection);
            //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
            DataSet hset = new DataSet();
            hadapter.Fill(hset);
            string hmx = hset.Tables[0].Rows[0]["HIGHVALUE"].ToString();
            string otime = hset.Tables[0].Rows[0]["ONLINETIME"].ToString();
            ONLINETIME = otime;
            high = hmx;
            //MessageBox.Show(hx);
            h_dbConnection.Close();
        }
        private void LMV()
        {
            SQLiteConnection l_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            l_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            SQLiteDataAdapter ladapter = new SQLiteDataAdapter("Select * From LINEIOT Where MAC=" + "'" + MAC + "'", l_dbConnection);
            //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
            DataSet lset = new DataSet();
            ladapter.Fill(lset);
            string lmx = lset.Tables[0].Rows[0]["LOWVALUE"].ToString();
            low = lmx;
            //MessageBox.Show(hx);
            l_dbConnection.Close();
        }
        private void HV()
        {
            SQLiteConnection h_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            h_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            SQLiteDataAdapter hadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", h_dbConnection);
            //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
            DataSet hset = new DataSet();
            hadapter.Fill(hset);
            string hx = hset.Tables[0].Rows[0]["HIGHVALUE"].ToString();
            high = hx;
            //MessageBox.Show(hx);
            h_dbConnection.Close();
        }
        private void LV()
        {
            SQLiteConnection l_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            l_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            SQLiteDataAdapter ladapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", l_dbConnection);
            //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
            DataSet lset = new DataSet();
            ladapter.Fill(lset);
            string lx = lset.Tables[0].Rows[0]["LOWVALUE"].ToString();
            low = lx;
            //MessageBox.Show(hx);
            l_dbConnection.Close();
        }
        private void EMAILMAC()
        {
            SQLiteConnection e_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            e_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From MACVALUE Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", m_dbConnection);
            SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where MAC=" + "'" + MAC + "'", e_dbConnection);
            DataSet eset = new DataSet();
            eadapter.Fill(eset);
            string ex = eset.Tables[0].Rows[0]["EMAIL"].ToString();
            Emailto = ex;

            e_dbConnection.Close();
        }
        private void EMAILVALUE()
        {
            SQLiteConnection e_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            e_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From MACVALUE Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", m_dbConnection);
            SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
            DataSet eset = new DataSet();
            eadapter.Fill(eset);
            string ex = eset.Tables[0].Rows[0]["EMAIL"].ToString();
            Emailto = ex;

            e_dbConnection.Close();
        }

        private void write2csv()
        {

            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source = database1.db3");
            m_dbConnection.Open();
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
            //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From MACVALUE Where LINEUSERID='" + LINEUSERID + "' and " + "MAC=" + "'" + MAC + "'", m_dbConnection);
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From MACVALUE Where MAC=" + "'" + MAC + "'", m_dbConnection);
            DataSet set = new DataSet();
            adapter.Fill(set);
            DataTable table = set.Tables[0];
            string filepath = Directory.GetCurrentDirectory();
            Save2Csv_rsx(table, filepath);

            m_dbConnection.Close();

        }
        public void Save2Csv_rsx(DataTable dt, string filePath)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            try
            {
                HV();
                LV();
                fs = new FileStream(MAC + ".csv", FileMode.Create, FileAccess.Write); //csv file name
                sw = new StreamWriter(fs, Encoding.Default);
                var data = string.Empty;
                for (var i = 10; i < dt.Columns.Count; i++)
                {


                    data += dt.Columns[i].ColumnName;
                    if (i == 10 || i == 11)
                    {
                        data += ",status";
                    }
                    if (i < dt.Columns.Count - 1)
                    {
                        data += ",";
                    }



                }

                sw.WriteLine(data);
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    data = string.Empty;
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        if (j < 10)
                            continue;
                        data += dt.Rows[i][j].ToString();
                        // MessageBox.Show(dt.Rows[i][10].ToString() + "i:"+ i + ",j:" + j);
                        if (Convert.ToDouble(dt.Rows[i][10]) < Convert.ToDouble(low))
                        {

                            if (j == 10)
                            {
                                //MessageBox.Show(dt.Rows[i][10].ToString() + "i:" + i + ",j:" + j);
                                //EMAILVALUE();
                                //CreateMessageWithAttachment("rsx-linebot@rsx.com.tw", EMAIL, "警告提醒!By IoT智能語音小助手", "您的裝置 "+ MAC +" 超過下限值 : "+ low +"!");

                                data += ",-,";

                            }
                            else
                            {
                                data += ",,";
                            }
                        }
                        else if (Convert.ToDouble(dt.Rows[i][10]) > Convert.ToDouble(high))
                        {

                            if (j == 10)
                            {
                                //EMAILVALUE();
                                //CreateMessageWithAttachment("rsx-linebot@rsx.com.tw", EMAIL, "警告提醒!By IoT智能語音小助手", "您的裝置 " + MAC + " 超過上限值 : " + high + "!");

                                data += ",+,";
                            }
                            else
                            {
                                data += ",,";
                            }
                        }
                        else
                        {
                            if (j < dt.Columns.Count - 1)
                            {
                                data += ",,";
                            }
                        }

                    }
                    sw.WriteLine(data);
                }
            }
            catch (IOException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            finally
            {
                if (sw != null) sw.Close();
                if (fs != null) fs.Close();
            }

        }
        private async Task Publishline()
        {
            string topic = "pub";

            string inputString = pubsuc.ToString();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
        }
        private async Task Publisherr()
        {
            string topic = "pub";

            string inputString = puberr.ToString();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
        }
        private void btnLogIn_Click(object sender, EventArgs e)
        {
            Console.WriteLine("");
            isReconnect = true;
            Task.Run(async () => { await ConnectMqttServerAsync(); });
            txtReceiveMessage.AppendText($">> {"[" + ONLINETIME + "]" + "已登入"}{Environment.NewLine}");
            Console.WriteLine("");
            Thread.Sleep(1000);
            timer1.Enabled = true;
            timer3.Enabled = true;

        }
        private static IMqttServer mqttServer = null;
        private static List<string> connectedClientId = new List<string>();
        private static int LINEUSERID_count;
        private string UPDATETIME;
        private string TPHONE;


        private void btnLogout_Click(object sender, EventArgs e)
        {
            isReconnect = false;
            Task.Run(async () => { await mqttClient.DisconnectAsync(); });
        }


        private static void MqttServer_ClientConnected(object sender, MQTTnet.Server.MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("");
            var ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            Console.WriteLine($"[{ONLINETIME}] Client ID:[{e.Client.ClientId}]連線成功，Version：{e.Client.ProtocolVersion}");
            connectedClientId.Add(e.Client.ClientId);
            Console.WriteLine("");

        }

        private static void MqttServer_ClientDisconnected(object sender, MQTTnet.Server.MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("");
            connectedClientId.Remove(e.Client.ClientId);
            Console.WriteLine($"Client ID:[{e.Client.ClientId}]失去連線");
            return;
        }

        private static void MqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {

            Console.WriteLine("");
            var ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string recv = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            // Console.WriteLine("### 簡訊傳送成功 ###");
            Console.WriteLine($"[{ONLINETIME}] Client ID:[{e.ClientId}]>> + Topic: = {e.ApplicationMessage.Topic}+/ Message:= {recv}");
            //Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            //Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
            Console.WriteLine();
            if (e.ApplicationMessage.Topic == "slave/json")
            {
                JsonData(recv);
                Console.WriteLine();
                return;
            }
            return;
        }

        private static void MqttNetTrace_TraceMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
            if (e.TraceMessage.Exception != null)
            {
                trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
            }

            Console.WriteLine(trace);
        }

        #region 2.7.5

        private static async Task StartMqttServer_2_7_5()
        {
            if (mqttServer == null)
            {
                // Configure MQTT server.
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithConnectionBacklog(100)
                    .WithDefaultEndpointPort(8222)
                    .WithConnectionValidator(ValidatingMqttClients())
                    ;

                // Start a MQTT server.
                mqttServer = new MqttFactory().CreateMqttServer();
                mqttServer.ApplicationMessageReceived += MqttServer_ApplicationMessageReceived;
                mqttServer.ClientConnected += MqttServer_ClientConnected;
                mqttServer.ClientDisconnected += MqttServer_ClientDisconnected;
                var ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                Task.Run(async () => { await mqttServer.StartAsync(optionsBuilder.Build()); });
                //mqttServer.StartAsync(optionsBuilder.Build());
                Console.WriteLine($"[{ONLINETIME}] MQTT server 連線成功！");


            }
        }

        private static async Task EndMqttServer_2_7_5()
        {
            if (mqttServer != null)
            {
                await mqttServer.StopAsync();
            }
            else
            {
                Console.WriteLine("mqttserver=null");
            }
        }

        private static Action<MqttConnectionValidatorContext> ValidatingMqttClients()
        {
            // Setup client validator.    
            var options = new MqttServerOptions();
            options.ConnectionValidator = c =>
            {
                /* SQLiteConnection co_dbConnection = new SQLiteConnection("Data Source = database1.db3");
                 co_dbConnection.Open();
                 //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
                 SQLiteDataAdapter coadapter = new SQLiteDataAdapter("Select Count(*) AS Count From LINEIOT", co_dbConnection);
                 //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
                 DataSet coset = new DataSet();
                 coadapter.Fill(coset);

                 LINEUSERID_count = Convert.ToInt32(coset.Tables[0].Rows[0]["Count"].ToString());
                 if(LINEUSERID_count != 0)
                 {
                     Console.WriteLine($"新增筆數{LINEUSERID_count}");
                 }
                 else
                 {
                     return;
                 }*/
                Dictionary<string, string> c_u = new Dictionary<string, string>();
                c_u.Add("ABCDEFGHIJK", "ABCDEFGHIJK");
                c_u.Add("12345654418", "12345654418");
                c_u.Add("8DAB2286F24", "8DAB2286F24");
                c_u.Add("12345604232", "12345604232");
                c_u.Add("12345654419", "12345654419");
                c_u.Add("1CF0A4286F24", "1CF0A4286F24");
                c_u.Add("7CE9B2286F24", "7CE9B2286F24");
                c_u.Add("54E9B3286F24", "54E9B3286F24");

                c_u.Add("ECDB9512CFA4", "ECDB9512CFA4");
                c_u.Add("E44A9612CFA4", "E44A9612CFA4");
                c_u.Add("2C9F9712CFA4", "2C9F9712CFA4");
                c_u.Add("4CF0A4286F24", "4CF0A4286F24");
                c_u.Add("CCE69612CFA4", "CCE69612CFA4");
                c_u.Add("78C99612CFA4", "78C99612CFA4");
                c_u.Add("ACC125C40A24", "ACC125C40A24");//-------------------------------------tmep
                c_u.Add("744E9612CFA4", "744E9612CFA4");
                c_u.Add("A8859612CFA4", "A8859612CFA4");
                c_u.Add("14249612CFA4", "14249612CFA4");
                c_u.Add("F4399612CFA4", "F4399612CFA4");
                c_u.Add("A85B9612CFA4", "A85B9612CFA4");
                c_u.Add("247A9612CFA4", "247A9612CFA4");
                c_u.Add("14C29612CFA4", "14C29612CFA4");
                c_u.Add("ECA65F286F24", "ECA65F286F24");

                //




                c_u.Add("0C965F286F24", "0C965F286F24");
                c_u.Add("A02060286F24", "A02060286F24");
                c_u.Add("9C6C5F286F24", "9C6C5F286F24");
                c_u.Add("F0B85F286F24", "F0B85F286F24");

                c_u.Add("780761286F24", "780761286F24");
                c_u.Add("149F9712CFA4", "149F9712CFA4");
                c_u.Add("049F9712CFA4", "049F9712CFA4");
                c_u.Add("EC0D61286F24", "EC0D61286F24");
                c_u.Add("880C61286F24", "880C61286F24");


                c_u.Add("7C315F286F24", "7C315F286F24");
                c_u.Add("049B9712CFA4", "049B9712CFA4");
                c_u.Add("98475F286F24", "98475F286F24");
                c_u.Add("B07A5F286F24", "B07A5F286F24");
                /*  for (int i=0; i< LINEUSERID_count; i++)
                 {
                     SQLiteConnection c_dbConnection = new SQLiteConnection("Data Source = database1.db3");
                     c_dbConnection.Open();
                     //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
                     SQLiteDataAdapter cadapter = new SQLiteDataAdapter("Select * From LINEIOT", c_dbConnection);
                     //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
                     DataSet cset = new DataSet();
                     cadapter.Fill(cset);
                     string cmx = cset.Tables[0].Rows[i]["MAC"].ToString();
                     macc = cmx;
                     Console.WriteLine($"已新增帳號[{cmx}] cmx！");
                     c_dbConnection.Close();
                     c_u.Add(cmx, cmx);

                 }*/
                Dictionary<string, string> u_psw = new Dictionary<string, string>();
                u_psw.Add("ABCDEFGHIJK", "ABCDEFGHIJK");
                u_psw.Add("12345654418", "12345654418");
                u_psw.Add("8DAB2286F24", "8DAB2286F24");
                u_psw.Add("12345604232", "12345604232");
                u_psw.Add("12345654419", "12345654419");
                u_psw.Add("1CF0A4286F24", "1CF0A4286F24");
                u_psw.Add("7CE9B2286F24", "7CE9B2286F24");
                u_psw.Add("54E9B3286F24", "54E9B3286F24");

                u_psw.Add("ECDB9512CFA4", "ECDB9512CFA4");
                u_psw.Add("E44A9612CFA4", "E44A9612CFA4");
                u_psw.Add("2C9F9712CFA4", "2C9F9712CFA4");
                u_psw.Add("4CF0A4286F24", "4CF0A4286F24");
                u_psw.Add("CCE69612CFA4", "CCE69612CFA4");
                u_psw.Add("78C99612CFA4", "78C99612CFA4");
                u_psw.Add("744E9612CFA4", "744E9612CFA4");
                u_psw.Add("ACC125C40A24", "ACC125C40A24");//ACC125C40A24-------------------------
                u_psw.Add("A8859612CFA4", "A8859612CFA4");
                u_psw.Add("14249612CFA4", "14249612CFA4");
                u_psw.Add("F4399612CFA4", "F4399612CFA4");
                u_psw.Add("A85B9612CFA4", "A85B9612CFA4");
                u_psw.Add("247A9612CFA4", "247A9612CFA4");
                u_psw.Add("14C29612CFA4", "14C29612CFA4");
                u_psw.Add("ECA65F286F24", "ECA65F286F24");

                u_psw.Add("0C965F286F24", "0C965F286F24");
                u_psw.Add("A02060286F24", "A02060286F24");
                u_psw.Add("9C6C5F286F24", "9C6C5F286F24");
                u_psw.Add("F0B85F286F24", "F0B85F286F24");


                u_psw.Add("780761286F24", "780761286F24");
                u_psw.Add("149F9712CFA4", "149F9712CFA4");
                u_psw.Add("049F9712CFA4", "049F9712CFA4");
                u_psw.Add("EC0D61286F24", "EC0D61286F24");
                u_psw.Add("880C61286F24", "880C61286F24");

                u_psw.Add("7C315F286F24", "7C315F286F24");
                u_psw.Add("049B9712CFA4", "049B9712CFA4");
                u_psw.Add("98475F286F24", "98475F286F24");
                u_psw.Add("B07A5F286F24", "B07A5F286F24");
                /* for (int i = 0; i < LINEUSERID_count; i++)
                 {
                     SQLiteConnection c_dbConnection = new SQLiteConnection("Data Source = database1.db3");
                     c_dbConnection.Open();
                     //SQLiteDataAdapter adapter = new SQLiteDataAdapter("Select * From payload", m_dbConnection);
                     SQLiteDataAdapter cadapter = new SQLiteDataAdapter("Select * From LINEIOT", c_dbConnection);
                     //SQLiteDataAdapter eadapter = new SQLiteDataAdapter("Select * From LINEIOT Where LINEUSERID=" + "'" + LINEUSERID + "'", e_dbConnection);
                     DataSet cset = new DataSet();
                     cadapter.Fill(cset);
                     string cmx = cset.Tables[0].Rows[i]["MAC"].ToString();
                     macc = cmx;
                     Console.WriteLine($"已新增密碼[{cmx}] cmx！");
                     c_dbConnection.Close();
                     u_psw.Add(cmx, cmx);
                 }*/

                if (c_u.ContainsKey(c.ClientId) && c_u[c.ClientId] == c.Username)
                {
                    if (u_psw.ContainsKey(c.Username) && u_psw[c.Username] == c.Password)
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                    }
                    else
                    {
                        c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                    }
                }
                else
                {
                    c.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                }


            };
            return options.ConnectionValidator;
        }
        private string GetMacAddress()
        {

            string macAddresses2 = string.Empty;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses2 += nic.GetPhysicalAddress().ToString();
                    break;
                }

            }
            macAddresses = macAddresses2;
            return macAddresses;
        }
        private static void Usingcertificate(ref MqttServerOptions options)
        {
            var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
            options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
            var aes = new System.Security.Cryptography.AesManaged();

        }

        #endregion

        #region Topic

        private static async void Topic_Hello(string msg)
        {
            string topic = "topic/hello";

            //2.4.0版本的
            //var appMsg = new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(inputString), MqttQualityOfServiceLevel.AtMostOnce, false);
            //mqttClient.PublishAsync(appMsg);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithAtLeastOnceQoS()
                .WithRetainFlag()
                .Build();
            await mqttServer.PublishAsync(message);
        }

        private static async void Topic_Host_Control(string msg)
        {
            string topic = "topic/host/control";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(false)
                .Build();
            await mqttServer.PublishAsync(message);
        }

        private static async void Topic_Serialize(string msg)
        {
            string topic = "topic/serialize";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(false)
                .Build();
            await mqttServer.PublishAsync(message);
        }

        /// <summary>
        /// 替指定的clientID订阅指定的内容
        /// </summary>
        /// <param name="topic"></param>
        private static void Subscribe(string topic)
        {
            List<TopicFilter> topicFilter = new List<TopicFilter>();
            topicFilter.Add(new TopicFilterBuilder()
                .WithTopic(topic)
                .WithAtLeastOnceQoS()
                .Build());
            //给"client001"订阅了主题为topicFilter的payload
            mqttServer.SubscribeAsync("client001", topicFilter);
            Console.WriteLine($"Subscribe:[{"client001"}]，Topic：{topic}");
        }

        #endregion

        #region JsonSerialize

        private static void JsonData(string recvPayload)
        {
            AllData data = new AllData();
            data.m_data = (EquipmentDataJson)JsonConvert.DeserializeObject(recvPayload, typeof(EquipmentDataJson));
            Console.Write($"recv: str_test={data.m_data.str_test}, str_arr_test={data.m_data.str_arr_test}");
            Console.Write($"recv: int_test={data.m_data.int_test}, int_arr_test={data.m_data.int_arr_test}");
            Console.WriteLine("");
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(async () => { await StartMqttServer_2_7_5(); });
            txtReceiveMessage.AppendText($">> {ONLINETIME + " MQTT server 連線成功！"}{Environment.NewLine}");
            Console.WriteLine($"[{ONLINETIME}] MQTT server 連線成功！");
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox1.Text == null)
            {
                return;
            }
            sqlite_connect_del = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect_del.Open(); //Open
            ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            sqlite_cmd_del = sqlite_connect_del.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_low);
            MAC = textBox1.Text;
            Console.WriteLine(MAC);
            sqlite_cmd_del.CommandText = "DELETE FROM LINEIOT  WHERE MAC=" + "'" + MAC + "'";
            sqlite_cmd_del.ExecuteNonQuery();
            sqlite_cmd_del.CommandText = "INSERT INTO BLACK (BMAC) VALUES('" + MAC + "');";
            sqlite_cmd_del.ExecuteNonQuery();
            sqlite_connect_del.Close();
            Console.WriteLine(MAC + "已刪除");
            textBox1.Text = "";
            return;
        }

        private void txtClientId_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private async Task PublishA()
        {

            string topic = txtPubTopic.Text.Trim();
            SENDTIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = "{\"ID\":\"A\",\"ORDER_ID\":\"A\",\"TRANSMITMODE\":\"A\",\"PHONE\":\"" + textBox2.Text + "\",\"MESSAGE\":\"" + txtSendMessage.Text + "\",\"SENDTIME\":\"" + SENDTIME + "\"}";

            var message = new MqttApplicationMessageBuilder()
               .WithTopic(topic)
               .WithPayload(inputString)
               .WithAtLeastOnceQoS()
               .WithRetainFlag(true)
               .Build();

            await mqttClient.PublishAsync(message);
            GetMacAddress();
            sqlite_connect_msg = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect_msg.Open(); //Open

            sqlite_cmd_msg = sqlite_connect_msg.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_msg);
            PHONE = textBox2.Text;
            MSGVALUE = SENDTIME + txtSendMessage.Text;
            sqlite_cmd_msg.CommandText = "INSERT INTO MSG VALUES(null, '" + "A" + "', '" + macAddresses + "', '" + PHONE + "', '" + MSGVALUE + "', '" + SENDTIME + "', '" + "" + "'); ";
            sqlite_cmd_msg.ExecuteNonQuery();
            sqlite_connect_msg.Close();
            return;
        }


        private async Task PublishB()
        {

            string topic = txtPubTopic.Text.Trim();
            SENDTIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = "{\"TRANSMITMODE\":\"B\",\"PHONE\":\"" + textBox2.Text + "\",\"MESSAGE\":\"" + txtSendMessage.Text + "\",\"SENDTIME\":\"" + SENDTIME + "\"}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
            sqlite_connect_msg = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect_msg.Open(); //Open

            sqlite_cmd_msg = sqlite_connect_msg.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_msg);
            PHONE = textBox2.Text;
            MSGVALUE = SENDTIME + txtSendMessage.Text;
            sqlite_cmd_msg.CommandText = "INSERT INTO MSG VALUES(null, '" + "B" + "', '" + macAddresses + "', '" + PHONE + "', '" + MSGVALUE + "', '" + SENDTIME + "', '" + "" + "'); ";
            sqlite_cmd_msg.ExecuteNonQuery();
            sqlite_connect_msg.Close();
            return;
        }

        private async Task PublishC()
        {

            string topic = txtPubTopic.Text.Trim();
            SENDTIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = "{\"TRANSMITMODE\":\"C\",\"PHONE\":\"" + textBox2.Text + "\",\"MESSAGE\":\"" + txtSendMessage.Text + "\",\"SENDTIME\":\"" + SENDTIME + "\"}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
            sqlite_connect_msg = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect_msg.Open(); //Open

            sqlite_cmd_msg = sqlite_connect_msg.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_msg);
            PHONE = textBox2.Text;
            MSGVALUE = SENDTIME + txtSendMessage.Text;
            sqlite_cmd_msg.CommandText = "INSERT INTO MSG VALUES(null, '" + "C" + "', '" + macAddresses + "', '" + PHONE + "', '" + MSGVALUE + "', '" + SENDTIME + "', '" + "" + "'); ";
            sqlite_cmd_msg.ExecuteNonQuery();
            sqlite_connect_msg.Close();
            return;
        }

        private async Task PublishD()
        {

            string topic = txtPubTopic.Text.Trim();
            SENDTIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show("发布主题不能为空！");
                return;
            }

            string inputString = "{\"TRANSMITMODE\":\"D\",\"PHONE\":\"" + textBox2.Text + "\",\"MESSAGE\":\"" + txtSendMessage.Text + "\",\"SENDTIME\":\"" + SENDTIME + "\"}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(inputString)
                .WithAtLeastOnceQoS()
                .WithRetainFlag(true)
                .Build();

            await mqttClient.PublishAsync(message);
            sqlite_connect_msg = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect_msg.Open(); //Open

            sqlite_cmd_msg = sqlite_connect_msg.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect_msg);
            PHONE = textBox2.Text;
            MSGVALUE = SENDTIME + txtSendMessage.Text;
            sqlite_cmd_msg.CommandText = "INSERT INTO MSG VALUES(null, '" + "D" + "', '" + macAddresses + "', '" + PHONE + "', '" + MSGVALUE + "', '" + SENDTIME + "', '" + "" + "'); ";
            sqlite_cmd_msg.ExecuteNonQuery();
            sqlite_connect_msg.Close();
            return;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Task.Run(async () => { await PublishA(); });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Task.Run(async () => { await PublishB(); });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Task.Run(async () => { await PublishC(); });
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Run(async () => { await PublishD(); });
        }

        private void button7_Click(object sender, EventArgs e)
        {
            sqlite_connect = new SQLiteConnection("Data source=database1.db3");
            //建立資料庫連線
            sqlite_connect.Open(); //Open
            ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            sqlite_cmd = sqlite_connect.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect);
            cmd.CommandText = "SELECT COUNT(SENDTIME) FROM MSG WHERE MSG.SENDTIME > " + "'" + textBox3.Text + "'" + " and MSG.SENDTIME <= " + "'" + textBox4.Text + " 23:59:59'";
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            Console.WriteLine("目前簡訊筆數" + count);
            sqlite_connect.Close();


            SQLiteConnection Connection = new SQLiteConnection("Data Source = database1.db3");
            Connection.Open();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM MSG WHERE MSG.SENDTIME > " + "'" + textBox3.Text + "'" + " and MSG.SENDTIME <= " + "'" + textBox4.Text + " 23:59:59'", Connection);
            DataSet sett = new DataSet();
            adapter.Fill(sett);
            DataTable table = sett.Tables[0];

            for (var i = 0; i < count; i++)
            {
                Console.WriteLine(table.Rows[i][0] + " " + table.Rows[i][1] + " " + table.Rows[i][2] + " " + table.Rows[i][3] + " " + table.Rows[i][4]);
            }
            Connection.Close();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Console.WriteLine("開啟簡訊裝置狀態頁面");
            txtReceiveMessage.AppendText($">> {"開啟簡訊裝置狀態頁面"}{Environment.NewLine}");

            maccount = 1;
            button9.Text = "" + (maccount);
            button10.Text = "" + (maccount + 1);
            button11.Text = "" + (maccount + 2);
            button12.Text = "" + (maccount + 3);
            button13.Text = "" + (maccount + 4);
            button14.Text = "" + (maccount + 5);
            button15.Text = "" + (maccount + 6);
            button16.Text = "" + (maccount + 7);
            button17.Text = "" + (maccount + 8);
            button18.Text = "" + (maccount + 9);

            button19.Text = "" + (maccount + 10);
            button20.Text = "" + (maccount + 11);
            button21.Text = "" + (maccount + 12);
            button22.Text = "" + (maccount + 13);
            button23.Text = "" + (maccount + 14);
            button24.Text = "" + (maccount + 15);
            button25.Text = "" + (maccount + 16);
            button26.Text = "" + (maccount + 17);
            button27.Text = "" + (maccount + 18);
            button28.Text = "" + (maccount + 19);
            if (pv == 0)
            {
                panel1.Visible = true;
                pv = 1;
            }
            else
            {
                panel1.Visible = false;
                pv = 0;


            }


        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (maccount > 180)
            {
                Console.WriteLine("已經是最後一頁");
                txtReceiveMessage.AppendText($">> {"已經是最後一頁"}{Environment.NewLine}");

                return;
            }

            maccount += 20;
            txtReceiveMessage.AppendText($">> {"換下一頁:" + maccount + "~" + (maccount + 19) + "筆"}{Environment.NewLine}");

            Console.WriteLine("換下一頁:" + maccount + "~" + (maccount + 19) + "筆");
            button9.Text = "" + (maccount);
            button10.Text = "" + (maccount + 1);
            button11.Text = "" + (maccount + 2);
            button12.Text = "" + (maccount + 3);
            button13.Text = "" + (maccount + 4);
            button14.Text = "" + (maccount + 5);
            button15.Text = "" + (maccount + 6);
            button16.Text = "" + (maccount + 7);
            button17.Text = "" + (maccount + 8);
            button18.Text = "" + (maccount + 9);

            button19.Text = "" + (maccount + 10);
            button20.Text = "" + (maccount + 11);
            button21.Text = "" + (maccount + 12);
            button22.Text = "" + (maccount + 13);
            button23.Text = "" + (maccount + 14);
            button24.Text = "" + (maccount + 15);
            button25.Text = "" + (maccount + 16);
            button26.Text = "" + (maccount + 17);
            button27.Text = "" + (maccount + 18);
            button28.Text = "" + (maccount + 19);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            if (maccount <= 1)
            {
                Console.WriteLine("已經是第一頁");
                txtReceiveMessage.AppendText($">> {"已經是第一頁"}{Environment.NewLine}");

                return;
            }
            maccount -= 20;
            txtReceiveMessage.AppendText($">> {"換上一頁:" + maccount + "~" + (maccount + 19) + "筆"}{Environment.NewLine}");

            Console.WriteLine("換上一頁:" + maccount + "~" + (maccount + 19) + "筆");
            button9.Text = "" + (maccount);
            button10.Text = "" + (maccount + 1);
            button11.Text = "" + (maccount + 2);
            button12.Text = "" + (maccount + 3);
            button13.Text = "" + (maccount + 4);
            button14.Text = "" + (maccount + 5);
            button15.Text = "" + (maccount + 6);
            button16.Text = "" + (maccount + 7);
            button17.Text = "" + (maccount + 8);
            button18.Text = "" + (maccount + 9);

            button19.Text = "" + (maccount + 10);
            button20.Text = "" + (maccount + 11);
            button21.Text = "" + (maccount + 12);
            button22.Text = "" + (maccount + 13);
            button23.Text = "" + (maccount + 14);
            button24.Text = "" + (maccount + 15);
            button25.Text = "" + (maccount + 16);
            button26.Text = "" + (maccount + 17);
            button27.Text = "" + (maccount + 18);
            button28.Text = "" + (maccount + 19);

        }

        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void button31_Click(object sender, EventArgs e)
        {
            SQLiteConnection ersqlite_connect;
            ersqlite_connect = new SQLiteConnection("Data source=database1.db3");

            ersqlite_connect.Open(); //Open
            var ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            ONLINETIME = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            SQLiteCommand ersqlite_cmd = new SQLiteCommand(ersqlite_connect);
            //建立資料庫連線
            SQLiteDataAdapter coadapter = new SQLiteDataAdapter("Select Count(*) From LINEIOT Where LINEUSERID=" + "'" + textBox6.Text + "'", ersqlite_connect);
            DataSet coset = new DataSet();
            coadapter.Fill(coset);

            LINEUSERID_count = Convert.ToInt32(coset.Tables[0].Rows[0]["Count(*)"].ToString());
            if (LINEUSERID_count != 0)
            {
                txtReceiveMessage.AppendText($">> {"帳號已註冊:" + textBox6.Text }{Environment.NewLine}");
                Console.WriteLine("帳號已註冊:" + textBox6.Text);
                return;
            }
            else if (LINEUSERID_count == 0)
            {
                ersqlite_cmd.CommandText = "INSERT INTO LINEIOT VALUES (null, '" + textBox6.Text + "', '" + textBox6.Text + "', '" + "10" + "', '" + "10" + "', '" + "" + "', '" + ONLINETIME + "');";
                ersqlite_cmd.ExecuteNonQuery();//using behind every write cmd
                ersqlite_connect.Close();

                txtReceiveMessage.AppendText($">> {"手動新增帳號:" + textBox6.Text }{Environment.NewLine}");
                Console.WriteLine("手動新增帳號:" + textBox6.Text);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
           
            Thread t1 = new Thread(SendFromQueue);

            t1.Start();
            t1.Join();

        }
        void CheckMSG()
        {
            /*
           for(int i = 0; i < 1000; i++)
            {
                for(int j = 0; j < 6; j++)
                {
                    SendQueue[i, j] = "";
                }
            }*/

            //txtReceiveMessage.AppendText($"test{Environment.NewLine}");
            Thread.Sleep(100);
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlCommand command = conn.CreateCommand();
            conn.Open();
            MySqlDataAdapter hadapter = new MySqlDataAdapter("Select * From record Where status='1'", conn);

            DataSet hset = new DataSet();
            hadapter.Fill(hset);

            sqlite_connect = new SQLiteConnection("Data source=database1.db3");//建立資料庫連線
            sqlite_connect.Open(); //Open
            sqlite_cmd = sqlite_connect.CreateCommand();//create command
            SQLiteCommand cmd = new SQLiteCommand(sqlite_connect);

            for (int i = 0; i < hset.Tables[0].Rows.Count; i++)
            {
                var TIME_CONV = DateTime.Parse(hset.Tables[0].Rows[i]["sendtime"].ToString());
                string min_comp = DateDiff(TIME_CONV, DateTime.Now);

                if (int.Parse(min_comp) >= 1)
                {
                    string no = hset.Tables[0].Rows[i]["msg_log"].ToString();
                    sqlite_cmd.CommandText = "UPDATE RECORD SET STATUS='1' WHERE NO='" + no + "'";
                    sqlite_cmd.ExecuteNonQuery();
                }


            }

            sqlite_connect.Close();
            conn.Close();

            Console.WriteLine("--------------------------------Messages Checked--------------------------------");



        }






        void CLR_Device()
        {

            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlCommand command = conn.CreateCommand();
            conn.Open();
            command.CommandText = "UPDATE device SET active_status = 'N'";
            command.ExecuteNonQuery();
            conn.Close();
        }

        private void lineiot_0424_1630_FormClosing(object sender, FormClosingEventArgs e)
        {

            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlCommand command = conn.CreateCommand();
            conn.Open();
            command.CommandText = "UPDATE device SET active_status = 'N'";
            command.ExecuteNonQuery();
            conn.Close();
        }
        private string DateDiff(DateTime DateTime1, DateTime DateTime2)
        {
            string dateDiff = null;
            TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
            TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
            TimeSpan ts = ts1.Subtract(ts2).Duration();
            dateDiff = ts.Minutes.ToString();
            Console.WriteLine(dateDiff);
            return dateDiff;
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            CheckMSG();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            CLR_Device();
        }

        private void txtReceiveMessage_TextChanged(object sender, EventArgs e)
        {

        }



        private void button32_Click_1(object sender, EventArgs e)
        {
            string Packet;
            string STR = "123";
            Thread.Sleep(50);
            //Pack the sending packet-------------------------------------------
            UPDATETIME = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //Packet = "{\"ID\":\"" + STR + "\",\"ORDER_ID\":\"" + STR + "\",\"TRANSMITMODE\":\"" + STR + "\",\"PHONE\":\"" + STR + "\",\"MESSAGE\":\"" + STR + "\",\"SENDTIME\":\"" + STR + "\",\"MSG_LOG\":\"" + STR + "\"}";
            Packet = textBox7.Text;
            //Pack the sending packet-------------------------------------------
            //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //sending message---------------------------------------------------
            var message = new MqttApplicationMessageBuilder()
                       .WithTopic("ACC125C40A24")
                       .WithPayload(Packet)
                       .WithAtLeastOnceQoS()
                       .WithRetainFlag(false)
                       .Build();
            Task.Run(async () =>
            {
                await mqttClient.PublishAsync(message);//MESSAGE改成message
            });
            Console.WriteLine(STR + "已傳送\n");
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtIp_TextChanged(object sender, EventArgs e)
        {

        }


    }
}
