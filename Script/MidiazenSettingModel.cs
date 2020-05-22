using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JHchoi.Models;
using System;

namespace Midiazen
{
    public class MidiazenSettingModel : Model
    {
        public ModelRef<MidiazenSettingModel> setting = new ModelRef<MidiazenSettingModel>();

        public void Setup()
        {
            setting.Model = new MidiazenSettingModel();
            LoadingSettingFile();
        }

        string sTTurl;
        public string STTUrl
        {
            get
            {
                return sTTurl;
            }
        }

        string tTSGirlUrl = "";
        public string TTSGirlUrl
        {
            get
            {
                return tTSGirlUrl;
            }
        }

        string tTSBoyUrl = "";
        public string TTSBoyUrl
        {
            get
            {
                return tTSBoyUrl;
            }
        }

        int channel;
        public int Channel
        {
            get
            {
                return channel;
            }
        }

        int sttFrequency;
        public int STTFrequency
        {
            get
            {
                return sttFrequency;
            }
        }

        int ttsFrequency;
        public int TTSFrequency
        {
            get
            {
                return ttsFrequency;
            }
        }

        Language language;
        public Language Language
        {
            get
            {
                return language;
            }
        }

        Cmd cmd;
        public Cmd Cmd
        {
            get
            {
                return cmd;
            }
        }

        bool intermediates;
        public bool Intermediates
        {
            get
            {
                return intermediates;
            }
        }

        string sdsIp;
        public string SdsIp
        {
            get { return sdsIp; }
        }

        int sdsPort;
        public int SdsPort
        {
            get { return sdsPort; }
        }

        private void LoadingSettingFile()
        {
            string line;
            string pathBase = Application.dataPath + "/StreamingAssets/";
            string path = "Setting/MidiazenSetting.txt";
            using (System.IO.StreamReader file = new System.IO.StreamReader(pathBase + path))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(";") || string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("STTUrl"))
                        sTTurl = line.Split('=')[1];
                    else if (line.StartsWith("TTSGirlUrl"))
                        tTSGirlUrl = line.Split('=')[1];
                    else if (line.StartsWith("TTSBoyUrl"))
                        tTSBoyUrl = line.Split('=')[1];
                    else if (line.StartsWith("SDSIp"))
                        sdsIp = line.Split('=')[1];
                    else if (line.StartsWith("SDSPort"))
                        sdsPort = int.Parse(line.Split('=')[1]);
                    else if (line.StartsWith("Channel"))
                        channel = int.Parse(line.Split('=')[1]);
                    else if (line.StartsWith("TTSFrequency"))
                        ttsFrequency = int.Parse(line.Split('=')[1]);
                    else if (line.StartsWith("STTFrequency"))
                        sttFrequency = int.Parse(line.Split('=')[1]);
                    else if (line.StartsWith("Language"))
                        language = (Language)Enum.Parse(typeof(Language), line.Split('=')[1]);
                    else if (line.StartsWith("Cmd"))
                        cmd = (Cmd)Enum.Parse(typeof(Cmd), line.Split('=')[1]);
                    else if (line.StartsWith("Intermediates"))
                        intermediates = bool.Parse(line.Split('=')[1]);
                }
                file.Close();
                line = string.Empty;
            }
        }
    }
}