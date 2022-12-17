using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Timers;
using Telegram.Bot;
using zingoy.Model;

namespace zingoy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ZingoyPriceTrackerController : ControllerBase
    {
        static System.Timers.Timer timer = new System.Timers.Timer();
        private readonly static string zingoyCleartripGCUrl = "https://www.zingoy.com/gift-cards/cleartrip";
        private readonly static string headerMediaType = "application/json";
        private readonly static string headerKeyAuthority = "authority";
        private readonly static string headerValueAuthority = "www.zingoy.com";
        private readonly static string headerKeyAccept = "accept";
        private readonly static string headerValueAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        private readonly static string apiTokenCleartripVoucherAlert = "5789194115:AAGtKf1vCr6dDbp-CiEG7qy5JOBHXGbL15w";
        private readonly static string chatIdCleartripVoucherAlert = "-1001682879422";
        
        
        public ZingoyPriceTrackerController()
        {

        }

        [Route("getZingo")]
        [HttpGet]
        public async Task GetZingoyPrice([FromQuery] InputModel inputModel)
        {  
            timer.Elapsed += (sender, e) => GetDisc(sender, e, inputModel.DiscountRate, inputModel.HeaderCookie);
            timer.Interval = inputModel.Interval * 1000;
            timer.Enabled = true;
            GC.KeepAlive(timer);
        }
        public async static void GetDisc(object? source, ElapsedEventArgs e, decimal rate, string headerCookie)
        {
            List<VoucherCost> voucherList = new List<VoucherCost>();
            List<HtmlNode> programmerLinks = new List<HtmlNode>();
            int pageNum = 0;
            bool breakLoop = false;

            try
            {
                HttpClient client = new HttpClient();

                #region --- client to fetch GC clearttrip ---

                client.BaseAddress = new Uri(zingoyCleartripGCUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(headerMediaType));
                client.DefaultRequestHeaders.Add(headerKeyAuthority, headerValueAuthority);
                client.DefaultRequestHeaders.Add(headerKeyAccept, headerValueAccept);
                #endregion --- client to fetch GC clearttrip ---               

                
                do
                {
                    ++pageNum;
                    
                    HttpResponseMessage response = new HttpResponseMessage();
                    string responseRead = string.Empty;
                    programmerLinks = new List<HtmlNode>();
                    response = await client.GetAsync($"{zingoyCleartripGCUrl}?&page={pageNum}&sort_by=discount");
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

                        int startIndex = item.InnerHtml.IndexOf("product_id");
                        int endIndex = 0;
                        int diff = "product_id".Length + 1;
                        string productid = string.Empty;
                        if (startIndex > 0)
                        {
                            endIndex = item.InnerHtml.IndexOf(">", startIndex);
                        }
                           
                        if (startIndex > 0 && startIndex < endIndex)
                        {
                            productid = item.InnerHtml.Substring(startIndex + diff, endIndex - startIndex - diff - 1);
                        }
                        if (voucherCostList != null)
                        {
                            voucherCost = voucherCostList[1]?.InnerText;
                            cashbackRate = voucherCostList[0]?.InnerText;
                        }
                        voucherValue = voucherValue?.Replace("\n", "").Replace(",", "").Replace("₹", "");
                        voucherCost = voucherCost?.Replace("\n", "").Replace(",", "").Replace("₹", "");
                        cashbackRate = cashbackRate?.Replace("\n", "").Replace("%", "").Trim();
                        var discountRate = string.IsNullOrEmpty(cashbackRate) ? 0 : Decimal.Parse(cashbackRate);
                        voucherList.Add(new VoucherCost
                        {
                            VoucherValue = voucherValue,
                            VoucherCosting = voucherCost,
                            DiscountRate = discountRate,
                            PageNum = pageNum,
                            ProductId = productid
                        });
                        if(discountRate < rate && !string.IsNullOrEmpty(productid))
                        {
                            breakLoop = true;
                            break;
                        }
                    }

                } while (programmerLinks?.Count >= 10 && pageNum < 6 && !breakLoop);

                if (voucherList.Any(s => s.DiscountRate >= rate))
                {
                    var eligibleVoucher = voucherList.Where(s => s.DiscountRate >= rate).ToList();
                    string message = string.Empty;
                    foreach (var item in eligibleVoucher)
                    {
                        message = message + $"{item.VoucherValue} is availabe at {item.DiscountRate}% on page num {item.PageNum} \n";
                        if (!string.IsNullOrEmpty(headerCookie))
                        {
                            try
                            {
                                HttpClientHandler handler = new HttpClientHandler();
                                var urladd = $"https://www.zingoy.com/add_to_cart?from_page=buy_gift_cards&product_id={item.ProductId}";

                                HttpClient clientForAddCart = new HttpClient();
                                clientForAddCart.BaseAddress = new Uri(urladd);
                                clientForAddCart.DefaultRequestHeaders.Accept.Clear();
                                clientForAddCart.DefaultRequestHeaders.Add("authority", "www.zingoy.com");
                                clientForAddCart.DefaultRequestHeaders.Add("accept", "*/*;q=0.5, text/javascript, application/javascript, application/ecmascript, application/x-ecmascript");
                                clientForAddCart.DefaultRequestHeaders.Add("cookie", headerCookie);
                                HttpResponseMessage responseForAddCart = new HttpResponseMessage();
                                var requestForAddCart = new HttpRequestMessage(HttpMethod.Post, clientForAddCart.BaseAddress);
                                responseForAddCart = await clientForAddCart.SendAsync(requestForAddCart);
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }
                        }
                    }
                    var linkToBuy = "https://www.zingoy.com/gift-cards/cleartrip?=1666083329714&page=1&sort_by=discount";
                    var bot = new TelegramBotClient(apiTokenCleartripVoucherAlert);
                    var s = await bot.SendTextMessageAsync(chatIdCleartripVoucherAlert, message + "\n \n" + linkToBuy);
                    
                }
            }
            catch (Exception ex)
            {
                voucherList.Add(new VoucherCost
                {
                    VoucherValue = ex.Message,
                    VoucherCosting = "a"
                });
            }

        }
    }
}