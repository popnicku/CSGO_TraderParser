using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace TraderParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static MainWindow main;
        public Parser Web_Parser;
        public Proxy Web_Proxy;

        public ConcurrentQueue<TradeDetails> TradersQueue = new ConcurrentQueue<TradeDetails>();
        private ObservableCollection<TradeDetails> TradersToBeDisplayed = new ObservableCollection<TradeDetails>();

        public MainWindow()
        {
            main = this;
            InitializeComponent();

            Web_Parser = new Parser();

            Thread UIThread = new Thread(UpdateUI_Thread);
            UIThread.Start();
            //Web_Proxy = new Proxy();

            //string test = Web_Parser.Page_Load();

            //Console.WriteLine("Parsed: " + test + "\n");
        }

        public void UpdatePricesFromProxy(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => { TextBox_Proxy_List.Text += text + "\n"; }));
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {

            //Console.WriteLine(IP_Proxy.GetProxyIP());
            //Console.WriteLine(IP_Proxy.GetNextProxy());
            Image_LoadingGIF.Visibility = Visibility.Visible;
            Web_Parser.InitializeThreads();
            //GetData("https://csgoempire.com");
        }

        public void UpdateUI_Thread()
        {
            
            for (; ; )
            {
                if (TradersQueue.Count > 0)
                {
                    if (TradersQueue.TryDequeue(out TradeDetails receivingStruct))
                    {
                        Console.WriteLine("Added " + receivingStruct.ItemName);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            //DataGrid_TradesList.Items.Refresh();
                            TradersToBeDisplayed.Add(new TradeDetails()
                            {
                                ItemLink = receivingStruct.ItemLink,
                                ItemName = receivingStruct.ItemName,
                                ItemPrice = receivingStruct.ItemPrice,
                                TradeLink = receivingStruct.TradeLink,
                            });
                            
                            this.DataGrid_TradesList.ItemsSource = TradersToBeDisplayed;

                        }));
                    }
                }
            }
        }
    }
}
