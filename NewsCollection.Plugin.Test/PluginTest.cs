using NewsCollection.Plugin.CollectNewsByRegex;
using Xunit;

namespace NewsCollection.Plugin.Test
{
    public class PluginTest
    {
        private readonly CollectMyDriverNews _collectMyDriverNews;
        private readonly MyDriverTimeByRegexConfig _config;

        public PluginTest()
        {
            _collectMyDriverNews = new CollectMyDriverNews();
            _config = MyDriverTimeByRegexConfig.Current;
        }

        [Fact]
        public void TestGetDriverNewsByDate()
        {
            _config.Keywords = string.Empty;
            _config.Save();

            _collectMyDriverNews.GetNewContent("http://news.mydrivers.com/1/464/464111.htm");
            Assert.True(_collectMyDriverNews.IsCollect);
        }

        [Fact]
        public void TestGetDriverNewsByDateByRightKeywords()
        {
            _config.Keywords = "唯品会|茅台酒";
            _config.Save();

            _collectMyDriverNews.GetNewContent("http://news.mydrivers.com/1/464/464111.htm");
            Assert.True(_collectMyDriverNews.IsCollect);
        }

        [Fact]
        public void TestGetDriverNewsByDateByWrongKeywords()
        {
            _config.Keywords = "千元机|安卓";
            _config.Save();

            _collectMyDriverNews.GetNewContent("http://news.mydrivers.com/1/464/464111.htm");
            Assert.True(!_collectMyDriverNews.IsCollect);
        }
    }
}
