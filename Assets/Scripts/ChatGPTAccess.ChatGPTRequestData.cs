namespace ChatGPT
{
    public partial class ChatGPTAccess
    {
        [System.Serializable]
        public class ChatGPTRequestData
        {
            public string model;
            public ContentData[] messages;
            //该参数代表最终回复的string的最大长度，默认为16，此时无法回复完整的内容，因此建议将其设置较大的数值
            public int max_tokens;

            //后续的参数可选
            /*public float temperature;
            public int top_p;
            public int n;
            public bool stream;
            public int logprobs;
            public string stop;*/
        }

        [System.Serializable]
        public class ContentData
        {
            public string role;
            public string content;
        }
    }


}