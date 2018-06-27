using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Util;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Threading.Tasks;

namespace TraderParser
{

    public class Parser
    {

        List<DataStructure> TradeStructure;
        JObject NameAndPrice;
        public Proxy IP_Proxy;
        private WebProxy wp;
        public bool proxyEnabled = false;
        private string Custom_URL;


        public Parser()
        {
            IP_Proxy = new Proxy();
            DownloadSteamAPI();
            TradeStructure = new List<DataStructure>();
        }

        private void DownloadSteamAPI()
        {
            WebClient client = new WebClient();
            string download = client.DownloadString("https://api.steamapis.com/market/items/730?api_key=cwPJTL15sWDgRywQQaWYKMKr6O4&format=compact&compact_value=latest");
            NameAndPrice = JObject.Parse(download);
        }

        public void InitializeThreads(string customURL)
        {
            if (customURL != null)
                Custom_URL = customURL;
            else
                Custom_URL = "https://csgolounge.com/result?ldef_index%5B%5D=2703&lquality%5B%5D=0&id%5B%5D=&";
            Thread th = new Thread(Start_Web_Thread);
            th.Start();
        }

        public void Start_Web_Thread()
        {
            //x items at a time
            string itemsList;
            for (int i = 0; i < 1; i++)
            {
                if (wp != null && proxyEnabled)
                {
                    wp = IP_Proxy.GetNextProxy();
                    Console.WriteLine("Thread " + wp.Address.ToString().Replace("http://", "") + " entered");
                }
                itemsList = ParseLoungeNames(); // contains all trades, need to split them
                MainWindow.main.Dispatcher.BeginInvoke(new Action
                    (() =>{
                        //foreach(string item in itemsList)
                        //MainWindow.main.Prices.Text = itemsList; 
                    }));
                    
            }
        }

        private List<string> ParseSingleTrade_Step1(string singleTrade)
        {
            string left = null, right = null;
            List<string> singleTradeList;

            singleTradeList = new List<string>();
            left = GetCustomXNode(singleTrade, "form", "left"); // list with contains all items from 1 trade on the left side [0, 1, 2,...]
            right = GetCustomXNode(singleTrade, "form", "right"); // contains all items from 1 trade on the right side3
           
            singleTradeList.Add(left);
            singleTradeList.Add(right);
            return singleTradeList;
        }
        private float ComputePrice(string itemName)
        {
            //string replacedString = ReplaceSpecialChars(itemName);
            //replacedString = ReplaceWithAPI(replacedString);
            return GetPriceFromAPI(itemName);
        }

        private TradeDetails SingleRowToSend(ItemsStructure row, DataStructure structureToBeParsed)
        {
            TradeDetails detailsToSend = new TradeDetails();
            detailsToSend = new TradeDetails();
            detailsToSend.ItemLink = row.ItemLink;
            detailsToSend.ItemName = row.ItemName;
            detailsToSend.ItemPrice = row.ItemPrice.ToString();
            detailsToSend.TradeLink = "https://csgolounge.com/" + structureToBeParsed.Tradelink;
            return detailsToSend;
        }
        private TradeDetails SingleRowToSend(string container)
        {
            TradeDetails detailsToSend = new TradeDetails();
            detailsToSend = new TradeDetails();
            detailsToSend.ItemLink = container;
            detailsToSend.ItemName = container;
            detailsToSend.ItemPrice = container;
            detailsToSend.TradeLink = container;
            return detailsToSend;
        }

