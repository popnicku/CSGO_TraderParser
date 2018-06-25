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
        /*public string GetProxyIP()
        {
            
            getProxyURL = "https://spinproxies.com/api/v1/proxyrotate?country_code=US&key=" + API_Key;
            proxyWebClient = new WebClient();
            string downString = proxyWebClient.DownloadString(getProxyURL);
            json = JObject.Parse(downString);
            IP_String = json["data"]["proxy"]["ip"] + ":" + json["data"]["proxy"]["port"];
            proxyList.Add(IP_String);
            return IP_String;
        }*/
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
                            downloadedString = webProxyClient.DownloadString("https://csgolounge.com/");
                            //Console.WriteLine("Downloaded string: " +  downloadedString);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception in task" + e);
                        }
                    });
                    if (proxTask.Wait(TimeSpan.FromSeconds(60))) 
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
                            Console.WriteLine("failed in task");
                        }
                    }
                    else
                    {
                        Console.WriteLine("timed out");
                    }

                }
                catch (Exception ex)
                {
                    //Handle exceptions
                }
            });
            Console.WriteLine("Proxies from list retrieved, total of " + proxyList.Count);
            MainWindow.main.UpdatePricesFromProxy("Total number of proxies:  " + proxyList.Count);
            MainWindow.main.Dispatcher.BeginInvoke(new Action(() => { MainWindow.main.ParseButton.IsEnabled = true; }));
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
