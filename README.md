# TraderParser
Trade Parser bot for CSGOLounge and Dota2Lounge. It parses the entire website for trades that are favorable for us. 

Ex(real scenario): their $84 CS:GO knife for our $75 knife. It basically gets all the items from all trades on the website and searches the price for that item on the steam community market returning favorable trades.

The program can search for particular items on the website instead of the entire website by checking the "Custom Lounge URL" togglebox.
On european country, the website doesn't work. This can be bypassed by checking the "Lounge Proxy?" togglebox. It will parse proxys from the internet and then test and use them to access the website. [UPDATE 2022: the website is now accessible again from European countries]

The parser will not work anymore and has been discontinued. The website has changed the layout and has also removed the trading part of the website (you can only bet on the website now).

![image](https://user-images.githubusercontent.com/9354674/195376428-9d66ad58-5c8e-4556-b84b-fc09eb720f56.png)

Yellow = Separates the left side for the right side of each trade.

Red = Separates each trade.


Favorable trades will appear in green.

Trade link = Lounge trade link.

Item Name

Item Price = Price parsed from steam community.

Item Link = Link for each item on the steam community market.
