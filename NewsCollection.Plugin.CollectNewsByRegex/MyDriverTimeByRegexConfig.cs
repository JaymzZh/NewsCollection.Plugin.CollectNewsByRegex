using System;
using System.ComponentModel;
using NewLife.Xml;

namespace NewsCollection.Plugin.CollectNewsByRegex
{
    [DisplayName("历史数据采集时间")]
    [XmlConfigFile("Config/MyDriverTimeConfig.config")]
    public class MyDriverTimeByRegexConfig : XmlConfig<MyDriverTimeByRegexConfig>
    {
        #region 属性

        private DateTime? _dealTime;
        /// <summary>历史数据采集时间点</summary>
        [DisplayName("历史数据采集时间点")]
        public DateTime DealTime
        {
            get
            {
                if (_dealTime == null) _dealTime = DateTime.Now;
                return _dealTime.Value;
            }
            set { _dealTime = value; }
        }

        private bool? _isDealt;
        /// <summary>是否已处理</summary>
        [DisplayName("是否已处理")]
        public bool IsDealt
        {
            get
            {
                if (_isDealt == null) _isDealt = false;
                return _isDealt.Value;
            }
            set { _isDealt = value; }
        }

        private string _keywords;
        /// <summary>采集关键字，多个时用'|'隔开</summary>
        [DisplayName("采集关键字，多个时用'|'隔开")]
        public string Keywords
        {
            get
            {
                if (_keywords == null) _keywords = string.Empty;
                return _keywords;
            }
            set { _keywords = value; }
        }

        #endregion
    }
}
