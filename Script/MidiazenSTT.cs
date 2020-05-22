//using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using JHchoi.Module;
using System.Text;
using System.Threading;
using System.Net;
using JHchoi.UI.Event;
using JHchoi.Models;
//SDSIp=121.182.21.153
//SDSPort=12324

namespace Midiazen
{
    public enum STTStatus
    {
        None,
        RecordReady,
        RecordIng,
        RecordEnd
    }

    public class MidiazenSTT : IModule
    {
        //TCP 서버
        STTStatus sTTStatus;
        Socket socket;
        bool isTcpConnect;
        MidiazenSettingModel Msm;
        AudioSource _audioSource;
        Thread socketThread;
        Thread recvThread;
        Coroutine corTimer;
        List<string> listMsg = new List<string>();

        bool isRecord;
        byte[] receiveBuffer = new byte[9999];
        byte[] sendBuffer = new byte[9999];
        int bytesCnt = 0;
        IPHostEntry ipHostInfo;
        IPAddress ipAddress;
        IPEndPoint remoteEP;

        PlayerInventoryModel playerInventory;

        protected override void OnLoadStart()
        {
            Log.Instance.log("STT START");
            playerInventory = Model.First<PlayerInventoryModel>();
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            Msm = new MidiazenSettingModel();
            Msm.Setup();
            AddMeeage();
            ipAddress = IPAddress.Parse(Msm.SdsIp);
            remoteEP = new IPEndPoint(ipAddress, Msm.SdsPort);
            //socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SetResourceLoadComplete();
        }

        public void InitModule(MidiazenSettingModel msm)
        {
            Log.Instance.log("STT START");
            playerInventory = Model.First<PlayerInventoryModel>();
            Msm = msm;
            _audioSource = this.gameObject.GetComponent<AudioSource>();
            ipAddress = IPAddress.Parse(Msm.SdsIp);
            remoteEP = new IPEndPoint(ipAddress, Msm.SdsPort);
            //socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sTTStatus = STTStatus.None;
            AddMeeage();
        }

        void AddMeeage()
        {
            Message.AddListener<STTRecord>(STTConnect);
            Message.AddListener<ResetStt>(STTReset);
        }

        void STTConnect(STTRecord msg)
        {
            if (sTTStatus == STTStatus.None || sTTStatus == STTStatus.RecordEnd)
            {
                if (socketThread != null)
                    socketThread = null;

                socketThread = new Thread(new ThreadStart(SocketConnect));
                socketThread.Start();
            }
        }

        void STTReset(ResetStt msg)
        {
            DestorySTT();
        }

        void SocketConnect()
        {
            Log.Instance.log("SocketConnect");
            isTcpConnect = false;

            try
            {
                sTTStatus = STTStatus.RecordReady;
                Message.Send<STTCheck>(new STTCheck(sTTStatus));
                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(remoteEP);
                Log.Instance.log("SocketConnect Success");
                SendLog("SocketConnect Success");
                recvThread = new Thread(new ThreadStart(RecvThread));
                recvThread.Start();
                isTcpConnect = true;
            }
            catch (Exception E)
            {
                Log.Instance.log("SocketConnect Fail : " + E.ToString());
            }
        }

        void SendLog(string logmsg)
        {
            Message.Send<AddLog>(new AddLog(logmsg));
        }

        void RecvThread()
        {
            string msg;
            while (socket != null)
            {
                try
                {
                    bytesCnt = socket.Receive(receiveBuffer, receiveBuffer.Length, 0);
                    if (receiveBuffer.Length > 0)
                    {
                        msg = ByteToString(receiveBuffer);
                        listMsg.Add(msg);
                        Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    }
                }
                catch (Exception E)
                {
                    Log.Instance.log("RecvThread Socket Receive Error : " + E.ToString());
                    isRecord = false;

                    if (socket != null)
                    {
                        socket.Close();
                        socket = null;
                    }
                    break;
                }
            }

            if (recvThread != null && recvThread.IsAlive)
            {
                recvThread.Abort();
                try
                {
                    recvThread = null;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }

            }
        }

