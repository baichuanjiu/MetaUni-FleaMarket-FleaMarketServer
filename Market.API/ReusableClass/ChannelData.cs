namespace Market.API.ReusableClass
{
    public class ChannelData
    {
        public ChannelData()
        {
        }

        public ChannelData(string mainChannel, string? subChannel)
        {
            MainChannel = mainChannel;
            SubChannel = subChannel;
        }

        public string MainChannel { get; set; }
        public string? SubChannel { get; set; }
    }
}
