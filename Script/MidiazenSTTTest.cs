//using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.Net.Sockets;
using JHchoi.Module;
using System.Text;
using System.Threading;

namespace Midiazen
{
    public class MidiazenSTTTest : IModule
    {
        #region 테스트 서버용
        /*
        class Connect
        {
            public string language;
            public bool intermediates;
            public string cmd;
        }

        WebSocket WebSocket { get; set; }
        Connect connect = new Connect();
        List<MessageEventArgs> ListMsg = new List<MessageEventArgs>();
        bool isSocketOpen = false;
        bool isSocketConnect = false;
        */
        #endregion

        //TCP 서버
        TcpClient tcpClient;
        NetworkStream netStream;
        bool isTcpConnect;
        MidiazenSettingModel Msm;
        AudioSource _audioSource;
        Thread recvThread;

        int _lastSample = 0;
        bool isRecordStart = false;
        bool isRecord;
        byte[] buffer = new byte[2048];

        protected override void OnLoadStart()
        {
            Debug.Log("STT START");
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            Msm = new MidiazenSettingModel();
            Msm.Setup();
            AddMeeage();
            SetResourceLoadComplete();
        }

        public void InitModule(MidiazenSettingModel msm)
        {
            Debug.Log("STT START");
            Msm = msm;
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            AddMeeage();
        }

        void AddMeeage()
        {
            Message.AddListener<STTRecord>(STTConnect);
        }

        void STTConnect(STTRecord msg)
        {
            StartCoroutine(SocketConnectTry());
        }

        IEnumerator SocketConnectTry()
        {
            yield return StartCoroutine(TCPConnect());
        }

