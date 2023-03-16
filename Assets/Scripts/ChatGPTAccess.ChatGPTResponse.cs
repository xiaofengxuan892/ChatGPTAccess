namespace ChatGPT
{
    public partial class ChatGPTAccess
    {
        [System.Serializable]
        public class ChatGPTResponse
        {
            //注意：在将json自负床转换成实际的T对象时，并不需要将该json中的所有属性都匹配，只需要将需要使用的属性匹配出来即可
            public string id;
            public ChoiceContent[] choices;
            /*public int created;
            public string model;*/

            [System.Serializable]
            public class ChoiceContent
            {
                public int index;
                public ContentData message;

                //public int logprobs;
                //public string finish_reason;
            }
        }
    }
}