        private void SendStructToUI(DataStructure structureToBeParsed)
        {
            List<ItemsStructure> items = new List<ItemsStructure>();
            TradeDetails detailsToSend = new TradeDetails();

            items = structureToBeParsed.LeftItems;


            foreach (ItemsStructure singleItem in items)
            {
                detailsToSend = new TradeDetails();
                detailsToSend = SingleRowToSend(singleItem, structureToBeParsed);
                MainWindow.main.TradersQueue.Enqueue(detailsToSend);
            }
            detailsToSend = new TradeDetails();
            detailsToSend = SingleRowToSend("");
            MainWindow.main.TradersQueue.Enqueue(detailsToSend);

            items = structureToBeParsed.RightItems;
            detailsToSend = new TradeDetails();
            foreach (ItemsStructure singleItem in items)
            {
                detailsToSend = new TradeDetails();
                detailsToSend = SingleRowToSend(singleItem, structureToBeParsed);
                MainWindow.main.TradersQueue.Enqueue(detailsToSend);
            }

            detailsToSend = new TradeDetails();
            detailsToSend = SingleRowToSend(" ");
            MainWindow.main.TradersQueue.Enqueue(detailsToSend);
            detailsToSend = new TradeDetails();
            detailsToSend = SingleRowToSend(" ");
            MainWindow.main.TradersQueue.Enqueue(detailsToSend);
        }

        private ItemsStructure ConstructSmallStructure(string itemLink, HtmlNode nodeToDo)
        {
            //bool proxyChanged = false;
            float price = -1;
            /*proxyNeeded:
            if (!proxyChanged)
            {
                try
                {
                    if (itemLink != "NVM")
                        price = ComputePrice(itemLink);
                    else
                        price = -3;
                }
                catch (WebException e)
                {
                    Console.WriteLine(e.Message);
                    if (e.Message == "The remote server returned an error: (429) Too Many Requests.")
                    {
                        wp = IP_Proxy.GetNextProxy();
                        proxyChanged = true;
                    }
                    goto proxyNeeded;
                }
            }*/
            ItemsStructure StructureToReturn = new ItemsStructure
            {
                ItemLink = ReplaceSpecialChars(itemLink),
                ItemName = nodeToDo.InnerHtml,
                ItemPrice = price
            };

            return StructureToReturn;
        }

        private List<string> GetLinksFromString(string content)
        {
            MatchCollection mc = Regex.Matches(content, "<a[^>]+href=\"(.*?)\"[^>]*>");
            List<string> steamLinks = new List<string>();
            foreach (Match m in mc)
            {
                if (m.Value.Contains("steamcommunity.com"))
                {
                    steamLinks.Add(m.Value);
                }
            }

            for(int i = 0; i < steamLinks.Count; i++)
            {
                steamLinks[i] = steamLinks[i].Remove(0, 9); // remove <a href....
                if(steamLinks[i].Contains("target=")) // failsafe
                {
                    steamLinks[i] = steamLinks[i].Remove(steamLinks[i].Length - 18, 18); // remove target=blabla from end
                }
            }

            return steamLinks;
        }

