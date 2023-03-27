using DMI.Common.Core;
using DMI.Common.Model;
using DMI.Common.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DccPushAlarmTester.ViewModel
{
    public class PushAlarmTesterViewModel: ViewModelBase
    {
        public PushAlarmTesterViewModel()
        {
        }

        private string _log;
        public string Log
        {
            get
            {
                return _log;
            }
            set
            {
                SetProperty<string>(ref _log, value);
            }
        }

        private DelegateCommand _clearLogCommand;
        public ICommand ClearLogCommand
        {
            get
            {
                if (_clearLogCommand == null)
                {
                    _clearLogCommand = new DelegateCommand(() =>
                    {
                        Log = string.Empty;
                    });
                }
                return _clearLogCommand;
            }
        }

        private AlarmMessage _inputAlarmMessage;
        public AlarmMessage InputAlarmMessage
        {
            get
            {
                if (_inputAlarmMessage == null) _inputAlarmMessage = new AlarmMessage();
                return _inputAlarmMessage;
            }
            set
            {
                SetProperty<AlarmMessage>(ref _inputAlarmMessage, value);
            }
        }

        private string _convertedResult;
        public string ConvertedResult
        {
            get
            {
                return _convertedResult;
            }
            set
            {
                SetProperty<string>(ref _convertedResult, value);
            }
        }

        private JObject _sendData;
        public JObject SendData
        {
            get
            {
                return _sendData;
            }
            set
            {
                SetProperty<JObject>(ref _sendData, value);
            }
        }

        private DelegateCommand _convertStringCommand;
        public ICommand ConvertStringCommand
        {
            get
            {
                if (_convertStringCommand == null)
                {
                    _convertStringCommand = new DelegateCommand(() =>
                    {
                        ConvertAlarmMessageToJson(InputAlarmMessage);
                    });
                }
                return _convertStringCommand;
            }
        }

        private void ConvertAlarmMessageToJson(AlarmMessage alarmMessage)
        {
            /*
            string protocol = @"{
                    'instance' :
                    [
                        {
		                    'date': '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
			                'component': '" + sendData.Type + @"',
			                'id': '" + sendData.Id + @"',
                            'msg': '" + sendData.Message + @"',
                            'detailmsg': '" + sendData.DetailMessage + @"'
                        }
                    ]
                }";

            JObject jsonReqest = JObject.Parse(protocol);

            ConvertedResult = jsonReqest.ToString().Replace("\r\n", "\\r\\n");

            SendData = protocol;//jsonReqest.ToString();
            */
            JObject jObject = new JObject(
                new JProperty("instance",
                    new JArray(
                        new JObject(
                            new JProperty("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                            new JProperty("component", alarmMessage.Type),
                            new JProperty("id", alarmMessage.Id),
                            new JProperty("msg", alarmMessage.Message),
                            new JProperty("detailmsg", alarmMessage.DetailMessage)
                        )
                    )
                )
            );
            SendData = jObject;
            ConvertedResult = SendData.ToString();
        }
        
        private DelegateCommand _sendPushAlarmCommand;
        public ICommand SendPushAlarmCommand
        {
            get
            {
                if (_sendPushAlarmCommand == null)
                {
                    _sendPushAlarmCommand = new DelegateCommand(() =>
                    {
                        SendPushAlarm();
                    });
                }
                return _sendPushAlarmCommand;
            }
        }

        private async void SendPushAlarm()
        {
            ConvertAlarmMessageToJson(InputAlarmMessage);
            try
            {
                Log += "\n"+ SendData + "\n";
                var sendClient = new UdpClient();

                byte[] datagram = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(SendData)); 
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 34581);

                await sendClient.SendAsync(datagram, datagram.Length, endPoint);

                sendClient.Close();
                Log += "======[전송 완료]====== \n";
            }
            catch (Exception ex)
            {
                Log += "\n ======[전송 오류]====== \n" + ex.ToString();
            }
        }

    }
}