        IEnumerator TCPConnect()
        {
            Log.Instance.log("TCPConnect");
            int reconnectCnt = 0;
            isTcpConnect = false;
            while (reconnectCnt < 5 && !isTcpConnect)
            {
                try
                {
                    tcpClient = new TcpClient(Msm.SdsIp, Msm.SdsPort);
                    netStream = tcpClient.GetStream();
                    isTcpConnect = true;
                    Log.Instance.log("TCPConnect Success");
                    recvThread = new Thread(new ThreadStart(RecvThread));
                    recvThread.Start();
                    StartCoroutine(CheckThread());
                }

                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("TCPConnect Fail : " + E.ToString());
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        IEnumerator TcpClientCheck()
        {
            while (true)
            {
                if(tcpClient == null)
                {
                    TCPConnect();
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        IEnumerator RecordStart()
        {
            Debug.Log("Record Start");
            _audioSource.clip = Microphone.Start(null, false, 1000, Msm.STTFrequency);

            while (isRecord)
            {
                yield return null;
                int pos = Microphone.GetPosition(null);
                int diff = pos - _lastSample;
                if (diff > 0)
                {
                    float[] samples = new float[diff * _audioSource.clip.channels];
                    _audioSource.clip.GetData(samples, _lastSample);
                    byte[] bytes = WavUtility.ConvertAudioClipDataToInt16ByteArray(samples);

                    string protocolString = "VD" + bytes.Length;
                    byte[] protocolBytes = StringToByte(protocolString);

                    Debug.Log(bytes.Length);
                    string temp1 = "VD" + bytes.Length + ByteToString(bytes);
                    bytes = StringToByte(temp1);

                    Buffer.BlockCopy(bytes, 0, protocolBytes, 0, protocolString.Length);

                    if (netStream.CanWrite)
                    {
                        try
                        {
                            netStream.Write(bytes, 0, bytes.Length);
                            
                        }
                        catch(Exception E)
                        {
                            Debug.Log(E.ToString());
                        }
                    }
                }
                _lastSample = pos;
            }
        }

        void RecvThread()
        {
            string msg;
            while (isTcpConnect)
            {
                try
                {
                    netStream.Read(buffer, 0, buffer.Length);
                    if (netStream.CanRead)
                    {
                        msg = ByteToString(buffer);
                        if (msg.Length > 0)
                            ReceiveMsgCheck(msg);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        IEnumerator CheckThread()
        {
            yield return new WaitForSeconds(3.0f);
            if (!recvThread.IsAlive)
            {
                Log.Instance.log("RecvThread Thread Die");
                try
                {
                    recvThread.Start();
                }
                catch (Exception E)
                {
                    Log.Instance.log("RecvThread Restart Fail : " + E.ToString());
                }
            }
        }

        void ReceiveMsgCheck(string msg)
        {
            Debug.Log(msg);
            if (msg.Contains("LI0005hello"))
            {
                Debug.Log("Connect Success");
                try
                {
                    if (netStream.CanWrite)
                    {
                        Debug.Log("Send Portocall ViceRecordStart");
                        netStream.Write(StringToByte("VS0000"), 0, StringToByte("VS0000").Length);
                        isRecordStart = true;
                    }
                }
                catch (Exception E)
                {
                    Debug.Log(E.ToString());
                }
            }
            else if (msg.Contains("VE0000"))
            {
                Debug.Log("음성인식 종료");
                isRecord = false;
                //isRecordStart = false;
            }
            else if (msg.Contains("JS"))
            {
                Debug.Log("STT 추출 완료");
                netStream.Close();
                tcpClient.Close();
            }
        }

        private void Update()
        {
            #region 테스트 서버용
            /*
            if (ListMsg.Count > 0)
            {
                for (int i = 0; i < ListMsg.Count; i++)
                {
                    StartCoroutine(MessageCheck(ListMsg[i]));
                    ListMsg.Remove(ListMsg[i]);
                }
            }
            */
            #endregion

            if (isRecordStart)
            {
                isRecordStart = false;
                isRecord = true;
                StartCoroutine(RecordStart());
            }
        }

        private string ByteToString(byte[] strByte) { string str = Encoding.UTF8.GetString(strByte); return str; }
        private byte[] StringToByte(string str) { byte[] StrByte = Encoding.UTF8.GetBytes(str); return StrByte; }

        private void OnDestroy()
        {
            DestorySTT();
        }

        protected override void OnUnload()
        {
            DestorySTT();
        }

        void DestorySTT()
        {
            #region 테스트 서버용
            /*
            if (WebSocket != null)
                WebSocket.Close();
            
            WebSocket = null;
            */
            #endregion

            recvThread.Abort();
            StopAllCoroutines();
            RemoveMessage();
            netStream.Close();
            tcpClient.Close();
            recvThread = null;
            tcpClient = null;
            netStream = null;
        }

        void RemoveMessage()
        {
            Message.RemoveListener<STTRecord>(STTConnect);
        }

        #region 테스트 서버 소켓 통신용
        /*
        IEnumerator SocketOpen()
        {
            Log.Instance.log("MidiazenSTT SocketOpen");
            int reconnectCnt = 0;
            while (reconnectCnt < 5 && !isSocketOpen)
            {
                try
                {
                    WebSocket = new WebSocket(Msm.STTUrl);
                    WebSocket.OnOpen += ws_Open;
                    WebSocket.OnMessage += ws_OnMessage;
                    WebSocket.OnError += ws_OnError;
                    WebSocket.OnClose += ws_Close;
                    isSocketOpen = true;
                    StartCoroutine(WebsocketNullCheck());

                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenSTT SocketOpen Fail : " + E.ToString());
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        //접속후 연결 정보 전달
        private void ws_Open(object sender, EventArgs e)
        {
            Debug.Log(e.ToString());
            StartCoroutine(SocketSend());
        }

        private void ws_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log(e.ToString());
            ListMsg.Add(e);
        }

        private void ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Debug.Log("Error : " + e.ToString());
        }

        private void ws_Close(object sender, CloseEventArgs e)
        {

        }

        IEnumerator SocketConnect()
        {
            Log.Instance.log("MidiazenSTT SocketConnect");
            int reconnectCnt = 0;
            isSocketConnect = false;
            while (reconnectCnt < 5 && !isSocketConnect)
            {
                try
                {
                    WebSocket.ConnectAsync();
                    isSocketConnect = true;
                    Log.Instance.log("MidiazenSTT SocketConnect Success");
                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenSTT SocketConnect Fail : " + E.ToString());
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        IEnumerator WebsocketNullCheck()
        {
            while (true)
            {
                yield return new WaitForSeconds(3.0f);
                if (WebSocket == null)
                {
                    Log.Instance.log("Websocket Null");
                    StartCoroutine(SocketConnectTry());
                    StopCoroutine(WebsocketNullCheck());
                }
            }
        }

        IEnumerator MessageCheck(MessageEventArgs msg)
        {
            yield return null;
            ConnectRespons connectRespons = null;
            STTResponse sTTResponse = null;

            Debug.Log(msg.Data);
            Debug.Log(msg.RawData);

            connectRespons = JsonUtility.FromJson<ConnectRespons>(msg.Data);
            sTTResponse = JsonUtility.FromJson<STTResponse>(msg.Data);

            if (connectRespons.payload.status == "ok")
            {
                Log.Instance.log("STT Recored Start");
                _audioSource.clip = Microphone.Start(null, false, 1000, Msm.Frequency);
                StartCoroutine(RecordStart());
            }
            else if (connectRespons.@event == "close")
            {
                Log.Instance.log("STT Connect Finsh");
                StopCoroutine(RecordStart());
            }

            if (sTTResponse.payload.epd)// || sTTResponse.payload.confidence >= 0.95f)
            {
                Log.Instance.log("STT Record Text : " + sTTResponse.payload.text);
                //Message.Send<STTReceiveMsg>(new STTReceiveMsg(sTTResponse.payload.text));
                StopCoroutine(RecordStart());
            }
        }
       
        IEnumerator SocketSend()
        {
            Log.Instance.log("MidiazenSTT SocketSend");
            connect.language = Msm.Language.ToString();
            connect.intermediates = Msm.Intermediates;
            connect.cmd = Msm.Cmd.ToString();
            string json = JsonUtility.ToJson(connect);
            bool isConnect = false;
            int reconnectCnt = 0;

            while (reconnectCnt < 5 && !isConnect)
            {
                try
                {
                    WebSocket.SendAsync(json, (bool a) =>
                    {
                        Debug.Log("TT");
                    });
                    isConnect = true;
                    Log.Instance.log("MidiazenSTT SocketSend Success");
                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenSTT SocketSend Fail : " + E.ToString());
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        void StartVoiceRecord(StartRecord msg)
        {
            Log.Instance.log("STT Recored Start");
            _audioSource.clip = Microphone.Start(null, false, 1000, Msm.Frequency);
            StartCoroutine(RecordStart());
        }
        */
        #endregion
    }
}
