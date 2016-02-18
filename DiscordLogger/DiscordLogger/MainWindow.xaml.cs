using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Discord;
using DiscordLogger;
using Discord.Legacy;
using System.IO;

namespace DiscordLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly int NUM_CHANNELS = 13;

        DiscordClient client;
        Dictionary<string, int> counters;
        string filename = "C:\\dlogs\\log.csv";
        int[] sessionOffset;

        public MainWindow()
        {
            InitializeComponent();
            client = new DiscordClient();
            counters = new Dictionary<string, int>();
            AddCounters();
            sessionOffset = new int[NUM_CHANNELS];

            ConnectToServer();

            client.MessageReceived += Client_MessageReceived;
            client.GatewaySocket.Disconnected += GatewaySocket_Disconnected;
        }

        private void GatewaySocket_Disconnected(object sender, DisconnectedEventArgs e)
        {
            statusLabel.Content = "Disconnected";
            while (client.State == ConnectionState.Disconnected)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    errorLog.Content += ("Trying to reconnect");
                });
                ConnectToServer();  // try to reconnect
                System.Threading.Thread.Sleep(5000);
            }
        }

        async void ConnectToServer()
        {
            filename = string.Format("C:\\dlogs\\log-{0:MM-dd HH-mm-ss}.csv", DateTime.UtcNow);
            try
            {
                string x = await client.Connect("coolgame88@hotmail.com", "bukbot");

                statusLabel.Content = "Connected";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    startTime.Content = string.Format("Program Started: {0:MM/dd HH:mm:ss}", DateTime.UtcNow);
                    sessionTime.Content = string.Format("Session Started: {0:MM/dd HH:mm:ss}", DateTime.UtcNow);
                });

            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    errorLog.Content += ("connectError" + e.Message);
                });
            }
        }

        void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            StringBuilder csv = new StringBuilder();
            Message msg = e.Message;

            // parse message
            string channel = msg.Channel.Name;
            ulong cId = msg.Channel.Id;
            DateTime time = msg.Timestamp;
            string user = msg.User.Name;
            ulong uId = msg.User.Id;
            string text = msg.Text;

            //strip quotations
            text = text.Replace("\"", "$");
            text = text.Replace(",", "$");

            // check for attachments
            Message.Attachment[] att = e.Message.Attachments;
            char hasFile = 'n';
            if (att.Length > 0 ||
                text.Contains(".jpg") || text.Contains(".png") || text.Contains(".gif"))
            {
                hasFile = 'y';
            }


            // write line to file
            string newLine = string.Format("\"{0:g}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"", 
                                            time, channel, cId, user, uId, hasFile, text);
            csv.AppendLine(newLine);

            // increment counter
            counters["total"]++;
            if (counters.ContainsKey(channel))
                counters[channel]++;
            else
                counters["other"]++;
            UpdateCounters(newLine);



            try
            {
                File.AppendAllText(filename, csv.ToString());
            }
            catch (Exception err)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    errorLog.Content += ("writeError" + err.Message);
                });
            }
        }


        void UpdateCounters(string newLine)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (counters["total"] % 10 == 0)
                    currentMsg.Content = newLine;
                else
                    currentMsg.Content += "\n" + newLine;

                timeNow.Content = string.Format("Last MSG: {0:MM/dd HH:mm:ss}", DateTime.UtcNow);

                counter0.Content = counters["total"];
                counter1.Content = counters["general"];
                counter2.Content = counters["miscellaneous"];
                counter3.Content = counters["advice-serious"];
                counter4.Content = counters["meta"];
                counter5.Content = counters["anime-manga"];
                counter6.Content = counters["games"];
                counter7.Content = counters["kancolle"];
                counter8.Content = counters["idol-heaven"];
                counter9.Content = counters["fanart"];
                counter10.Content = counters["nsfw"];
                counter11.Content = counters["skynet"];
                counter12.Content = counters["other"];

                counter0_Copy.Content = counters["total"] - sessionOffset[0];
                counter1_Copy.Content = counters["general"] - sessionOffset[1];
                counter2_Copy.Content = counters["miscellaneous"] - sessionOffset[2];
                counter3_Copy.Content = counters["advice-serious"] - sessionOffset[3];
                counter4_Copy.Content = counters["meta"] - sessionOffset[4];
                counter5_Copy.Content = counters["anime-manga"] - sessionOffset[5];
                counter6_Copy.Content = counters["games"] - sessionOffset[6];
                counter7_Copy.Content = counters["kancolle"] - sessionOffset[7];
                counter8_Copy.Content = counters["idol-heaven"] - sessionOffset[8];
                counter9_Copy.Content = counters["fanart"] - sessionOffset[9];
                counter10_Copy.Content = counters["nsfw"] - sessionOffset[10];
                counter11_Copy.Content = counters["skynet"] - sessionOffset[11];
                counter12_Copy.Content = counters["other"] - sessionOffset[12];
            });
        }


        void AddCounters()
        {
            counters.Add("total", 0);
            counters.Add("general", 0);
            counters.Add("miscellaneous", 0);
            counters.Add("advice-serious", 0);
            counters.Add("meta", 0);
            counters.Add("anime-manga", 0);
            counters.Add("games", 0);
            counters.Add("kancolle", 0);
            counters.Add("idol-heaven", 0);
            counters.Add("fanart", 0);
            counters.Add("nsfw", 0);
            counters.Add("skynet", 0);
            counters.Add("other", 0);
        }

        // reset counter offset
        private void button_Click(object sender, RoutedEventArgs e)
        {
            // save offset
            sessionOffset[0] = counters["total"];
            sessionOffset[1] = counters["general"];
            sessionOffset[2] = counters["miscellaneous"];
            sessionOffset[3] = counters["advice-serious"];
            sessionOffset[4] = counters["meta"];
            sessionOffset[5] = counters["anime-manga"];
            sessionOffset[6] = counters["games"];
            sessionOffset[7] = counters["kancolle"];
            sessionOffset[8] = counters["idol-heaven"];
            sessionOffset[9] = counters["fanart"];
            sessionOffset[10] = counters["nsfw"];
            sessionOffset[11] = counters["skynet"];
            sessionOffset[12] = counters["other"];

            Application.Current.Dispatcher.Invoke(() =>
            {
                sessionTime.Content = string.Format("Session Started: {0:MM/dd HH:mm:ss}", DateTime.UtcNow);

                // reset session counters
                counter0_Copy.Content = 0;
                counter1_Copy.Content = 0;
                counter2_Copy.Content = 0;
                counter3_Copy.Content = 0;
                counter4_Copy.Content = 0;
                counter5_Copy.Content = 0;
                counter6_Copy.Content = 0;
                counter7_Copy.Content = 0;
                counter8_Copy.Content = 0;
                counter9_Copy.Content = 0;
                counter10_Copy.Content = 0;
                counter11_Copy.Content = 0;
                counter12_Copy.Content = 0;
            });
        }
    }
}
