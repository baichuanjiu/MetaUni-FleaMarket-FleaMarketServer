using Market.API.Filters;
using Market.API.MinIO;
using Market.API.MongoDBServices.Mission;
using Market.API.Protos.BriefUserInfo;
using Market.API.Redis;
using Market.API.ReusableClass;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Market.API.Controllers.Mission
{
    public class PostMediaMetadata
    {
        public PostMediaMetadata()
        {
        }

        public PostMediaMetadata(IFormFile file, double aspectRatio, IFormFile? previewImage, int? timeTotal)
        {
            File = file;
            AspectRatio = aspectRatio;
            PreviewImage = previewImage;
            TimeTotal = timeTotal;
        }

        public IFormFile File { get; set; }
        public double AspectRatio { get; set; }
        public IFormFile? PreviewImage { get; set; }
        public int? TimeTotal { get; set; }
    }
    public class PostMissionRequestData 
    {
        public PostMissionRequestData()
        {
        }

        public PostMissionRequestData(string type, string title, string description, List<PostMediaMetadata>? medias, Dictionary<string, string>? labels, PriceData priceData, string? campus, ChannelData channelData)
        {
            Type = type;
            Title = title;
            Description = description;
            Medias = medias;
            Labels = labels;
            PriceData = priceData;
            Campus = campus;
            ChannelData = channelData;
        }

        public string Type { get; set; } // sell purchase
        public string Title { get; set; }
        public string Description { get; set; }
        public List<PostMediaMetadata>? Medias { get; set; }
        public Dictionary<string, string>? Labels { get; set; }
        public PriceData PriceData { get; set; }
        public string? Campus { get; set; }
        public ChannelData ChannelData { get; set; }
    }
    public class PostMissionResponseData 
    {
        public PostMissionResponseData(string id)
        {
            Id = id;
        }

        public string Id { get; set; }
    }
    public class BriefMissionDataForClient 
    {
        public BriefMissionDataForClient(Models.Mission.Mission mission,ReusableClass.BriefUserInfo user)
        {
            Id = mission.Id!;
            Type = mission.Type;
            User = user;
            Title = mission.Title;
            if (mission.Medias.Count == 0)
            {
                Cover = null;
            }
            else 
            {
                Cover = mission.Medias[0];
            }
            PriceData = mission.PriceData;
            Campus = mission.Campus;
            Tags = mission.Labels.Values.ToList();
            CreatedTime = mission.CreatedTime;
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public ReusableClass.BriefUserInfo User { get; set; }
        public string Title { get; set; }
        public MediaMetadata? Cover { get; set; }
        public PriceData PriceData { get; set; }
        public string? Campus { get; set; }
        public List<string> Tags { get; set; }
        public DateTime CreatedTime { get; set; }
    }
    public class MissionDataForClient 
    {
        public MissionDataForClient(Models.Mission.Mission mission,ReusableClass.BriefUserInfo user) 
        {
            Id = mission.Id!;
            Type = mission.Type;
            ChannelData = mission.ChannelData;
            User = user;
            Title = mission.Title;
            Description = mission.Description;
            Medias = mission.Medias;
            Labels = mission.Labels;
            PriceData = mission.PriceData;
            Campus = mission.Campus;
            IsCompleted = mission.IsCompleted;
            IsDeleted = mission.IsDeleted;
            CreatedTime = mission.CreatedTime;
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public ChannelData ChannelData { get; set; }
        public ReusableClass.BriefUserInfo User { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<MediaMetadata> Medias { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public PriceData PriceData { get; set; }
        public string? Campus { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedTime { get; set;}
    }
    public class GetBriefMissionsResponseData 
    {
        public GetBriefMissionsResponseData(List<BriefMissionDataForClient> dataList)
        {
            DataList = dataList;
        }

        public List<BriefMissionDataForClient> DataList { get; set; }
    }
    public class GetMissionResponseData 
    {
        public GetMissionResponseData(MissionDataForClient data)
        {
            Data = data;
        }

        public MissionDataForClient Data { get; set; }
    }
    public class SearchMissionsByKeyRequestData 
    {
        public SearchMissionsByKeyRequestData(string searchKey, string type, DateTime? lastDateTime, string? lastId)
        {
            SearchKey = searchKey;
            Type = type;
            LastDateTime = lastDateTime;
            LastId = lastId;
        }

        public string SearchKey { get; set; }
        public string Type { get; set; }
        public DateTime? LastDateTime { get; set; }
        public string? LastId { get; set; }
    }
    public class SearchMissionsByChannelRequestData
    {
        public SearchMissionsByChannelRequestData(ChannelData channelData, string type, DateTime? lastDateTime, string? lastId)
        {
            ChannelData = channelData;
            Type = type;
            LastDateTime = lastDateTime;
            LastId = lastId;
        }

        public ChannelData ChannelData { get; set; }
        public string Type { get; set; }
        public DateTime? LastDateTime { get; set; }
        public string? LastId { get; set; }
    }

    [ApiController]
    [Route("/mission")]
    [ServiceFilter(typeof(JWTAuthFilterService), IsReusable = true)]
    public class MissionController : Controller
    {
        //依赖注入
        private readonly IConfiguration _configuration;
        private readonly MissionService _missionService;
        private readonly RedisConnection _redisConnection;
        private readonly MissionMediasMinIOService _missionMediasMinIOService;
        private readonly GetBriefUserInfo.GetBriefUserInfoClient _rpcUserClient;
        private readonly ILogger<MissionController> _logger;

        public MissionController(IConfiguration configuration, MissionService missionService, RedisConnection redisConnection, MissionMediasMinIOService missionMediasMinIOService, GetBriefUserInfo.GetBriefUserInfoClient rpcUserClient, ILogger<MissionController> logger)
        {
            _configuration = configuration;
            _missionService = missionService;
            _redisConnection = redisConnection;
            _missionMediasMinIOService = missionMediasMinIOService;
            _rpcUserClient = rpcUserClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostMission([FromForm] PostMissionRequestData formData,[FromHeader] string JWT, [FromHeader] int UUID) 
        {
            formData.Labels ??= new();
            formData.Medias ??= new();

            //不检查 ChannelData
            if (!(formData.Type == "sell" || formData.Type == "purchase")) 
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，原因为发布类型错误，疑似正绕过前端进行操作。", UUID);
                ResponseT<string> postMissionFailed = new(2, "发布失败，发布类型错误");
                return Ok(postMissionFailed);
            }

            if (IsStringEmpty(formData.Title) || IsStringEmpty(formData.Description)) 
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，原因为标题或描述为空，疑似正绕过前端进行操作。", UUID);
                ResponseT<string> postMissionFailed = new(3, "发布失败，标题或描述不能为空");
                return Ok(postMissionFailed);
            }

            if (formData.Medias.Count > 9)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，原因为用户上传了超过限制数量的文件，疑似正绕过前端进行操作。", UUID);
                ResponseT<string> postMissionFailed = new(4, "发布失败，上传文件数超过限制");
                return Ok(postMissionFailed);
            }

            for (int i = 0; i < formData.Medias.Count; i++)
            {
                if ((!formData.Medias[i].File.ContentType.Contains("image") && !formData.Medias[i].File.ContentType.Contains("video")) || (formData.Medias[i].File.ContentType.Contains("video") && (formData.Medias[i].PreviewImage == null || (formData.Medias[i].PreviewImage != null && !formData.Medias[i].PreviewImage!.ContentType.Contains("image")))))
                {
                    _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，原因为用户上传了图片或视频以外的媒体文件，疑似正绕过前端进行操作。", UUID);
                    ResponseT<string> postMissionFailed = new(5, "发布失败，禁止上传规定格式以外的文件");
                    return Ok(postMissionFailed);
                }
            }

            List<Task<bool>> tasks = new();
            List<MediaMetadata> medias = new();
            List<string> paths = new();
            for (int i = 0; i < formData.Medias.Count; i++)
            {
                if (formData.Medias[i].File.ContentType.Contains("image"))
                {
                    IFormFile file = formData.Medias[i].File;

                    string extension = Path.GetExtension(file.FileName);

                    Stream stream = file.OpenReadStream();

                    string timestamp = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds.ToString().Replace(".", "");

                    string fileName = timestamp + extension;

                    paths.Add(fileName);

                    string url = _configuration["MinIO:MissionMediasURLPrefix"]! + fileName;

                    tasks.Add(_missionMediasMinIOService.UploadImageAsync(fileName, stream));

                    medias.Add(new MediaMetadata("image", url, formData.Medias[i].AspectRatio, null, null));
                }
                else if (formData.Medias[i].File.ContentType.Contains("video"))
                {
                    IFormFile file = formData.Medias[i].File;

                    string extension = Path.GetExtension(file.FileName);

                    Stream stream = file.OpenReadStream();

                    string timestamp = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds.ToString().Replace(".", "");

                    string fileName = timestamp + extension;

                    paths.Add(fileName);

                    string url = _configuration["MinIO:MissionMediasURLPrefix"]! + fileName;

                    tasks.Add(_missionMediasMinIOService.UploadVideoAsync(fileName, stream));

                    IFormFile preview = formData.Medias[i].PreviewImage!;

                    string previewExtension = Path.GetExtension(preview.FileName);

                    Stream previewStream = preview.OpenReadStream();

                    string previewTimestamp = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds.ToString().Replace(".", "");

                    string previewFileName = previewTimestamp + previewExtension;

                    paths.Add(previewFileName);

                    string previewURL = _configuration["MinIO:MissionMediasURLPrefix"]! + previewFileName;

                    tasks.Add(_missionMediasMinIOService.UploadImageAsync(previewFileName, previewStream));

                    medias.Add(new MediaMetadata("video", url, formData.Medias[i].AspectRatio, previewURL, formData.Medias[i].TimeTotal));
                }
            }

            Task.WaitAll(tasks.ToArray());
            bool isStoreMediasSucceed = true;
            foreach (var task in tasks)
            {
                if (!task.Result)
                {
                    isStoreMediasSucceed = false;
                    break;
                }
            }
            if (!isStoreMediasSucceed)
            {
                _ = _missionMediasMinIOService.DeleteFilesAsync(paths);
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，MinIO存储媒体文件时发生错误。", UUID);
                ResponseT<string> postMissionFailed = new(6, "发生错误，发布失败");
                return Ok(postMissionFailed);
            }

            Models.Mission.Mission mission = new(null,formData.Type,UUID,formData.Title,formData.Description,medias,formData.Labels,formData.PriceData,formData.ChannelData,formData.Campus,DateTime.Now,false,false);
            try
            {
                await _missionService.CreateAsync(mission);
            }
            catch (Exception ex)
            {
                _ = _missionMediasMinIOService.DeleteFilesAsync(paths);
                _logger.LogWarning("Warning：用户[ {UUID} ]发布Mission时失败，将数据[ {mission} ]存入数据库时发生错误，报错信息为[ {ex} ]。", UUID, mission, ex);
                ResponseT<string> postMissionFailed = new(7, "发生错误，发布失败");
                return Ok(postMissionFailed);
            }

            ResponseT<PostMissionResponseData> postMissionSucceed = new(0, "发布成功", new(mission.Id!));
            return Ok(postMissionSucceed);
        }

        [HttpGet("brief/{type}/{lastDateTime?}/{lastId?}")]
        public async Task<IActionResult> GetBriefMissionsByLastResult([FromRoute] string type,[FromRoute] DateTime? lastDateTime, [FromRoute] string? lastId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            if (!(type == "sell" || type == "purchase")) 
            {
                type = "sell";
            }

            List<Models.Mission.Mission> missions = await _missionService.GetMissionsByLastResultAsync(type,lastDateTime, lastId);

            GetBriefMissionsResponseData getBriefMissionsResponseData = new(AssembleBriefMissionsData(missions));
            ResponseT<GetBriefMissionsResponseData> getBriefMissionsSucceed = new(0, "获取成功", getBriefMissionsResponseData);
            return Ok(getBriefMissionsSucceed);
        }

        [HttpGet("details/{missionId}")]
        public async Task<IActionResult> GetMissionDetails([FromRoute] string missionId, [FromHeader] string JWT, [FromHeader] int UUID) 
        {
            Models.Mission.Mission? mission = await _missionService.GetMissionNotCompletedAndNotDeletedByIdAsync(missionId);

            if (mission == null)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在查询不存在的Mission[ {missionId} ]的信息。", UUID, missionId);
                ResponseT<string> getMissionFailed = new(2, "您正在对不存在或已删除的数据进行查询");
                return Ok(getMissionFailed);
            }

            GetMissionResponseData getMissionResponseData = new(new(mission,await GetBriefUserInfoAsync(mission.User)));
            ResponseT<GetMissionResponseData> getMissionSucceed = new(0, "获取成功", getMissionResponseData);
            return Ok(getMissionSucceed);
        }

        //关键词搜索
        [HttpPost("search/key")]
        public async Task<IActionResult> SearchMissionsByKey([FromBody] SearchMissionsByKeyRequestData searchRequestData, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            if (!(searchRequestData.Type == "sell" || searchRequestData.Type == "purchase"))
            {
                searchRequestData.Type = "sell";
            }

            var searchKeys = Regex.Split(searchRequestData.SearchKey, " +").ToList();
            searchKeys.RemoveAll(key => key == "");

            if (searchKeys.Count == 0)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]在查询Missions时传递了不合法的参数[ {searchRequestData} ]，疑似正绕过前端进行操作。", UUID, searchRequestData);
                ResponseT<string> searchMissionsFailed = new(2, "查询失败，查询关键词不可为空");
                return Ok(searchMissionsFailed);
            }
            else
            {
                List<Models.Mission.Mission> missions = await _missionService.Search(searchKeys,searchRequestData.Type,searchRequestData.LastDateTime,searchRequestData.LastId);

                GetBriefMissionsResponseData getBriefMissionsResponseData = new(AssembleBriefMissionsData(missions));
                ResponseT<GetBriefMissionsResponseData> getBriefMissionsSucceed = new(0, "查询成功", getBriefMissionsResponseData);
                return Ok(getBriefMissionsSucceed);
            }
        }

        //频道搜索
        [HttpPost("search/channel")]
        public async Task<IActionResult> SearchMissionsByChannel([FromBody] SearchMissionsByChannelRequestData searchRequestData, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            if (!(searchRequestData.Type == "sell" || searchRequestData.Type == "purchase"))
            {
                searchRequestData.Type = "sell";
            }

            List<Models.Mission.Mission> missions = await _missionService.Search(searchRequestData.ChannelData, searchRequestData.Type, searchRequestData.LastDateTime, searchRequestData.LastId);

            GetBriefMissionsResponseData getBriefMissionsResponseData = new(AssembleBriefMissionsData(missions));
            ResponseT<GetBriefMissionsResponseData> getBriefMissionsSucceed = new(0, "查询成功", getBriefMissionsResponseData);
            return Ok(getBriefMissionsSucceed);
        }

        [HttpGet("me/history/{type}/{lastDateTime?}/{lastId?}")]
        public async Task<IActionResult> GetMyMissionsByLastResult([FromRoute] string type, [FromRoute] DateTime? lastDateTime, [FromRoute] string? lastId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            if (!(type == "sell" || type == "purchase"))
            {
                type = "sell";
            }

            List<Models.Mission.Mission> missions = await _missionService.GetMyMissionsByLastResultAsync(type, lastDateTime, lastId,UUID);

            GetBriefMissionsResponseData getBriefMissionsResponseData = new(AssembleBriefMissionsData(missions));
            ResponseT<GetBriefMissionsResponseData> getBriefMissionsSucceed = new(0, "获取成功", getBriefMissionsResponseData);
            return Ok(getBriefMissionsSucceed);
        }

        [HttpDelete("{missionId}")]
        public async Task<IActionResult> DeleteMissionById([FromRoute] string missionId, [FromHeader] string JWT, [FromHeader] int UUID)
        {
            Models.Mission.Mission? mission = await _missionService.GetMissionByIdAsync(missionId);

            if (mission == null || mission.IsDeleted)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在删除不存在或已被删除的Mission[ {missionId} ]。", UUID, missionId);
                ResponseT<string> deleteMissionFailed = new(2, "您正在对一个不存在或已被删除的记录进行删除");
                return Ok(deleteMissionFailed);
            }

            if (mission.User != UUID)
            {
                _logger.LogWarning("Warning：用户[ {UUID} ]正在删除一个不属于该用户的Mission[ {missionId} ]。", UUID, missionId);
                ResponseT<string> deleteMissionFailed = new(3, "您正在对一个不属于您的记录进行删除");
                return Ok(deleteMissionFailed);
            }

            //if (mission.Medias.Count != 0)
            //{
            //    List<string> paths = new();
            //    foreach (var medias in mission.Medias)
            //    {
            //        paths.Add(medias.URL.Replace(_configuration["MinIO:MissionMediasURLPrefix"]!, ""));
            //    }
            //    _ = _missionMediasMinIOService.DeleteFilesAsync(paths);
            //}

            _ = _missionService.DeleteAsync(missionId);

            ResponseT<bool> deleteMissionSucceed = new(0, "删除成功", true);
            return Ok(deleteMissionSucceed);
        }

        private static bool IsStringEmpty(string s) 
        {
            var split = Regex.Split(s, " +").ToList();
            split.RemoveAll(key => key == "");

            return split.Count == 0;
        }

        private List<BriefMissionDataForClient> AssembleBriefMissionsData(List<Models.Mission.Mission> missions)
        {
            if (missions.Count == 0)
            {
                return new();
            }

            IDatabase briefUserInfoRedis = _redisConnection.GetBriefUserInfoDatabase();
            var briefUserInfoBatch = briefUserInfoRedis.CreateBatch();
            Dictionary<int, Task<RedisValue>> briefUserInfoDictionary = new();

            foreach (var mission in missions)
            {
                if (!briefUserInfoDictionary.ContainsKey(mission.User))
                {
                    briefUserInfoDictionary.Add(mission.User, briefUserInfoBatch.StringGetAsync(mission.User.ToString()));
                }
            }
            briefUserInfoBatch.Execute();
            briefUserInfoBatch.WaitAll(briefUserInfoDictionary.Values.ToArray());

            GetBriefUserInfoMapRequest request = new();
            foreach (var mission in missions)
            {
                if (briefUserInfoDictionary[mission.User].Result == RedisValue.Null)
                {
                    request.QueryList.Add(mission.User);
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

            List<BriefMissionDataForClient> dataList = new();
            for (int i = 0; i < missions.Count; i++)
            {
                dataList.Add(new(missions[i], briefUserInfoMap[missions[i].User]));
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
