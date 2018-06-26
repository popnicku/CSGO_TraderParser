using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace TraderParser
{
    public class Proxy
    {
        //private string API_Key = "8r94x34lxjpy3bgc5cox6ukoftwzqw";
        //private string getProxyURL;
        //private string IP_String = null;
        //private WebClient proxyWebClient;
        //private JObject json;
        private List<WebProxy> proxyList = new List<WebProxy>();
        private int proxy_counter = -1;
        //private int linesOfProxy = 0;
        public bool proxyBusy = true;
        public int numberOfWorkingProxies = 0;
        
        public Proxy()
        {
            Console.WriteLine("Initializing proxy thread...");
            MainWindow.main.UpdatePricesFromProxy("Getting proxies, please wait\n");
            Thread getProxyThread = new Thread(GetProxyListFromFile);
            getProxyThread.Start();
            
        }
        Stopwatch sw = new Stopwatch();
        private void GetProxyListFromFile()
        {
            Console.WriteLine("Geting proxies from list");
            StreamReader file = new StreamReader("../../proxy_list.txt");
            string line;
            
            //string parsedLine;
            List<string> proxyLines = new List<string>();
            while ((line = file.ReadLine()) != null)
			{
                proxyLines.Add(line);
            }
            file.Dispose(); // hmm?
			Parallel.For(0, proxyLines.Count, index =>
            {
                try
                {
                    string currentProxy = proxyLines[index];
                    //WebProxy proxy = new WebProxy(currentProxy);

                    WebClient webProxyClient = new WebClient()
                    {
                        Proxy = new WebProxy(currentProxy),
                        //UseDefaultCredentials = true
                    };
                    webProxyClient.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

                    string downloadedString = null;

                    Task proxTask = Task.Run(() =>
                    {
                        try
                        {
                            downloadedString = webProxyClient.DownloadString("https://steamcommunity.com/market/priceoverview/?appid=730&currency=3&market_hash_name=%E2%98%85%20Flip%20Knife%20%7C%20Urban%20Masked%20(Field-Tested)");
                            //Console.WriteLine("Downloaded string: " +  downloadedString);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception in task" + e);
                        }
                    });
                    if (proxTask.Wait(TimeSpan.FromSeconds(10))) 
                    {
                        if (downloadedString != null)
                        {
                            Console.WriteLine("Proxy " + currentProxy + " ok");
                            MainWindow.main.UpdatePricesFromProxy(currentProxy);
                            numberOfWorkingProxies++;
                            proxyList.Add(new WebProxy(currentProxy));
                            if (numberOfWorkingProxies >= 5)
							{
                                //MainWindow.main.Web_Parser.InitializeThreads();
							}
                            //Console.WriteLine("Downloaded string2: " + downloadedString);
                        }
                        else
                        {
                            Console.WriteLine("[IF Failed][Proxy.cs][GetProxyListFromFile]:: downloadString is null");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[IF Failed][Proxy.cs][GetProxyListFromFile]:: Page took too long to load");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EXCEPTION][Proxy.cs][GetProxyListFromFile]:: " + ex.Message);
                    //Handle exceptions
                }
            });
            Console.WriteLine("Proxies from list retrieved, total of " + proxyList.Count);
            MainWindow.main.UpdatePricesFromProxy("Total number of proxies:  " + proxyList.Count);
            MainWindow.main.Dispatcher.BeginInvoke(new Action(() => 
            {
                MainWindow.main.ParseButton.IsEnabled = true;
                MainWindow.main.Image_LoadingGIF.Visibility = System.Windows.Visibility.Hidden;
            }));
        }


        public void RemoveProxy(WebProxy proxyToRemove)
        {
            proxyList.Remove(proxyToRemove);
        }

        public WebProxy GetNextProxy()
        {
            if (proxyList.Count > 0)
            {
                try
                {
                    proxy_counter++;
                    Console.WriteLine("Proxy is changed to: " + proxyList[proxy_counter].Address);
                    return proxyList[proxy_counter];
                }
                catch
                {
                    proxy_counter = 0;
                    Console.WriteLine("Proxy is changed to: " + proxyList[proxy_counter].Address);
                    return proxyList[proxy_counter];
                }
            }
            else
            {
                return null;
            }
        }



    }
}
