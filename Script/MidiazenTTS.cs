using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using JHchoi.Module;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using JHchoi.Constants;

namespace Midiazen
{
    /*
     * TTS 수행
            접속된 websocket 으로 TTS를 수행할 text를 전송
            websocket 에서 수신하는 데이터는 pcm data 입니다.
            PCM 정보 :
            sample rate : 16000
            sample : 16bit
     *      channel : 1
     */
    public class MidiazenTTS : IModule
    {
        WebSocket WebSocketGirl;
        WebSocket WebSocketBoy;
        MidiazenSettingModel Msm;
        string fileName;
        string saveFileName;
        float[] _audioFloats;
        AudioSource _audioSource;
        List<float[]> ListAudioFloat = new List<float[]>();
        List<float> ListSaveAudioFloat = new List<float>();
        int reconnectTryCnt = 0;
        bool isWebSocketConnect = false;

        Coroutine corWebSocketCheck;
        Coroutine corListCountCheck;

        protected override void OnLoadStart()
        {
            Debug.Log("TTS START");
            Msm = new MidiazenSettingModel();
            Msm.Setup();
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            AddMeeage();
            StartCoroutine(SocketOpen());
        }

        public void InitModule(MidiazenSettingModel msm)
        {
            Debug.Log("TTS START");
            Msm = msm;
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            AddMeeage();
            StartCoroutine(SocketOpen());

        }

        void AddMeeage()
        {
            Message.AddListener<TTSSendMsg>(SendTTS);
            Message.AddListener<TTSSaveMsg>(TTSSave);
        }


        void SendLog(string logmsg)
        {
            Message.Send<AddLog>(new AddLog(logmsg));
        }

