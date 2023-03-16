using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ChatGPT
{
    public delegate void GetChatResponseCallback(string msg);

    public partial class ChatGPTAccess : MonoBehaviour
    {
        //UI相关
        private TMP_Text textQuestion, textAnswer;
        private TMP_InputField popupInputField, chatInputField;
        private Transform popupDialogTrans, popupDialogBgTrans;
        private float tweenDuration = 0.3f;

        private string requestMsg;
        private string chatGPTUrl = "https://api.openai.com/v1/chat/completions";
        private string chatGPTApiKey = "YOUR OWN API KEY";

        private event GetChatResponseCallback m_GetChatResponseCallback;

        void Start() {
            InitUIComponent();

            m_GetChatResponseCallback += OnGetChatResponseCallback;
        }

        void InitUIComponent() {
            //主界面
            var textQuestionGo = GameObject.Find("TextQuestion (TMP)");
            textQuestion = textQuestionGo.GetComponent<TMP_Text>();
            var textAnswerGo = GameObject.Find("TextAnswer (TMP)");
            textAnswer = textAnswerGo.GetComponent<TMP_Text>();
            var chatInputFieldGo = GameObject.Find("ChatInputField (TMP)");
            chatInputField = chatInputFieldGo.GetComponent<TMP_InputField>();
            var buttonSend = GameObject.Find("ButtonSend").GetComponent<Button>();
            buttonSend.onClick.AddListener(OnButtonSendClick);

            #region 弹窗界面的内容，暂时不用
            //“咨询弹窗”界面
            /*var buttonChat = GameObject.Find("ButtonChat").GetComponent<Button>();
            buttonChat.onClick.AddListener(OnButtonChatClick);
            var popupDialogPrefab = Resources.Load("Prefabs/PopupDialog") as GameObject;
            popupDialogTrans = Instantiate(popupDialogPrefab, textQuestionGo.transform.root).transform;
            popupDialogTrans.localPosition = Vector3.zero;
            popupDialogTrans.localScale = Vector3.zero;
            popupDialogBgTrans = popupDialogTrans.Find("ImageBg");
            //点击外部区域时关闭该弹窗
            var popupDialogCloseBtn = popupDialogTrans.GetComponent<Button>();
            popupDialogCloseBtn.onClick.AddListener(ClosePopupDialog);
            //获取输入框的内容
            var popupInputFieldTrans = popupDialogTrans.Find("ImageBg/InputField (TMP)");
            popupInputField = popupInputFieldTrans.GetComponent<TMP_InputField>();
            //点击“Ok”按钮开始发送请求
            var buttonOkTrans = popupDialogTrans.Find("ImageBg/ButtonOK");
            var buttonOk = buttonOkTrans.GetComponent<Button>();
            buttonOk.onClick.AddListener(OnButtonOKClick);*/
            #endregion
        }

        IEnumerator GetChatGPTResponse() {
            var contentData = new ContentData() {
                role = "user",
                content = requestMsg
            };
            ContentData[] dataArray = {contentData};
            ChatGPTRequestData requestData = new ChatGPTRequestData() {
                model = "gpt-3.5-turbo",
                messages = dataArray,
                //该参数限定了回复的最大字符数量(为了显示完整的内容，该数值尽量设置大些)
                max_tokens = 1000,
            };

            UnityWebRequest request = new UnityWebRequest(chatGPTUrl, "POST");
            string jsonStr = JsonUtility.ToJson(requestData);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonStr));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", chatGPTApiKey));

            yield return request.SendWebRequest();

            Debug.LogFormat("ResultCode: {0}", request.responseCode);
            string result = "";
            if (request.responseCode == 200) {
                string msg = request.downloadHandler.text;
                //将返回的json字符串换行输出到控制台中(便于查看)
                //注意：这里需要将“已经是json字符串但不具备换行的json”反序列，之后加入“Formatting.Indented”使其具备“换行和缩进”的功能
                var jsonFormat = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(msg),
                    Formatting.Indented);
                Debug.LogFormat("{0}", jsonFormat);

                //直接获取json字符串中某个属性的value
                JObject jsonObject = JObject.Parse(msg);
                string id = jsonObject.GetValue("id").ToString();
                Debug.LogFormat("{0}", id);

                //将json字符串转换成指定类型对象后再获取其中的具体参数值
                ChatGPTResponse response = JsonUtility.FromJson<ChatGPTResponse>(msg);
                ChatGPTResponse.ChoiceContent[] contents = response.choices;
                if (contents.Length > 0) {
                    //去除字符串开头的”\n\n“换行符
                    result = contents[0].message.content.TrimStart('\n');
                    Debug.LogFormat("{0}", result);
                }
            }
            else {
                StringBuilder sb = new StringBuilder("解析失败：");
                sb.Append(string.Format("ResponseCode: {0}", request.responseCode));
                result = sb.ToString();
            }

            if (m_GetChatResponseCallback != null)
                m_GetChatResponseCallback(result);

            //释放该对象占用的资源(如果没有释放，Unity中会报错)
            request.Dispose();
        }

        void OnGetChatResponseCallback(string msg) {
            StringBuilder sb = new StringBuilder();
            sb.Append(msg);
            textAnswer.text = sb.ToString();
            ResizeRectTransform(textAnswer);
        }

        void OnButtonSendClick() {
            if (string.IsNullOrEmpty(chatInputField.text)) {
                return;
            }

            requestMsg = chatInputField.text;
            textQuestion.text = requestMsg;
            textAnswer.text = "等待回应中......";
            ResizeRectTransform(textQuestion);
            ResizeRectTransform(textAnswer);
            chatInputField.text = "";
            StartCoroutine(GetChatGPTResponse());
        }

        //重置文本框内容使其可自由滑动(content的高度会自动改变)
        void ResizeRectTransform(TMP_Text text) {
            var preferredHeight = text.preferredHeight;
            var textRectTransform = text.rectTransform;
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }

        #region 弹窗界面的内容，暂时不用
        //点击“咨询弹窗”的“OK”按钮
        void OnButtonOKClick() {
            if (string.IsNullOrEmpty(popupInputField.text)) {
                return;
            }

            //开始发送请求
            requestMsg = popupInputField.text;
            StringBuilder sb = new StringBuilder();
            sb.Append(requestMsg);
            textQuestion.text = sb.ToString();
            textAnswer.text = "等待回应中......";
            ResizeRectTransform(textQuestion);
            ResizeRectTransform(textAnswer);
            popupInputField.text = "";
            ClosePopupDialog();

            //开始协程并等待回应
            StartCoroutine(GetChatGPTResponse());
        }

        //点击“聊天”按钮打开“咨询弹窗”页面
        void OnButtonChatClick() {
            popupDialogBgTrans.DOScale(Vector3.one, tweenDuration).onComplete = () => {
                popupDialogTrans.localScale = Vector3.one;
            };
        }

        //点击“咨询弹窗”外部区域时关闭该界面
        void ClosePopupDialog() {
            popupDialogBgTrans.DOScale(Vector3.zero, tweenDuration).onComplete = () => {
                popupDialogTrans.localScale = Vector3.zero;
            };
        }
        #endregion
    }
}

