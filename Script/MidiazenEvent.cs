using JHchoi.Constants;

namespace Midiazen
{
    public class TTSSendMsg : Message
    {
        public string fileName;
        public string msg;
        public Character character;

        public TTSSendMsg(string fileName, string msg, Character character = Character.Girl)
        {
            this.fileName = fileName;
            this.msg = msg;
            this.character = character;
        }
    }

    public class TTSSaveMsg : Message
    {

    }

    public class STTRecord : Message
    {

    }

    public class StartRecord : Message
    {

    }

    public class STTReceiveMsg : Message
    {
        public string intent;
        public string text;

        public STTReceiveMsg(string intent, string text)
        {
            this.intent = intent;
            this.text = text;
        }
    }

    public class STTCheck : Message
    {
        public STTStatus status;

        public STTCheck(STTStatus status)
        {
            this.status = status;
        }
    }

    public class AddLog : Message
    {
        public string logmsg;

        public AddLog(string logmsg)
        {
            this.logmsg = logmsg;
        }
    }

    public class ResetStt : Message
    {

    }

    public class WakeUpMsg : Message
    {
        public bool isWakeUp;
        public WakeUpMsg(bool isWakeUp)
        {
            this.isWakeUp = isWakeUp;
        }
    }
}