        public string ParseLoungeNames() // returns html of trades
        {
            string listToBeOut = null, downloadString;
            string oneLeft, oneRight;
            string[] customUrl;
            List<HtmlNode> singleRowTrade_Node = new List<HtmlNode>();
            List<List<string>> LeftPlusRight = new List<List<string>>();
            List<ItemsStructure> WholeLeftStructure;
            List<ItemsStructure> WholeRightStructure;
            List<string> NamesList = new List<string>();
            List<string> LinksList = new List<string>();

            List<string> singleTradeList;

            using (WebClient client = new WebClient())
            {
                if (proxyEnabled)
                {
                    client.Proxy = wp;
                    Console.WriteLine("Changing proxy...");
                    Console.WriteLine("Proxy changed to " + wp.Address);
                }
                try
                {
                    while (client.IsBusy)
                    {
                        Thread.Sleep(20);
                    }
                    //downloadString = client.DownloadString("https://csgolounge.com");
                    downloadString = client.DownloadString(Custom_URL);
                    //client.Proxy = new WebProxy(); // reset proxy
                    singleRowTrade_Node = ParseHTMLFromString(downloadString);
                    customUrl = singleRowTrade_Node[0].InnerHtml.Split('Â'); // contains all trades

                    //retrieve username from customURL
                    foreach (string singleTrade in customUrl)
                    {
                        singleTradeList = new List<string>();
                        if (singleTrade.Contains("left") || singleTrade.Contains("right"))
                        {
                            NamesList.Add(GetName(singleTrade));
                            LinksList.Add(GetLink(singleTrade));
                            singleTradeList = ParseSingleTrade_Step1(singleTrade);
                            LeftPlusRight.Add(singleTradeList); // contains list with all trades (0, 2, 4...left; 1,3,5,7, - right) 
                        }                                //need to compare all from left to all from right
                    }
                    // repeat this for steam links?
                    int iterator = 0;
                    foreach (List<string> singleOne in LeftPlusRight)
                    {
                        WholeLeftStructure = new List<ItemsStructure>();
                        WholeRightStructure = new List<ItemsStructure>();
                        //single one now contains 2 lists (one for left, one for right)
                        if(singleOne.Count < 2)
                        {
                            Console.WriteLine("[IF Failed][Parser.cs][ParseLoungeNames]:: singleOne too small: " + singleOne.Count);
                        }
                        oneLeft = singleOne[0];
                        oneRight = singleOne[1];
                        List<HtmlNode> nodeListBuffer = GetCustomSimpleNode(oneLeft, "b");

                        MatchCollection mc = Regex.Matches(oneLeft, "<a[^>]+href=\"(.*?)\"[^>]*>");
                        List<string> steamLinksLeft = GetLinksFromString(oneLeft);
                        List<string> steamLinksRight = GetLinksFromString(oneRight);

                        int linkStartRight = oneRight.IndexOf("<a href=\"http:");
                        int linkEndRight = oneRight.IndexOf("target=");
                        string itemLinkRight = HttpUtility.HtmlDecode(oneRight.Substring(linkStartRight + 9, linkEndRight - linkStartRight - 11));

                        int linksIterator = 0;
                        foreach (HtmlNode nodeBuffer in nodeListBuffer)
                        {

                            ItemsStructure leftStructureForList;
                            if (nodeBuffer.InnerHtml.Contains("Any Offers") || nodeBuffer.InnerHtml.Contains("Real Money") || nodeBuffer.InnerHtml.Contains("Any Keys"))
                            {
                                leftStructureForList = ConstructSmallStructure("NVM", nodeBuffer);
                            }
                            if (linksIterator < steamLinksLeft.Count)
                            {
                                leftStructureForList = ConstructSmallStructure(steamLinksLeft[linksIterator], nodeBuffer);
                                WholeLeftStructure.Add(leftStructureForList);
                                linksIterator++;
                            }
                            else
                            {
                                //Console.WriteLine("Iterator too big");
                                Console.WriteLine("[IF Failed][Parser.cs][ParseLoungeNames]:: Left Iterator too big: " + linksIterator + ", max is: " + steamLinksLeft.Count);
                            }
                        }
                        linksIterator = 0;
                        nodeListBuffer = GetCustomSimpleNode(oneRight, "b");
                        foreach (HtmlNode nodeBuffer in nodeListBuffer)
                        {
                            ItemsStructure rightStructureForList;
                            if (nodeBuffer.InnerHtml.Contains("Any Offers") || nodeBuffer.InnerHtml.Contains("Real Money") || nodeBuffer.InnerHtml.Contains("Any Keys"))
                            {
                                rightStructureForList = ConstructSmallStructure("NVM", nodeBuffer);
                            }
                            if (linksIterator < steamLinksRight.Count)
                            {
                                rightStructureForList = ConstructSmallStructure(steamLinksRight[linksIterator], nodeBuffer);
                                WholeRightStructure.Add(rightStructureForList);
                                linksIterator++;
                            }
                            else
                            {
                                Console.WriteLine("[IF Failed][Parser.cs][ParseLoungeNames]:: Right Iterator too big: " + linksIterator + ", max is: " + steamLinksRight.Count);
                            }
                        }
                        if(iterator >= NamesList.Count)
                        {
                            Console.WriteLine("[IF Failed][Parser.cs][ParseLoungeNames]:: Names Iterator too big: " + linksIterator + ", max is: " + NamesList.Count);
                        }
                        DataStructure toBeAddedToStruct = new DataStructure
                        {
                            SellerName = NamesList[iterator],
                            Tradelink = LinksList[iterator],
                            LeftItems = WholeLeftStructure,
                            RightItems = WholeRightStructure
                        };
                        TradeStructure.Add(toBeAddedToStruct);
                        //SendStructToUI(toBeAddedToStruct);
                        iterator++;
                    }
                    SeparatePriceComputer();
                }
                catch(Exception e)
                {
                    Console.WriteLine("[EXCEPTION][Parser.cs][ParseLoungeNames]:: " + e.Message);
                }
            }
            TradeStructure = new List<DataStructure>();
            return listToBeOut;
        }

