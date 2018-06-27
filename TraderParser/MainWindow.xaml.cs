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
using MahApps.Metro.Controls;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using System.Diagnostics;

namespace TraderParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
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
            if (CheckBox_CustomURL.IsChecked == true)
            {
                if (TextBox_CustomURL.Text.Contains("https://csgolounge.com"))
                {
                    Web_Parser.InitializeThreads(TextBox_CustomURL.Text);
                }
                else
                {
                    MessageBox.Show("Invalid URL");
                }
            }
            else
            {
                Web_Parser.InitializeThreads(null);
            }
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

        private void CheckBox_LoungeProxy_Checked(object sender, RoutedEventArgs e)
        {
            Web_Parser.proxyEnabled = (bool)CheckBox_LoungeProxy.IsChecked;
        }

        private void CheckBox_CustomURL_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_CustomURL.IsChecked == true)
                TextBox_CustomURL.Visibility = Visibility.Visible;
            else
                TextBox_CustomURL.Visibility = Visibility.Hidden;
        }
        private void Button_ClearTable_Click(object sender, RoutedEventArgs e)
        {
            TradersToBeDisplayed = new ObservableCollection<TradeDetails>();
            DataGrid_TradesList.ItemsSource = null;
            DataGrid_TradesList.Items.Refresh();
            DataGrid_TradesList.Items.Clear();
        }
        private void DG_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)e.OriginalSource;
            Process.Start(link.NavigateUri.AbsoluteUri);
        }
    }
}
