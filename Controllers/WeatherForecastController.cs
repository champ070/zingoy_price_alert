using Google.Protobuf.WellKnownTypes;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Timers;
using Telegram.Bot;
using zingoy.Model;

namespace zingoy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private int count= 0;
        public WeatherForecastController()
        {
            
        }

        [Route("getZingo")]
        [HttpGet]
        public async Task GetZingoyPrice()
        {
            GetDisc();
            //System.Timers.Timer timer = new System.Timers.Timer();
            //timer.Elapsed += GetDisc;

            //timer.Interval = 10000;
            //timer.Enabled = true;
            //if(count > 5)
            //{
            //    timer.Stop();
            //    timer.Start();
            //}
            //GC.KeepAlive(timer);
            //var resp =  await GetDisc();
            //return resp;

        }

        [Route("getZingoy")]
        [HttpGet]
        public async void GetDisc(/*object source, ElapsedEventArgs e*/)
        {
            bool runInfinite = true;

            while (runInfinite)
            {
                List<VoucherCost> voucherList = new List<VoucherCost>();
                try
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("https://www.zingoy.com/gift-cards/cleartrip");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("authority", "www.zingoy.com");
                    client.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                    List<HtmlNode> programmerLinks = new List<HtmlNode>();

                    int pageNum = 0;
                    do
                    {
                        ++pageNum;
                        HttpResponseMessage response = new HttpResponseMessage();
                        string responseRead = string.Empty;
                        programmerLinks = new List<HtmlNode>();

                        response = await client.GetAsync($"https://www.zingoy.com/gift-cards/cleartrip?page={pageNum}");
                        responseRead = await response.Content.ReadAsStringAsync();

                        HtmlDocument htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(responseRead);
                        programmerLinks = htmlDoc.DocumentNode.Descendants("div")
                                .Where(node => node.GetAttributeValue("class", "").Contains("shadow5 bgwhite pr mb10")).ToList();
                        foreach (var item in programmerLinks)
                        {

                            string? voucherCost = string.Empty;
                            string? cashbackRate = string.Empty;
                            var voucherValue = item.SelectSingleNode(".//div[@class='pt20 grid-10 tablet-grid-15 roboto-medium 100-zingcash grid-parent']")?.InnerText;
                            var voucherCostList = (item.SelectNodes(".//div[@class='pt20 p5 grid-15 tablet-grid-10 roboto-medium']"));
                            if (voucherCostList != null)
                            {
                                voucherCost = voucherCostList[1]?.InnerText;
                                cashbackRate = voucherCostList[0]?.InnerText;
                            }
                            voucherValue = voucherValue?.Replace("\n", "").Replace(",", "").Replace("₹", "");
                            voucherCost = voucherCost?.Replace("\n", "").Replace(",", "").Replace("₹", "");
                            cashbackRate = cashbackRate?.Replace("\n", "").Replace("%", "").Trim();
                            cashbackRate = string.IsNullOrEmpty(cashbackRate) ? "" : cashbackRate?.Substring(0, cashbackRate.IndexOf("."));
                            voucherList.Add(new VoucherCost
                            {
                                VoucherValue = voucherValue,
                                VoucherCosting = voucherCost,
                                DiscountRate = string.IsNullOrEmpty(cashbackRate) ? 0 : Int32.Parse(cashbackRate),
                                PageNum = pageNum
                            });
                        }

                    } while (programmerLinks?.Count >= 10);

                    if (voucherList.Any(s => s.DiscountRate >= 15))
                    {
                        string apiToken = "5789194115:AAGtKf1vCr6dDbp-CiEG7qy5JOBHXGbL15w";
                        //string urlString = $"https://api.telegram.org/bot{apiToken}/sendMessage?chat_id={"-1001682879422"}&text={"test"}";
                        var eligibleVoucher = voucherList.Where(s => s.DiscountRate >= 15).ToList();
                        string message = string.Empty;
                        foreach (var item in eligibleVoucher)
                        {
                            message = message + $"{item.VoucherValue} is availabe at {item.DiscountRate}% on page num {item.PageNum} \n";
                        }
                        var linkToBuy = "https://www.zingoy.com/gift-cards/cleartrip?=1666083329714&page=1&sort_by=discount";
                        var bot = new TelegramBotClient(apiToken);
                        var s = await bot.SendTextMessageAsync("-1001682879422", message + "\n \n" + linkToBuy);
                    }
                    if (voucherList.Any(s => s.DiscountRate >= 10))
                    {
                        string apiToken = "5789194115:AAGtKf1vCr6dDbp-CiEG7qy5JOBHXGbL15w";
                        //string urlString = $"https://api.telegram.org/bot{apiToken}/sendMessage?chat_id={"-1001682879422"}&text={"test"}";
                        var eligibleVoucher = voucherList.Where(s => s.DiscountRate >= 10).ToList();
                        string message = string.Empty;
                        foreach (var item in eligibleVoucher)
                        {
                            message = message + $"---------------{item.VoucherValue} is availabe at {item.DiscountRate}% on page num {item.PageNum} \n";
                        }
                        var linkToBuy = "https://www.zingoy.com/gift-cards/cleartrip?=1666083329714&page=1&sort_by=discount";
                        var bot = new TelegramBotClient(apiToken);
                        var s = await bot.SendTextMessageAsync("-816145363", message + "\n \n" + linkToBuy);
                    }
                    //return voucherList;
                }
                catch (Exception ex)
                {
                    voucherList.Add(new VoucherCost
                    {
                        VoucherValue = ex.Message,
                        VoucherCosting = "a"
                    });
                    //return voucherList;
                }
            }
            
        }
    }
}