        private void SeparatePriceComputer()
        {
            //foreach(DataStructure singleRow in TradeStructure)
            for(int i = 0; i < TradeStructure.Count; i++)
            {
                //foreach(ItemsStructure item in singleRow.LeftItems)
                for(int j = 0; j < TradeStructure[i].LeftItems.Count; j++)
                {
                    TradeStructure[i].LeftItems[j].ItemPrice = ComputePrice(TradeStructure[i].LeftItems[j].ItemName);
                }
                for (int j = 0; j < TradeStructure[i].RightItems.Count; j++)
                {
                    TradeStructure[i].RightItems[j].ItemPrice = ComputePrice(TradeStructure[i].RightItems[j].ItemName);
                }
                SendStructToUI(TradeStructure[i]);
            }
        }

        private string GetCustomXNode(string html, string xName, string xClass)
        {
            HtmlDocument resultat = new HtmlDocument();
            string source = WebUtility.HtmlDecode(html);
            resultat.LoadHtml(source);
            List<HtmlNode> tofTitleList = resultat.DocumentNode.Descendants().Where(
                                x => (
                                    x.Name == xName &&
                                    x.Attributes["class"] != null && x.Attributes["class"].Value.Contains(xClass))).ToList();

            List<HtmlNode> xx = tofTitleList.ToList();
            return tofTitleList[0].InnerHtml;
        }

        private List<HtmlNode> GetCustomSimpleNode(string html, string AB)
        {
            HtmlDocument resultat = new HtmlDocument();
            List<HtmlNode> tofTitleList;
            string source = WebUtility.HtmlDecode(html);
            resultat.LoadHtml(source);
            tofTitleList = resultat.DocumentNode.Descendants().Where(
                                x => (
                                    x.Name == AB)).ToList();
            return tofTitleList;
        }

        private string GetHREFContents(string html)
        {
            HtmlDocument resultat = new HtmlDocument();
            List<HtmlNode> tofTitleList;
            string source = WebUtility.HtmlDecode(html);
            resultat.LoadHtml(source);
            tofTitleList = resultat.DocumentNode.Descendants().Where(
                x => (
                        x.Name == "a")).ToList();

            return tofTitleList[0].OuterHtml.Substring(9,17);
        }

        private List<HtmlNode> ParseHTMLFromString(string html) // gets all trades
        {
            HtmlDocument resultat = new HtmlDocument();
            string source = WebUtility.HtmlDecode(html);
            List<HtmlNode> tofTitleList;
            resultat.LoadHtml(source);
            tofTitleList = resultat.DocumentNode.Descendants().Where(
                                x => (
                                    x.Name == "article" &&
                                    x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("standard") && x.Id =="tradelist")).ToList();
            return tofTitleList;
        }

