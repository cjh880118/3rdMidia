using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JHchoi.Module;
using JHchoi;

namespace Midiazen
{
    public class MidiazenMain : IModule
    {
        //실제
        //STTUrl=ws://58.225.115.230:62324/ws
        //TTSUrl=ws://58.225.115.230:62324/ws

        //테스트
        //STTUrl=ws://39.118.204.91:35531/ws
        //TTSUrl=ws://39.118.204.91:35532/ws
        MidiazenSettingModel settingModel;
        protected override void OnLoadStart()
        {
            settingModel = new MidiazenSettingModel();
            settingModel.Setup();
            StartCoroutine(LoadModule());
            SetResourceLoadComplete();
        }

        IEnumerator LoadModule()
        {
            string path = "Modules/MidiazenTTS";
            yield return StartCoroutine(ResourceLoader.Instance.Load<GameObject>(path,
                o =>
                {
                    var gameObject = Instantiate(o) as GameObject;
                    gameObject.transform.SetParent(this.gameObject.transform);
                    gameObject.GetComponent<MidiazenTTS>().InitModule(settingModel);
                }));

            path = "Modules/MidiazenSTT";
            yield return StartCoroutine(ResourceLoader.Instance.Load<GameObject>(path,
                o =>
                {
                    var gameObject = Instantiate(o) as GameObject;
                    gameObject.transform.SetParent(this.gameObject.transform);
                    //gameObject.GetComponent<MidiazenSTT>().InitModule(settingModel);
                    gameObject.GetComponent<MidiazenSTT>().InitModule(settingModel);
                }));
        }
    }
}
