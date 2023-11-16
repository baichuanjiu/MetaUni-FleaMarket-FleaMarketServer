using Market.API.Filters;
using Market.API.MinIO;
using Market.API.MongoDBServices.Mission;
using Market.API.MongoDBServices.Profit;
using Market.API.MongoDBServices.Record;
using Market.API.Protos.BriefUserInfo;
using Market.API.Redis;
using Market.API.ReusableClass;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace Market.API.Controllers.Record
{
    public class PostRecordRequestData 
    {
        public PostRecordRequestData()
        {
        }

        public PostRecordRequestData(string missionId, double price, string remark)
        {
            MissionId = missionId;
            Price = price;
            Remark = remark;
        }

        public string MissionId { get; set; }
        public double Price { get; set; }
        public string Remark { get; set; }
    }
    public class PostRecordResponseData
    {
        public PostRecordResponseData(string id)
        {
            Id = id;
        }

        public string Id { get; set; }
    }
    public class BriefRecordDataForClient
    {
        public BriefRecordDataForClient(Models.Record.Record record, ReusableClass.BriefUserInfo user)
        {
            Id = record.Id!;
            Type = record.Type;
            User = user;
            Title = record.Title;
            if (record.Medias.Count == 0)
            {
                Cover = null;
            }
            else
            {
                Cover = record.Medias[0];
            }
            Campus = record.Campus;
            Tags = record.Labels.Values.ToList();
            CreatedTime = record.CreatedTime;
            Price = record.Price;
            Remark = record.Remark;
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public ReusableClass.BriefUserInfo User { get; set; }
        public string Title { get; set; }
        public MediaMetadata? Cover { get; set; }
        public string? Campus { get; set; }
        public List<string> Tags { get; set; }
        public DateTime CreatedTime { get; set; }
        public double Price { get; set; }
        public string Remark { get; set; }
    }
    public class RecordDataForClient
    {
        public RecordDataForClient(Models.Record.Record record, ReusableClass.BriefUserInfo user)
        {
            Id = record.Id!;
            Type = record.Type;
            User = user;
            Title = record.Title;
            Description = record.Description;
            Medias = record.Medias;
            Labels = record.Labels;
            Campus = record.Campus;
            IsDeleted = record.IsDeleted;
            CreatedTime = record.CreatedTime;
            Price = record.Price;
            Remark = record.Remark;
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public ReusableClass.BriefUserInfo User { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<MediaMetadata> Medias { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public string? Campus { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedTime { get; set; }
        public double Price { get; set; }
        public string Remark { get; set; }
    }
    public class GetBriefRecordsResponseData
    {
        public GetBriefRecordsResponseData(List<BriefRecordDataForClient> dataList)
        {
            DataList = dataList;
        }

        public List<BriefRecordDataForClient> DataList { get; set; }
    }
    public class GetRecordResponseData
    {
        public GetRecordResponseData(RecordDataForClient data)
        {
            Data = data;
        }

        public RecordDataForClient Data { get; set; }
    }

    [ApiController]
    [Route("/record")]
    [ServiceFilter(typeof(JWTAuthFilterService), IsReusable = true)]
    public class RecordController : Controller
    {
        //依赖注入
        private readonly IConfiguration _configuration;
        private readonly MissionService _missionService;
        private readonly RecordService _recordService;
        private readonly ProfitService _profitService;
        private readonly RedisConnection _redisConnection;
        private readonly MissionMediasMinIOService _missionMediasMinIOService;
        private readonly GetBriefUserInfo.GetBriefUserInfoClient _rpcUserClient;
        private readonly ILogger<RecordController> _logger;

        public RecordController(IConfiguration configuration, MissionService missionService, RecordService recordService, ProfitService profitService, RedisConnection redisConnection, MissionMediasMinIOService missionMediasMinIOService, GetBriefUserInfo.GetBriefUserInfoClient rpcUserClient, ILogger<RecordController> logger)
        {
            _configuration = configuration;
            _missionService = missionService;
            _recordService = recordService;
            _profitService = profitService;
            _redisConnection = redisConnection;
            _missionMediasMinIOService = missionMediasMinIOService;
            _rpcUserClient = rpcUserClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostRecord([FromForm] PostRecordRequestData formData, [FromHeader] string JWT, [FromHeader] int UUID) 
        {
            if (formData.Price < 0) 
            {
                formData.Price = 0;
            }

            Models.Mission.Mission? mission = await _missionService.GetMissionByIdAsync(formData.MissionId);

            if (mission == null || mission.IsCompleted || mission.IsDeleted)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在尝试对不存在或已被删除的Mission[ {missionId} ]进行完成操作。", UUID, formData.MissionId);
                ResponseT<string> postRecordFailed = new(2, "您正在对一个不存在或已被删除的记录进行操作");
                return Ok(postRecordFailed);
            }

            if (mission.User != UUID)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在尝试完成一个不属于该用户的Mission[ {missionId} ]。", UUID, formData.MissionId);
                ResponseT<string> postRecordFailed = new(3, "您正在对一个不属于您的记录进行操作");
                return Ok(postRecordFailed);
            }

            Models.Record.Record record = new(null,mission.Type,mission.User,mission.Title,mission.Description,mission.Medias,mission.Labels,mission.Campus,DateTime.Now,formData.Price,formData.Remark,false);
            try
            {
                _ = _missionService.CompleteAsync(mission.Id!);
                await _recordService.CreateAsync(record);
                if (record.Type == "sell")
                {
                    _ = _profitService.ComputeMyIncomeAsync(formData.Price,UUID);
                }
                else if (record.Type == "purchase") 
                {
                    _ = _profitService.ComputeMyExpenditureAsync(formData.Price, UUID);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Record时失败，将数据[ {record} ]存入数据库时发生错误，报错信息为[ {ex} ]。", UUID, record, ex);
                ResponseT<string> postRecordFailed = new(4, "发生错误，操作失败");
                return Ok(postRecordFailed);
            }

            ResponseT<PostRecordResponseData> postRecordSucceed = new(0, "发布成功", new(record.Id!));
            return Ok(postRecordSucceed);
        }

        [HttpGet("details/{recordId}")]
        public async Task<IActionResult> GetRecordDetails([FromRoute] string recordId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            Models.Record.Record? record = await _recordService.GetRecordNotCompletedAndNotDeletedByIdAsync(recordId);

            if (record == null)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在查询不存在的Record[ {recordId} ]的信息。", UUID, recordId);
                ResponseT<string> getRecordFailed = new(2, "您正在对不存在或已删除的数据进行查询");
                return Ok(getRecordFailed);
            }

            GetRecordResponseData getRecordResponseData = new(new(record, await GetBriefUserInfoAsync(record.User)));
            ResponseT<GetRecordResponseData> getRecordSucceed = new(0, "获取成功", getRecordResponseData);
            return Ok(getRecordSucceed);
        }

        [HttpGet("me/history/{type}/{lastDateTime?}/{lastId?}")]
        public async Task<IActionResult> GetMyRecordsByLastResult([FromRoute] string type, [FromRoute] DateTime? lastDateTime, [FromRoute] string? lastId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            if (!(type == "sell" || type == "purchase"))
            {
                type = "sell";
            }

            List<Models.Record.Record> records = await _recordService.GetMyRecordsByLastResultAsync(type, lastDateTime, lastId, UUID);

            GetBriefRecordsResponseData getBriefRecordsResponseData = new(AssembleBriefRecordsData(records));
            ResponseT<GetBriefRecordsResponseData> getBriefRecordsSucceed = new(0, "获取成功", getBriefRecordsResponseData);
            return Ok(getBriefRecordsSucceed);
        }

        [HttpDelete("{recordId}")]
        public async Task<IActionResult> DeleteRecordById([FromRoute] string recordId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            Models.Record.Record? record = await _recordService.GetRecordByIdAsync(recordId);

            if (record == null || record.IsDeleted)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在删除不存在或已被删除的Record[ {recordId} ]。", UUID, recordId);
                ResponseT<string> deleteRecordFailed = new(2, "您正在对一个不存在或已被删除的记录进行删除");
                return Ok(deleteRecordFailed);
            }

            if (record.User != UUID)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在删除一个不属于该用户的Record[ {recordId} ]。", UUID, recordId);
                ResponseT<string> deleteRecordFailed = new(3, "您正在对一个不属于您的记录进行删除");
                return Ok(deleteRecordFailed);
            }

            //if (record.Medias.Count != 0)
            //{
            //    List<string> paths = new();
            //    foreach (var medias in record.Medias)
            //    {
            //        paths.Add(medias.URL.Replace(_configuration["MinIO:RecordMediasURLPrefix"]!, ""));
            //    }
            //    _ = _recordMediasMinIOService.DeleteFilesAsync(paths);
            //}

            if (record.Type == "sell")
            {
                _ = _profitService.ComputeMyIncomeAsync(-record.Price, UUID);
            }
            else if (record.Type == "purchase")
            {
                _ = _profitService.ComputeMyExpenditureAsync(-record.Price, UUID);
            }

            _ = _recordService.DeleteAsync(recordId);

            ResponseT<bool> deleteRecordSucceed = new(0, "删除成功", true);
            return Ok(deleteRecordSucceed);
        }

        private List<BriefRecordDataForClient> AssembleBriefRecordsData(List<Models.Record.Record> records)
        {
            if (records.Count == 0)
            {
                return new();
            }

            IDatabase briefUserInfoRedis = _redisConnection.GetBriefUserInfoDatabase();
            var briefUserInfoBatch = briefUserInfoRedis.CreateBatch();
            Dictionary<int, Task<RedisValue>> briefUserInfoDictionary = new();

            foreach (var record in records)
            {
                if (!briefUserInfoDictionary.ContainsKey(record.User))
                {
                    briefUserInfoDictionary.Add(record.User, briefUserInfoBatch.StringGetAsync(record.User.ToString()));
                }
            }
            briefUserInfoBatch.Execute();
            briefUserInfoBatch.WaitAll(briefUserInfoDictionary.Values.ToArray());

            GetBriefUserInfoMapRequest request = new();
            foreach (var record in records)
            {
                if (briefUserInfoDictionary[record.User].Result == RedisValue.Null)
                {
                    request.QueryList.Add(record.User);
                }
            }

            Dictionary<int, ReusableClass.BriefUserInfo> briefUserInfoMap = new();
            if (request.QueryList.Count != 0)
            {
                GetBriefUserInfoMapReply reply = _rpcUserClient.GetBriefUserInfoMap(request);
                var briefUserInfoCacheBatch = briefUserInfoRedis.CreateBatch();

                foreach (KeyValuePair<int, Protos.BriefUserInfo.BriefUserInfo> entry in reply.BriefUserInfoMap)
                {
                    _ = briefUserInfoCacheBatch.StringSetAsync(entry.Key.ToString(), JsonSerializer.Serialize(new ReusableClass.BriefUserInfo(entry.Value)), TimeSpan.FromMinutes(15));
                    briefUserInfoMap.Add(entry.Key, new ReusableClass.BriefUserInfo(entry.Value));
                }

                briefUserInfoCacheBatch.Execute();
            }

            foreach (var entry in briefUserInfoDictionary)
            {
                if (entry.Value.Result != RedisValue.Null)
                {
                    briefUserInfoMap.Add(entry.Key, JsonSerializer.Deserialize<ReusableClass.BriefUserInfo>(entry.Value.Result.ToString())!);
                }
            }

            List<BriefRecordDataForClient> dataList = new();
            for (int i = 0; i < records.Count; i++)
            {
                dataList.Add(new(records[i], briefUserInfoMap[records[i].User]));
            }

            return dataList;
        }

        private async Task<ReusableClass.BriefUserInfo> GetBriefUserInfoAsync(int UUID)
        {
            IDatabase briefUserInfoRedis = _redisConnection.GetBriefUserInfoDatabase();

            var briefUserInfoCache = await briefUserInfoRedis.StringGetAsync(UUID.ToString());

            if (briefUserInfoCache.IsNull)
            {
                GetBriefUserInfoSingleRequest request = new()
                {
                    UUID = UUID,
                };
                var reply = _rpcUserClient.GetBriefUserInfoSingle(request);
                _ = briefUserInfoRedis.StringSetAsync(UUID.ToString(), JsonSerializer.Serialize(new ReusableClass.BriefUserInfo(reply.BriefUserInfo)), TimeSpan.FromMinutes(15));
                return new(reply.BriefUserInfo);
            }
            else
            {
                return JsonSerializer.Deserialize<ReusableClass.BriefUserInfo>(briefUserInfoCache.ToString())!;
            }
        }
    }
}
