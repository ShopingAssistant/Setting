using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VOL.WebApi.Manager
{
    public  class SpiderHelper
    {

        public static CookieContainer cookiesContainer { get; set; }//定义Cookie容器


        /// <summary>
        /// 异步创建爬虫
        /// </summary>
        /// <param name="uri">爬虫URL地址</param>
        /// <param name="proxy">代理服务器</param>
        /// <returns>网页源代码</returns>
        public static async Task<string> Start(string url, string proxy = null)
        {
            var uri = new Uri(url); 

            return await Task.Run(() =>
            {
                var pageSource = string.Empty;
                try
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    request.Accept = "*/*";
                    request.ServicePoint.Expect100Continue = false;//加快载入速度
                    request.ServicePoint.UseNagleAlgorithm = false;//禁止Nagle算法加快载入速度
                    request.AllowWriteStreamBuffering = false;//禁止缓冲加快载入速度
                    request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");//定义gzip压缩页面支持
                    request.ContentType = "application/x-www-form-urlencoded";//定义文档类型及编码
                    request.AllowAutoRedirect = false;//禁止自动跳转
                    //设置User-Agent，伪装成Google Chrome浏览器
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
                    request.Timeout = 5000;//定义请求超时时间为5秒
                    request.KeepAlive = true;//启用长连接
                    request.Method = "GET";//定义请求方式为GET         
                    if (proxy != null) request.Proxy = new WebProxy(proxy);//设置代理服务器IP，伪装请求地址
                    //request.CookieContainer = cookiesContainer;//附加Cookie容器
                    request.ServicePoint.ConnectionLimit = int.MaxValue;//定义最大连接数

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {//获取请求响应

                        //foreach (Cookie cookie in response.Cookies) this.cookiesContainer.Add(cookie);//将Cookie加入容器，保存登录状态

                        if (response.ContentEncoding.ToLower().Contains("gzip"))//解压
                        {
                            using (GZipStream stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                            {
                                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GB2312")))
                                {
                                    pageSource = reader.ReadToEnd();
                                }
                            }
                        }
                        else if (response.ContentEncoding.ToLower().Contains("deflate"))//解压
                        {
                            using (DeflateStream stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress))
                            {
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    pageSource = reader.ReadToEnd();
                                }

                            }
                        }
                        else
                        {
                            using (Stream stream = response.GetResponseStream())//原始
                            {
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {

                                    pageSource = reader.ReadToEnd();
                                }
                            }
                        }
                    }
                    request.Abort();
                    watch.Stop();
                    //var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;//获取当前任务线程ID
                    var milliseconds = watch.ElapsedMilliseconds;//获取请求执行时间
                   
                }
                catch (Exception ex)
                {

                }
                return pageSource;
            });
        }


        /// <summary>
        /// HtmlAgilityPack 版
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> StartByAgility(string url)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument document = await htmlWeb.LoadFromWebAsync(url);
            HtmlNodeCollection nodeCollection = document.DocumentNode.SelectNodes(@"//table/tr/td/a[@href]");  //代表获取所有
            string name = document.DocumentNode.SelectNodes(@"//meta[@name='keywords']")[0].GetAttributeValue("content", "").Split(',')[0];
            //foreach (var node in nodeCollection)
            //{
            //    HtmlAttribute attribute = node.Attributes["href"];
            //    String val = attribute.Value;  //章节url
            //    var title = htmlWeb.Load(val).DocumentNode.SelectNodes(@"//h1")[0].InnerText;  //文章标题
            //    var doc = htmlWeb.Load(val).DocumentNode.SelectNodes(@"//dd[@id='contents']");
            //    var content = doc[0].InnerHtml.Replace("&nbsp;", "").Replace("<br>", "\r\n");  //文章内容
            //                                                                                   //txt文本输出
            //    string path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/") + "Txt/";
            //    //Novel(title + "\r\n" + content, name, path);
            //}
            return "";
        }


        public static async Task<string> HttpGet(string url,string id)
        {
            /** 京东商品sku详情
             * 1.秒杀 https://item.jd.com/65475811217.html
             * 2.精选 plus价格  https://item.jd.com/2218521.html
             * 3.海外购 https://item.jd.hk/40682842338.html
             **/


            try
            {
                ChromeDriver driver = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory.ToString());
                driver.Navigate().GoToUrl(url);

                //var modelHtmlList = driver.FindElementsByXPath("//meta[@name='keywords']");
               
                JDProductSkuModel model = new JDProductSkuModel() { JDID = id};

                model.Name = driver.FindElementsByXPath("//div[@class='sku-name']")?.FirstOrDefault()?.Text;
                //价格
                model.SeckillPrice = driver.FindElementsByXPath($"//span[@class='price J-p-{id}']")?.FirstOrDefault()?.Text;
                model.OriginPrice = driver.FindElementsByXPath("//del[@id='page_origin_price']")?.FirstOrDefault()?.Text;
                model.PlusPrice = driver.FindElementsByXPath($"//span[@class='price J-p-p-{id}']")?.FirstOrDefault()?.Text;


                #region  image

                model.image = driver.FindElementsByXPath($"//div[@id='spec-n1']/img")?.FirstOrDefault()?.GetAttribute("src");
                if (string.IsNullOrWhiteSpace(model.image))
                {
                    model.image = driver.FindElementsByXPath($"//img[@id='spec-img']")?.FirstOrDefault()?.GetAttribute("src");
                }


                #endregion




                driver.Quit();


                return JsonConvert.SerializeObject(model);
            }
            catch (Exception ex)
            {
                throw;
            }

            return "";
        }

       
    }



    public  class JDProductSkuModel 
    {
        public string JDID { get; set; }

        public string image { get; set; }

        public string Name { get; set; }

        public string SeckillPrice { get; set; }

        public string OriginPrice { get; set; }

        public string PlusPrice { get; set; }
    }
}
