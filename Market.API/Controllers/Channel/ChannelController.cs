using Market.API.Filters;
using Market.API.ReusableClass;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers.Channel
{
    [ApiController]
    [Route("/channel")]
    [ServiceFilter(typeof(JWTAuthFilterService), IsReusable = true)]
    public class ChannelController : Controller
    {
        //依赖注入
        private readonly ILogger<ChannelController> _logger;

        public ChannelController(ILogger<ChannelController> logger)
        {
            _logger = logger;
        }

        private static readonly Dictionary<string, List<string>> _channelsMap = new()
        {
            { "书本",new(){ "课堂教材","考试教辅","课外读物","其它"} },
            { "数码",new(){ "耳机","手机平板","游戏光盘","电脑硬件","电脑外设", "摄影器材","智能设备","小型配件","其它"} },
            { "运动",new(){ "篮球","足球","网球","羽毛球","乒乓球","其它"} },
            { "交通",new(){ "电动车","自行车","平衡车","其它"} },
            { "生活",new(){ "日用","电器","工具","收纳","其它"} },
            { "服饰",new(){ "衣物","鞋子","包包","发饰","饰品","其它"} },
            { "票券",new(){ "表演","电影","赛事","漫展", "活动", "优惠券","其它"} },
            { "周边",new(){ "影碟","手办","玩偶","画集","小型谷子","其它"} },
            { "毕业季",new(){ "电动车","显示器","其它"} },
            { "其它",new(){ } },
        };

        [HttpGet("all")]
        public IActionResult GetAllChannels([FromHeader] string JWT, [FromHeader] int UUID) 
        {
            return Ok(new ResponseT<Dictionary<string, List<string>>>(0,"获取成功",_channelsMap));
        }

        [HttpGet("all/main")]
        public IActionResult GetAllMainChannels([FromHeader] string JWT, [FromHeader] int UUID)
        {
            return Ok(new ResponseT<List<string>>(0, "获取成功", _channelsMap.Keys.ToList()));
        }
    }
}
