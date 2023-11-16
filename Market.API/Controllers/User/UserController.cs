using Market.API.Filters;
using Market.API.MongoDBServices.Profit;
using Market.API.Protos.ChatRequest;
using Market.API.Redis;
using Market.API.ReusableClass;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Market.API.Controllers.User
{
    public class SendChatRequestRequestData
    {
        public SendChatRequestRequestData(string title, int targetUser, string greetText)
        {
            Title = title;
            TargetUser = targetUser;
            GreetText = greetText;
        }

        public string Title { get; set; }
        public int TargetUser { get; set; }
        public string GreetText { get; set; }
    }

    [ApiController]
    [Route("/user")]
    [ServiceFilter(typeof(JWTAuthFilterService), IsReusable = true)]
    public class UserController : Controller
    {
        //依赖注入
        private readonly ILogger<UserController> _logger;
        private readonly ProfitService _profitService;
        private readonly RedisConnection _redisConnection;
        private readonly SendChatRequest.SendChatRequestClient _rpcChatRequestClient;

        public UserController(ILogger<UserController> logger, ProfitService profitService, RedisConnection redisConnection, SendChatRequest.SendChatRequestClient rpcChatRequestClient)
        {
            _logger = logger;
            _profitService = profitService;
            _redisConnection = redisConnection;
            _rpcChatRequestClient = rpcChatRequestClient;
        }

        //向某人发送私聊请求
        [HttpPost("chatRequest")]
        public async Task<IActionResult> SendChatRequest([FromBody] SendChatRequestRequestData requestData, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            IDatabase database = _redisConnection.GetChatRequestDatabase();

            //无法向自己发送私聊请求
            if (requestData.TargetUser == UUID)
            {
                ResponseT<string> sendChatRequestFailed = new(2, "发送私聊请求失败，您无法向自己发送私聊请求");
                return Ok(sendChatRequestFailed);
            }

            //同一用户在六十分钟内无法受到来自相同用户的多次私聊请求
            if (database.KeyExists($"{UUID}SendChatRequestTo{requestData.TargetUser}"))
            {
                ResponseT<string> sendChatRequestFailed = new(3, "发送私聊请求失败，您向该用户发送私聊请求的操作太过频繁");
                return Ok(sendChatRequestFailed);
            }

            //发送RPC请求
            SendChatRequestSingleRequest request = new()
            {
                SenderUUID = UUID,
                TargetUUID = requestData.TargetUser,
                GreetText = $"来自《{requestData.Title}》：{requestData.GreetText}",
                MessageText = "来自中古的私聊请求",
            };

            GeneralReply reply = await _rpcChatRequestClient.SendChatRequestSingleAsync(
                          request);

            switch (reply.Code)
            {
                case 0:
                    {
                        _ = database.StringSetAsync($"{UUID}SendChatRequestTo{requestData.TargetUser}", "", expiry: TimeSpan.FromMinutes(60));
                        ResponseT<string> sendChatRequestSucceed = new(0, "发送私聊请求成功");
                        return Ok(sendChatRequestSucceed);
                    }
                default:
                    {
                        ResponseT<string> sendChatRequestFailed = new(4, reply.Message);
                        return Ok(sendChatRequestFailed);
                    }
            }
        }

        [HttpGet("income")]
        public async Task<IActionResult> GetMyIncome([FromHeader] string JWT, [FromHeader] int UUID) 
        {
            var profit = await _profitService.GetMyProfitAsync(UUID);

            double income;
            if (profit == null)
            {
                income = 0;
            }
            else 
            {
                income = profit.Income;
            }
            return Ok(new ResponseT<double>(0,"获取成功",income));
        }

        [HttpGet("expenditure")]
        public async Task<IActionResult> GetMyExpenditure([FromHeader] string JWT, [FromHeader] int UUID)
        {
            var profit = await _profitService.GetMyProfitAsync(UUID);

            double expenditure;
            if (profit == null)
            {
                expenditure = 0;
            }
            else
            {
                expenditure = profit.Expenditure;
            }
            return Ok(new ResponseT<double>(0, "获取成功", expenditure));
        }

        [HttpGet("profit")]
        public async Task<IActionResult> GetMyProfit([FromHeader] string JWT, [FromHeader] int UUID)
        {
            var profit = await _profitService.GetMyProfitAsync(UUID);

            if (profit == null)
            {
                return Ok(new ResponseT<double>(0, "获取成功", 0));
            }
            else
            {
                return Ok(new ResponseT<double>(0, "获取成功", profit.Income - profit.Expenditure));
            }
        }
    }
}
