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

        string loginfile = "C:\\dlogs\\login.txt";  // lol username/password in plain text
        string filename = "C:\\dlogs\\log.csv";
        string counterLog = "C:\\dlogs\\counter.txt";
        int[] sessionOffset;

        string username;
        string password;

        List<Label> totalCounters;
        Label[] sessionCounters;

        DateTime sessionStartTime;

        public MainWindow()
        {
            InitializeComponent();
            client = new DiscordClient();
            counters = new Dictionary<string, int>();
            sessionOffset = new int[NUM_CHANNELS];
            sessionCounters = new Label[NUM_CHANNELS];
            totalCounters = new List<Label>();

            AddCounters();

            loggerCanvas.Visibility = Visibility.Collapsed;
            connectCanvas.Visibility = Visibility.Visible;

            buttonLogin.Click += ButtonLogin_Click;
            this.Closing += MainWindow_Closing;

            client.MessageReceived += Client_MessageReceived;
            client.GatewaySocket.Disconnected += GatewaySocket_Disconnected;

            // preload username/pw cause lazy
            if (File.Exists(loginfile))
            {
                string[] login = File.ReadAllLines(loginfile);
                textboxUsername.Text = login[0];
                textboxPassword.Password = login[1];
            }
        }

        /*
        *   Warning when user tries to close   
        */
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Do you want to exit?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }

        /*
        *   Login using the given email and password.
        *   Currently no error checking!
        */
        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            username = textboxUsername.Text;
            password = textboxPassword.Password;

            ConnectToServer();

            loggerCanvas.Visibility = Visibility.Visible;
            connectCanvas.Visibility = Visibility.Collapsed;
        }

        /*
        *   Attempt to reconnect when disconnected.
        *   Untested.
        */
        private void GatewaySocket_Disconnected(object sender, DisconnectedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                statusLabel.Content = "Disconnected";
            
                while (client.State == ConnectionState.Disconnected)
                {
                    errorLog.Content += ("Trying to reconnect");
                    ConnectToServer();  // try to reconnect
                    System.Threading.Thread.Sleep(5000);
                }
            });
        }


        /*
        *   Connect to Discord, and create a new log file with current timestamp
        */
        async void ConnectToServer()
        {
            filename = string.Format("C:\\dlogs\\log-{0:MM-dd HH-mm-ss}.csv", DateTime.UtcNow);
            try
            {
                string x = await client.Connect(username, password);

                statusLabel.Content = "Connected";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    sessionStartTime = DateTime.UtcNow;
                    startTime.Content = string.Format("Program Started: {0:MM/dd HH:mm:ss}", sessionStartTime);
                    sessionTime.Content = string.Format("Session Started: {0:MM/dd HH:mm:ss}", sessionStartTime);
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    errorLog.Content += ("connectError" + e.Message);
                });
            }
            LoadCounters();
        }

        /*
        *   Log a received message
        */
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


        /*
        *   Update message counters
        */
        void UpdateCounters(string newLine)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (counters["total"] % 10 == 0)
                    currentMsg.Content = newLine;
                else
                    currentMsg.Content += "\n" + newLine;

                // log the total counter every 500 messages
                if (counters["total"] % 5 == 0)
                    LogCounters();

                DateTime now = DateTime.UtcNow;
                timeNow.Content = string.Format("Last MSG: {0:MM/dd HH:mm:ss}", now);

                TimeSpan duration = now - sessionStartTime;
                elapsedTime.Content = string.Format("Time Elapsed: {0}days, {1:00}:{2:00}:{3:00}",
                                            duration.Days,
                                            duration.Hours,
                                            duration.Minutes,
                                            duration.Seconds);
                

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


                sessionCounter0.Content = counters["total"] - sessionOffset[0];
                sessionCounter1.Content = counters["general"] - sessionOffset[1];
                sessionCounter2.Content = counters["miscellaneous"] - sessionOffset[2];
                sessionCounter3.Content = counters["advice-serious"] - sessionOffset[3];
                sessionCounter4.Content = counters["meta"] - sessionOffset[4];
                sessionCounter5.Content = counters["anime-manga"] - sessionOffset[5];
                sessionCounter6.Content = counters["games"] - sessionOffset[6];
                sessionCounter7.Content = counters["kancolle"] - sessionOffset[7];
                sessionCounter8.Content = counters["idol-heaven"] - sessionOffset[8];
                sessionCounter9.Content = counters["fanart"] - sessionOffset[9];
                sessionCounter10.Content = counters["nsfw"] - sessionOffset[10];
                sessionCounter11.Content = counters["skynet"] - sessionOffset[11];
                sessionCounter12.Content = counters["other"] - sessionOffset[12];
            });
        }


        /*
        *   Log the counters to a file
        */
        void LogCounters()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> counter in counters)
            {
                sb.AppendLine(counter.Key +" "+counter.Value);
            }
            
            File.WriteAllText(counterLog, sb.ToString());
        }


        /*
        *   Load total counters from file
        */
        void LoadCounters()
        {
            string[] counterData = File.ReadAllLines(counterLog);

            foreach(string countertext in counterData)
            {   
                // split on space, then read key value pair
                string[] channel = countertext.Split(' ');
                counters[channel[0]] = Int32.Parse(channel[1]);
            }

            // set session offsets properly
            ResetSession(null, null);
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

            // add totalLabels to array
            totalCounters.Add(counter0);
            totalCounters.Add(counter1);
            totalCounters.Add(counter2);
            totalCounters.Add(counter3);
            totalCounters.Add(counter4);
            totalCounters.Add(counter5);
            totalCounters.Add(counter6);
            totalCounters.Add(counter7);
            totalCounters.Add(counter8);
            totalCounters.Add(counter9);
            totalCounters.Add(counter10);
            totalCounters.Add(counter11);
            totalCounters.Add(counter12);


            // add sessionLabels to array
            sessionCounters[0] = sessionCounter0;
            sessionCounters[1] = sessionCounter1;
            sessionCounters[2] = sessionCounter2;
            sessionCounters[3] = sessionCounter3;
            sessionCounters[4] = sessionCounter4;
            sessionCounters[5] = sessionCounter5;
            sessionCounters[6] = sessionCounter6;
            sessionCounters[7] = sessionCounter7;
            sessionCounters[8] = sessionCounter8;
            sessionCounters[9] = sessionCounter9;
            sessionCounters[10] = sessionCounter10;
            sessionCounters[11] = sessionCounter11;
            sessionCounters[12] = sessionCounter12;
        }

        /*
        *   Reset session counter by saving the offset
        *
        */
        private void ResetSession(object sender, RoutedEventArgs e)
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
                sessionStartTime = DateTime.UtcNow;
                sessionTime.Content = string.Format("Session Started: {0:MM/dd HH:mm:ss}", sessionStartTime);
                
                // reset session counters on GUI
                foreach (Label sessionCounter in sessionCounters)
                {
                    sessionCounter.Content = 0;
                }
            });
        }
    }
}