        private string ReplaceSpecialChars(string input)
        {
            string output = input;
            //output.Replace("â˜… ", "%E2%98%85%20");
            output = output.Replace(" ", "%20");
            output = output.Replace("|", "%7C");
            output = output.Replace("â", "%E2");
            output = output.Replace("„", "%84");
            output = output.Replace("¢", "%A2");
            output = output.Replace("˜", "%98");
            output = output.Replace("…", "%85");
            output = output.Replace("'", "%27");
            return output;
        }
        private string ReplaceWithAPI(string input)
        {
            string output = input;
            output = output.Replace("listings/730/", "priceoverview/?appid=730&currency=3&market_hash_name=");
            //Console.WriteLine(output);
            return output;
        }
        private float GetPriceFromAPI(string name)
        {
            float price = 0;
            //string downloadedString;
            /*JObject jObject;
            try
            {
                downloadedString = DownloadAPI(link);
                jObject = JObject.Parse(downloadedString);
                try
                {
                    price = (float)(decimal.Parse(jObject["median_price"].ToString().Replace("€", ""), System.Globalization.NumberStyles.Currency)) / 100;
                }
                catch(Exception e)
                {
                    Console.WriteLine("[EXCEPTION 1][Parser.cs][GetPriceFromApi]:: " + e.Message + "\n, continuing to 2nd try");
                    try
                    {
                        price = (float)(decimal.Parse(jObject["lowest_price"].ToString().Replace("€", ""), System.Globalization.NumberStyles.Currency)) / 100;
                    }
                    catch(Exception e2)
                    {
                        Console.WriteLine("[EXCEPTION 2][Parser.cs][GetPriceFromApi]:: " + e2.Message);
                        price = -2;
                    }
                }
            }
            catch(Exception e3)
            {
                Console.WriteLine("[EXCEPTION 3][Parser.cs][GetPriceFromApi]:: " + e3.Message);
                downloadedString = null;
            }*/
            try
            {
                price = (float)(decimal.Parse(NameAndPrice[name].ToString()));
            }
            catch
            {
                price = -4;
            }
            return price;
        }

        private string DownloadAPI(string link)
        {
            //need to set timers
            /*WebClient client = new WebClient();
            client.Proxy = new WebProxy();
            string stringToReturn = null;
            stringToReturn = client.DownloadString(link);
            Thread.Sleep(1000);*/
            
            string stringToReturn = null;
            WebClient client = new WebClient();
            try
            {
                WebProxy prox = IP_Proxy.GetNextProxy();
                Console.WriteLine("[Parser.cs] Trying with proxy " + prox.Address);
                client.Proxy = prox;
                Task task = Task.Run(() =>
                {
                    stringToReturn = client.DownloadString(link);
                });
                if(task.Wait(TimeSpan.FromSeconds(10)))
                {
                    Console.WriteLine("[Parser.cs] Succeded with proxy " + prox.Address);
                }
                else
                {
                    Console.WriteLine("[IF Failed][Parser.cs]:: Proxy " + prox.Address + " failed (1st try)(took too long)");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[EXCEPTION][Parser.cs][DownloadAPI]:: " + e.Message + ", retrying");
                try
                {
                    Console.WriteLine("[Parser.cs] Removing proxy and retrying");
                    client.Proxy = new WebProxy();
                    Task task = Task.Run(() =>
                    {
                        stringToReturn = client.DownloadString(link);
                    });
                    if (task.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Console.WriteLine("[Parser.cs] Succeded without proxy ");
                    }
                    else
                    {
                        Console.WriteLine("[IF Failed][Parser.cs]:: Failed without proxy (took too long)");
                    }

                }
                catch(Exception e2)
                {
                    Console.WriteLine("[EXCEPTION][Parser.cs][DownloadAPI]:: " + e2.Message);
                }
            }
            return stringToReturn;
        }

        private string GetName(string tradeThing)
        {
            return GetCustomSimpleNode(tradeThing, "a")[0].InnerText;
        }
        private string GetLink(string tradeThing)
        {
            return GetHREFContents(tradeThing);
        }
    }
}
