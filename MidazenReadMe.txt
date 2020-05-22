MidiazenSTT, MidiazenTTS 프리팹을 Resources/Modules 로 이동 각각 모듈 등록

MidiazenSetting파일을 StreamingAssets/Setting로 이동

STT 수신 메세지 Message.AddListener<STTReceiveMsg> 등록 하여 수신

STTReceiveMsg로 수신시 msg는 기존 녹음 요청 음성 데이터

sttmsg는 가공하지 않은 json 형식

기타 Josn안에 특정 파일을 뽑고 싶으면 MidiazenSTT.cs파일에서 ReceiveMsgCheck함수 확인

json양식 및 프로토콜은 별도 문서 확인

MidiazenSetting 내에서 별도 셋팅값 변경 가능

서버URL및 TTS STT 주파수등

TTS로 출력한 음성을 저장하고 싶으면 음성출력 완료후 TTSSaveMsg 이벤트 실행