using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CynosureScraper
{
    class Engine
    {
        private const string SERVER_ADDR = "https://www.cynosure.com";
        private const string RESULT_ADDR = "/results";

        private string mCountry;
        private string mZipCode;
        private string mOutputPath;

        public bool ProcessItem(string url)
        {
            // Name, Street Address, City, State, Zip Ciode, Country, Phone, Website
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            HtmlNode rootNode = doc.DocumentNode;
            HtmlNode tmpNode;
            string name, website = string.Empty, street, city = string.Empty, state = string.Empty, zipcode = string.Empty;

            tmpNode = rootNode.SelectSingleNode("//h1[contains(@class, 'subheader') and contains(@class, 'title')]");
            if (null == tmpNode) return false;
            name = tmpNode.InnerText.Trim();

            tmpNode = rootNode.SelectSingleNode("//a[contains(@class, 'practice-website-url')]");
            if (null != tmpNode) website = tmpNode.Attributes["href"].Value;

            tmpNode = rootNode.SelectSingleNode("//div[@id='location']/div[contains(@class, 'row')]/div[contains(@class, 'col-4')]/p");
            if (null == tmpNode) return false;
            string innerText = tmpNode.InnerHtml.Trim();
            string[] arr = innerText.Split(new string[]{"<br>"}, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length < 2) return false;
            street = arr[0].Trim();

            arr = arr[1].Trim().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length > 0) city = arr[0];
            if (arr.Length > 1) state = arr[1];
            if (arr.Length > 2) zipcode = arr[2];

            tmpNode = tmpNode.SelectSingleNode(".//a");
            if (null == tmpNode) return false;
            string phone = tmpNode.InnerText.Trim();

            using (StreamWriter sw = File.AppendText(mOutputPath))
            {
                string line = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\"",
                    name, street, city, state, zipcode, mCountry, phone, website);
                sw.WriteLine(line);
            }

            return true;
        }

        public void Process(string zipCode, string country, int distance, int[] treatmentList, string outputPath, ItemAdded callback)
        {
            string url = SERVER_ADDR + RESULT_ADDR + "?country=" + country + "&zipcode=" + zipCode;

            mCountry = country;
            mZipCode = zipCode;
            mOutputPath = outputPath;

            if (distance > 0)
                url += "&proximity=" + distance;

            for (int i = 0; i < treatmentList.Length; ++i)
                url += "&treatment-id%5B%5D=" + treatmentList[i];

            url += "&campaign-code=default&session-id=default";

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            HtmlNode rootNode = doc.DocumentNode;

            HtmlNodeCollection collection = rootNode.SelectNodes("//div[contains(@class, 'provider-item')]");

            if (null != collection)
            {
                foreach (HtmlNode item in collection)
                {
                    HtmlNode link = item.SelectSingleNode(".//a[contains(@class, 'logo')]");
                    if (ProcessItem(link.Attributes["href"].Value))
                        callback();
                }
            }
        }
    }
}
