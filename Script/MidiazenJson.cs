using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Midiazen
{
    struct response {

    }

    [System.Serializable]
    struct Payload
    {
        public string status;
        //public string response;
    }

    [System.Serializable]
    struct VoicePayload
    {
        public string user;
        public string text;
        public float confidence;
        public bool epd;
    }

    [System.Serializable]
    class ConnectRespons
    {
        public string topic;
        public Payload payload;
        public string @event;
    }

    [System.Serializable]
    class STTResponse
    {
        public string topic;
        public VoicePayload payload;
    }

    [System.Serializable]
    struct Keyward
    {
        public string name;
        public string val;
    }

    [System.Serializable]
    struct Results
    {
        public Keyward[] slot;
        public string intent;
    }

    [System.Serializable]
    class STTMsg
    {
        public Results[] results;
        public string text;
    }

}