        IEnumerator RecordStart()
        {
            Log.Instance.log("Record Start");
            sTTStatus = STTStatus.RecordIng;
            Message.Send<STTCheck>(new STTCheck(sTTStatus));
            _audioSource.clip = Microphone.Start(null, false, 100, Msm.STTFrequency);
            int _lastSample = 0;

            while (isRecord)
            {
                yield return null;
                int pos = Microphone.GetPosition(null);
                int diff = pos - _lastSample;

                if (diff > 0)
                {
                    Array.Clear(sendBuffer, 0, sendBuffer.Length);
                    float[] samples = new float[diff * _audioSource.clip.channels];
                    _audioSource.clip.GetData(samples, _lastSample);
                    byte[] bytes = WavUtility.ConvertAudioClipDataToInt16ByteArray(samples);
                    string protocolString = "VD" + bytes.Length;
                    byte[] protocolBytes = StringToByte(protocolString);
                    sendBuffer = new Byte[protocolBytes.Length + bytes.Length];
                    Buffer.BlockCopy(protocolBytes, 0, sendBuffer, 0, protocolBytes.Length);
                    Buffer.BlockCopy(bytes, 0, sendBuffer, protocolBytes.Length, bytes.Length);

                    try
                    {
                        if (diff >= 2560)
                        {
                            socket.Send(sendBuffer);
                            _lastSample = pos;
                        }
                    }
                    catch (Exception E)
                    {
                        SendLog("RecordStart Socekt Send Error");
                        Log.Instance.log("RecordStart Socekt Send Error: " + E.ToString());
                        isRecord = false;

                        if (socket != null)
                        {
                            socket.Close();
                            socket = null;
                        }

                        if (recvThread != null && recvThread.IsAlive)
                        {
                            recvThread.Abort();
                            recvThread = null;
                        }

                        Message.Send<STTReceiveMsg>(new STTReceiveMsg("reset", "reset"));
                    }
                }
            }
        }

        void ReceiveMsgCheck(string msg)
        {
            Log.Instance.log("ReceiveMsgCheck : " + msg);
            
            if (msg.Contains("LI0005hello"))
            {
                Log.Instance.log("Connect Success");
                try
                {
                    if (socket != null)
                    {
                        //Message.Send<TTSSendMsg>(new TTSSendMsg("", " 네?", playerInventory.NowCharacter));
                        //Message.Send<STTBtnSetMsg>(new STTBtnSetMsg(false));
                        //Message.Send<InfoMsg>(new InfoMsg("음성을 입력해주세요."));
                        Log.Instance.log("Send Portocall ViceRecordStart");
                        socket.Send(StringToByte("VS0000"));
                        isRecord = true;
                        StartCoroutine(RecordStart());
                    }
                }
                catch (Exception E)
                {
                    SendLog("ReceiveMsgCheck Socekt SendError");
                    Log.Instance.log("ReceiveMsgCheck Socekt SendError : " + E.ToString());
                }
            }
            else if (msg.Contains("VE0000"))
            {
                if (corTimer != null)
                    StopCoroutine(corTimer);

                Log.Instance.log("음성인식 종료");
                SendLog("음성인식 종료");
                sTTStatus = STTStatus.RecordEnd;
                Message.Send<STTCheck>(new STTCheck(sTTStatus));
                isRecord = false;
            }
            else if (msg.Contains("JS"))
            {
                Debug.Log(msg);
                Log.Instance.log("STT 추출 완료");
                SendLog("STT 추출 완료");

                string length = msg.Substring(2, 4);
                if (length != "0000")
                {
                    string sttString = msg.Substring(6);
                    var sttmsg = JsonUtility.FromJson<STTMsg>(sttString);
                    string intent = "";
                    for (int i = 0; i < sttmsg.results.Length; i++)
                    {
                        intent = sttmsg.results[i].intent;
                        SendLog("intent : " + sttmsg.results[i].intent);
                        for (int j = 0; j < sttmsg.results[i].slot.Length; j++)
                        {
                            SendLog("name : " + sttmsg.results[i].slot[j].name);
                            SendLog("Val : " + sttmsg.results[i].slot[j].val);
                        }
                    }
                    Message.Send<STTReceiveMsg>(new STTReceiveMsg(intent, sttmsg.text));

                }

                recvThread.Abort();
                recvThread = null;
                socket.Close();
                socket = null;
            }
        }

        IEnumerator ReceiveTimer(DateTime startTime)
        {
            while (startTime.AddSeconds(7) > DateTime.Now)
            {
                yield return null;
            }

            if (socket != null)
            {
                socket.Close();
                socket = null;
            }


            if (recvThread.IsAlive && recvThread != null)
            {
                recvThread.Abort();
                recvThread = null;
                //sTTStatus = STTStatus.None;
                //Message.Send<STTReceiveMsg>(new STTReceiveMsg("reset", "reset"));
            }

            sTTStatus = STTStatus.None;
            Message.Send<STTReceiveMsg>(new STTReceiveMsg("reset", "reset"));

        }

        private void Update()
        {
            if (listMsg.Count > 0)
            {
                ReceiveMsgCheck(listMsg[0]);
                listMsg.RemoveAt(0);
            }
        }

        private string ByteToString(byte[] strByte) { string str = Encoding.Default.GetString(strByte); return str; }
        private byte[] StringToByte(string str) { byte[] StrByte = Encoding.ASCII.GetBytes(str); return StrByte; }

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
            StopAllCoroutines();
            RemoveMessage();
        }

        void RemoveMessage()
        {
            Message.RemoveListener<STTRecord>(STTConnect);
        }
    }
}
