using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midiazen;
using UnityEngine.UI;
using System;

public class MidiazenSample : MonoBehaviour
{
    public Button btnTTS;
    public Button btnSTT;
    public Button btnSTTReset;
    public Button btnReset;
    public GameObject LogParent;
    public GameObject LogPrefabs;
    public Text txtRecordStatus;
    public InputField inputTTS;
    public InputField inputFileName;
    public Button btnTTSSAVE;
    // Use this for initialization
    void Start()
    {
        btnTTS.onClick.AddListener(() => SendMsg());
        btnSTT.onClick.AddListener(() => STTConnect());
        btnSTTReset.onClick.AddListener(() => STTReset());
        btnReset.onClick.AddListener(() => STTLogReset());
        btnTTSSAVE.onClick.AddListener(() => Message.Send<TTSSaveMsg>(new TTSSaveMsg()));
        AddMessage();
    }

    void AddMessage()
    {
        Message.AddListener<STTReceiveMsg>(ReceiveMsg);
        Message.AddListener<STTCheck>(STTRecordCheck);
        Message.AddListener<AddLog>(ReceiveLogEvent);
    }

    void ReceiveMsg(STTReceiveMsg msg)
    {
        Debug.Log("메세지 수신 : " + msg.text);
        Message.Send<TTSSendMsg>(new TTSSendMsg(inputFileName.text, msg.text));
        ReceiveLog(msg.text);
    }

    void STTRecordCheck(STTCheck msg)
    {
        txtRecordStatus.text = msg.status.ToString();
    }

    void ReceiveLogEvent(AddLog msg)
    {
        ReceiveLog(msg.logmsg);
    }

    void SendMsg()
    {
        STTLogReset();
        Message.Send<TTSSendMsg>(new TTSSendMsg("abc", inputFileName.text));
    }

    void STTConnect()
    {
        STTLogReset();
        Message.Send<STTRecord>(new STTRecord());
    }

    void STTLogReset()
    {
        for (int i = 0; i < LogParent.transform.childCount - 1; i++)
        {
            Destroy(LogParent.transform.GetChild(i + 1).gameObject);
        }
    }

    void STTReset()
    {
        Message.Send<ResetStt>(new ResetStt());
    }

    void RecordStard()
    {
        Message.Send<StartRecord>(new StartRecord());
    }

    void ReceiveLog(string msg)
    {
        GameObject logObject = GameObject.Instantiate(LogPrefabs);
        logObject.SetActive(true);
        logObject.GetComponent<Text>().text = msg;
        logObject.transform.parent = LogParent.transform;
        logObject.transform.localPosition = new Vector3(0, (LogParent.transform.childCount - 1) * -logObject.GetComponent<RectTransform>().sizeDelta.y);
        logObject.transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        RemoveMessage();
    }

    void RemoveMessage()
    {
        Message.RemoveListener<STTReceiveMsg>(ReceiveMsg);
        Message.RemoveListener<STTCheck>(STTRecordCheck);
    }
}