        IEnumerator SocketOpen()
        {
            Log.Instance.log("MidiazenTTS SocketOpen");
            bool isConnect = false;
            int reconnectCnt = 0;
            ListAudioFloat.Clear();

            while (reconnectCnt < 5 && !isConnect)
            {
                try
                {
                    //test = null;
                    WebSocketGirl = new WebSocket(Msm.TTSGirlUrl);
                    WebSocketGirl.OnMessage += ws_OnMessage;
                    WebSocketGirl.OnError += ws_OnError;

                    WebSocketBoy = new WebSocket(Msm.TTSBoyUrl);
                    WebSocketBoy.OnMessage += ws_OnMessage;
                    WebSocketBoy.OnError += ws_OnError;

                    isConnect = true;
                    corWebSocketCheck = StartCoroutine(WebsocketNullCheck());
                    corListCountCheck = StartCoroutine(ListCountCheck());

                    SetResourceLoadComplete();
                    Log.Instance.log("MidiazenTTS SocketOpen Success");
                    SendLog("MidiazenTTS SocketOpen Success");
                    break;
                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenTTS SocketOpen Fail : " + E.ToString());
                    SendLog("MidiazenTTS SocketOpen Fail");
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        IEnumerator WebsocketNullCheck()
        {
            while (true)
            {
                yield return new WaitForSeconds(3.0f);

                if (WebSocketGirl == null || WebSocketBoy == null)
                {
                    if (corWebSocketCheck != null)
                        StopCoroutine(corWebSocketCheck);
                    if (corListCountCheck != null)
                        StopCoroutine(corListCountCheck);

                    StartCoroutine(SocketOpen());
                }
            }
        }

        private void ws_OnError(object sender, ErrorEventArgs e)
        {
            Debug.Log("Error : " + e.ToString());
            ErrorCheck(e.ToString());
        }

        private void ws_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log("TTS Receive : " + e.ToString());
            _audioFloats = null;
            _audioFloats = PCMBytesToFloats(e.RawData);
            ListAudioFloat.Add(_audioFloats);
        }

        private void ws_BoyOnError(object sender, ErrorEventArgs e)
        {
            Debug.Log("Error : " + e.ToString());
            ErrorCheck(e.ToString());
        }

        private void ws_BoyOnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log("TTS Receive : " + e.ToString());
            _audioFloats = null;
            _audioFloats = PCMBytesToFloats(e.RawData);
            ListAudioFloat.Add(_audioFloats);
        }

        bool isPlayStay = false;

        IEnumerator ListCountCheck()
        {
            while (true)
            {
                yield return null;
                if (ListAudioFloat.Count > 0 && !isPlayStay)
                {
                    isPlayStay = true;
                    StartCoroutine(TTS());
                }
            }
        }

        public IEnumerator TTS()
        {
            AudioClip audioClip = AudioClip.Create(fileName, ListAudioFloat[0].Length, Msm.Channel, Msm.TTSFrequency, false);
            audioClip.SetData(ListAudioFloat[0], 0);
            _audioSource.clip = audioClip;

            for (int i = 0; i < ListAudioFloat[0].Length; i++)
                ListSaveAudioFloat.Add(ListAudioFloat[0][i]);

            if (_audioSource.clip && !_audioSource.isPlaying)
            {
                Debug.Log("오디오 소스 있음");
                _audioSource.Play();
            }
            else
            {
                Debug.Log("오디오 소스 없음클립");
            }

            yield return new WaitForSeconds(0.5f);
            StartCoroutine(AudioPlayCheck());
        }

        IEnumerator AudioPlayCheck()
        {
            while (_audioSource.isPlaying)
            {
                yield return null;
            }

            ListAudioFloat.Remove(ListAudioFloat[0]);
            isPlayStay = false;
        }

        void TTSSave(TTSSaveMsg msg)
        {
            Debug.Log("save");
            AudioClip audioClip = AudioClip.Create(fileName, ListSaveAudioFloat.ToArray().Length, Msm.Channel, Msm.TTSFrequency, false);
            audioClip.SetData(ListSaveAudioFloat.ToArray(), 0);
            _audioSource.clip = audioClip;
            string time = Regex.Replace(DateTime.Now.ToString(), @"[^a-zA-Z0-9가-힣]", "", RegexOptions.Singleline);
            string name = string.Format("{0}_{1}", saveFileName, time);
            SavWav.Save(name, audioClip, "MidiaZentTTS");
            ListSaveAudioFloat.Clear();
        }

        float[] PCMBytesToFloats(byte[] bytes)
        {
            float[] floats = new float[bytes.Length / 2];
            for (int i = 0; i < bytes.Length; i += 2)
            {
                short s = BitConverter.ToInt16(bytes, i); // convert 2 bytes to short
                floats[i / 2] = ((float)s) / (float)32768; // convert short to float
            }
            return floats;
        }

        void SendTTS(TTSSendMsg msg)
        {
            saveFileName = msg.fileName;
            if (saveFileName == null)
            {
                saveFileName = "이름없음";
            }

            StartCoroutine(SendCheckTTS(msg.msg, msg.character));
        }
          
        IEnumerator SendCheckTTS(string msg, Character character)
        {
            yield return StartCoroutine(WebSocketConnect(character));

            while (!isWebSocketConnect)
                yield return null;

            yield return StartCoroutine(WebSocketSendMsg(msg, character));
        }

        IEnumerator WebSocketConnect(Character character)
        {
            Log.Instance.log("MidiazenTTS WebSocketConnect");
            isWebSocketConnect = false;
            int reconnectCnt = 0;
            while (reconnectCnt < 5 && !isWebSocketConnect)
            {
                try
                {
                    if(character == Character.Girl)
                        WebSocketGirl.Connect();

                    else if(character == Character.Boy)
                        WebSocketBoy.Connect();

                    isWebSocketConnect = true;
                    Log.Instance.log("MidiazenTTS WebSocketConnect Success");
                    SendLog("MidiazenTTS WebSocketConnect Success");
                    break;
                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenTTS WebSocketConnect Fail : " + E.ToString());
                    SendLog("MidiazenTTS WebSocketConnect Fail");
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        IEnumerator WebSocketSendMsg(string msg, Character character)
        {
            Log.Instance.log("MidiazenTTS WebSocketSendMsg : " + msg);
            bool isConnect = false;
            int reconnectCnt = 0;
            fileName = null;
            while (reconnectCnt < 5 && !isConnect)
            {
                try
                {
                    fileName = msg;
                    if (character == Character.Girl)
                        WebSocketGirl.Send(msg);
                    else
                        WebSocketBoy.Send(msg);

                    isConnect = true;
                    Log.Instance.log("MidiazenTTS WebSocketSendMsg Success");
                    break;
                }
                catch (Exception E)
                {
                    reconnectCnt++;
                    Log.Instance.log("MidiazenTTS WebSocketSendMsg Fail : " + E.ToString());
                    SendLog("MidiazenTTS WebSocketSendMsg Fail");
                }
                yield return new WaitForSeconds(3.0f);
            }
        }

        void ErrorCheck(string msg)
        {
            if (reconnectTryCnt > 10)
            {

            }

            if (msg == "b")
            {

            }
            else if (msg == "a")
            {
                reconnectTryCnt++;
                //TTSConnect();
            }
        }

        void MessageCheck(MessageEventArgs msg)
        {
            //연결 성공 알림
            //음성 데이터 전달 받음
            if (msg.ToString() == "")
            {
                reconnectTryCnt = 0;

                corWebSocketCheck = StartCoroutine(ListCountCheck());
            }
            else if (msg.ToString() == "")
            {
                _audioFloats = null;
                _audioFloats = PCMBytesToFloats(msg.RawData);
                ListAudioFloat.Add(_audioFloats);
            }
        }

        protected override void OnUnload()
        {
            RemoveMessage();
            if (WebSocketGirl != null)
                WebSocketGirl.Close();

            if (WebSocketBoy != null)
                WebSocketGirl.Close();

            if (corListCountCheck != null)
                StopCoroutine(corListCountCheck);

            if (corWebSocketCheck != null)
                StopCoroutine(corWebSocketCheck);

        }

        void RemoveMessage()
        {
            Message.RemoveListener<TTSSendMsg>(SendTTS);
            Message.RemoveListener<TTSSaveMsg>(TTSSave);
        }
    }
}
