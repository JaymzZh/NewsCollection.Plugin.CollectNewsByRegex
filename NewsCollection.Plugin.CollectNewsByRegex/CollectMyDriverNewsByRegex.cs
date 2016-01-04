using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NewsCollection.Core;
using NewsCollention.Entity;
using XCode;

namespace NewsCollection.Plugin.CollectNewsByRegex
{
    public class CollectMyDriverNewsByRegex
    {
        /// <summary>
        /// 用于判断任务结束没
        /// </summary>
        public bool IsDealing = false;

        /// <summary>
        /// 是否采集
        /// </summary>
        public bool IsCollectOrNot = false;

        /// <summary>
        /// 基本内容正则匹配
        /// </summary>
        private static readonly Regex BaseRegex = new Regex(@"
<div\sclass=""pce_lb"".*?>
    <div\sclass=""pce_lb1"">
        <a\shref=""(?<url>.*?)"".*?>(?<title>.*?)</a>
        <ul\sclass=""hui2"">
            <li.*?>
                <a\shref=""(?<author_url>.*?)"".*?>(?<author>.*?)</a>
            </li>
            <li>(?<publishdate>.*?)</li>
        </ul>
    </div>
    <div\sclass=""pce_lb2"">
        <div\sclass=""pce_lb2_left"".*?>
            (.|\n)*?
        </div>
        <div\sclass=""pce_lb2_right\shui"">
            <p>(.|\n)*?</p>
            <div\sclass=""pce_lb2_right1\shui2"">
                <ul>
                    <li.*?>标签</li>
                    (?<tags><li.*?>.*?</li>)
                </ul>
            </div>
            <div\sid=""NewsCount_\d*?""\sclass=""pce_title4\shui2"">
                <div\sclass=pc_title4_left>
                    (.|\n)*?
                </div>
                <div\sclass=pc_title4_right>
                    (.|\n)*?
                </div>
            </div>
        </div>
    </div>
</div>
",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// 标签正则匹配
        /// </summary>
        private static readonly Regex TagRegex =
            new Regex(@"<li.*?><a\shref=""(?<tag_url>.*?)"".*?>(?<tag_name>.*?)</a></li>",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 页码Div正则匹配
        /// </summary>
        private static readonly Regex PageDivRegex = new Regex(@"
<div\sclass=""postpage"">
    (?<pages><a.*?>.*?</a>)
</div>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 页码数正则匹配
        /// </summary>
        private static readonly Regex PagesRegex = new Regex(@"<a.*?>\d?</a>",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 新闻内容正则匹配
        /// </summary>
        private static readonly Regex NewsContentRegex = new Regex(@"
<div\sclass=""news_info"".*?>
    (?<content>.*?)
<p\sclass=""jcuo1"".*?>",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.ExplicitCapture);

        /// <summary>
        /// 测评内容Div正则匹配
        /// </summary>
        private static readonly Regex PcContentDivRegex = new Regex(@"
<div\sclass=""pc_info"".*?>
    (?<content>.*?)
<p\sclass=""news_bq"".*?>",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.ExplicitCapture);

        /// <summary>
        /// 测评内容正则匹配
        /// </summary>
        private static readonly Regex PcContentRegex =
            new Regex(@"<div\sclass=""pc_dh"">.*?</div>(?<content><p.*?>.*</p>)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 测评内容页正则匹配
        /// </summary>
        private static readonly Regex PcPageRegex = new Regex(@"
<select\sname=""Split_Page"".*?>
    (?<pageOptions>.*?)
</select>",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.ExplicitCapture);

        /// <summary>
        /// 测评内容页码正则匹配
        /// </summary>
        private static readonly Regex PcPageOptionsRegex = new Regex(@"<option.*?value=""(?<url>.*?)"">.*?</option>",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// 抓取http://news.mydrivers.com/上的新闻
        /// </summary>
        public void GetDriverNewsByDate(DateTime date, int page = 1)
        {
            try
            {
                IsDealing = true;

                var urlStr =
                    $"http://news.mydrivers.com/getnewsupdatelistdata.aspx?data={date.ToString("yyyy-MM-dd")}&pageid={page}";
            
                var html = HtmlHelper.GetWebSource(urlStr, Encoding.Default);
                html = HtmlHelper.TrimOther(html);
                
                var matches = BaseRegex.Matches(html);

                foreach (Match node in matches)
                {
                    New @new = new New();
                    
                    @new.Title = node.Groups["title"].Value;

                    var url = node.Groups["url"].Value;

                    //判断该新闻是否已入库
                    var n = New.FindByUrl(url);
                    if (n != null)
                        continue;

                    @new.Content = GetNewContent(url);
                    //如果不包含关键字则不采集
                    if(!IsCollectOrNot)
                        continue;

                    @new.Url = url;

                    //查找作者信息
                    var author = AddAuthor(node);
                    @new.AuthorId = author.Id;

                    var time = $"{date.Year}-{node.Groups["publishdate"].Value}";
                    DateTime t;
                    DateTime.TryParse(time, out t);
                    @new.CreateTime = t;

                    //查找标签信息
                    var tagsMatch = node.Groups["tags"].Value;
                    var tags = AddTags(tagsMatch);
                    tags.Save();

                    @new.Save();

                    AddNewTag(@new, tags);

                    //延迟一定时间，避免IP被封
                    Thread.Sleep(1200);
                }

                GoNext(html, date, page);
            }
            catch (Exception)
            {
                IsDealing = false;
            }
        }

        private static Author AddAuthor(Match authorMatch)
        {
            var authorName = authorMatch.Groups["author"].Value;
            var author = Author.FindByName(authorName); //首先查询数据库里有该作者没，没有则添加
            if (author == null)
            {
                author = new Author {Name = authorName};
                var authorUrl = authorMatch.Groups["author_url"].Value;
                author.Url = authorUrl;
                author.Save();
            }
            return author;
        }

        private static EntityList<Tag> AddTags(string tagStr)
        {
            var tags = new EntityList<Tag>();
            var matches = TagRegex.Matches(tagStr);
            foreach (Match match in matches)
            {
                var tagName = match.Groups["tag_name"].Value;
                var tag = Tag.FindByName(tagName) ?? new Tag
                {
                    Name = tagName,
                    Url = match.Groups["tag_url"].Value
                };
                tags.Add(tag);
            }
            return tags;
        }

        private void GoNext(string html, DateTime date, int page)
        {
            Match match = PageDivRegex.Match(html);
            var pageMatch = PagesRegex.Matches(match.Value);
            var pagecount = pageMatch.Count;
            page += 1;
            if (pagecount > 0 && pagecount >= page)
            {
                //嵌套循环
                GetDriverNewsByDate(date, page);
            }
            IsDealing = false;
        }

        private static void AddNewTag(New @new, List<Tag> tags)
        {
            EntityList<NewTag> newTags = new EntityList<NewTag>();
            newTags.AddRange(tags.Select(tag => new NewTag
            {
                NewId = @new.Id, TagId = tag.Id
            }));
            newTags.Save();
        }

        /// <summary>
        /// 获取新闻内容
        /// </summary>
        /// <param name="url">新闻链接</param>
        public string GetNewContent(string url)
        {
            var html = HtmlHelper.GetWebSource(url, Encoding.Default);
            html = HtmlHelper.TrimOther(html);

            string result = string.Empty;
            
            //新闻提取
            if (NewsContentRegex.IsMatch(html))
            {
                result = NewsContentRegex.Match(html).Groups["content"].Value;
            }
            else if(PcContentDivRegex.IsMatch(html))
            {
                var matchContent = PcContentDivRegex.Match(html).Groups["content"].Value;
                result = PcContentRegex.Match(matchContent).Groups["content"].Value;
                //如果包含分页，则匹配分页继续添加内容
                if (PcPageRegex.IsMatch(html))
                {
                    result = GetPagedPcContent(url, result, html);
                }
            }
            else
            {
                IsCollectOrNot = false;
                return string.Empty;
            }

            //正则匹配，只采集包含关键字的新闻
            var regStr = MyDriverTimeByRegexConfig.Current.Keywords;
            IsCollectOrNot = string.IsNullOrWhiteSpace(regStr) || Regex.IsMatch(result, $"({regStr})");
            return result;
        }

        private static string GetPagedPcContent(string url, string result, string html)
        {
            var content = new StringBuilder(result);
            var baseUrl = url.Substring(0, url.LastIndexOf('/'));//基础网址
            var currentUrl = url.Substring(url.LastIndexOf('/') + 1);//当前网址（不含基础网址）
            var matchPage = PcPageRegex.Match(html).Groups["pageOptions"].Value;
            foreach (Match match in PcPageOptionsRegex.Matches(matchPage))
            {
                var pageUrl = match.Groups["url"].Value;
                if (currentUrl.Equals(pageUrl, StringComparison.OrdinalIgnoreCase))
                    continue;

                html = HtmlHelper.GetWebSource($"{baseUrl}/{pageUrl}", Encoding.Default);
                html = HtmlHelper.TrimOther(html);
                var matchContent = PcContentDivRegex.Match(html).Groups["content"].Value;
                content.Append(PcContentRegex.Match(matchContent).Groups["content"].Value);
                //延迟一定时间，避免IP被封
                Thread.Sleep(600);
            }

            result = content.ToString();
            return result;
        }
    }
}