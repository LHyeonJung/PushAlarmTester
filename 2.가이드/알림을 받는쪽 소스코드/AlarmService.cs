
using DMI.Common.Core;
using DMI.Common.DataAccess;
using DMI.Common.Model;
using DMI.Common.Services;
using DMI.Common.ViewModel;
using DMI.Manager.Repositories;
using DMI.Manager.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DMI.Manager.Services
{
    public class AlarmPortListener : ManagerBaseViewModel
    {
        private ToastViewModel ToastViewModel = new ToastViewModel();
        public int PortNumber { get; set; }

        private UdpClient _udpClient;
        private IPEndPoint _ipEndPoint;

        private CancellationTokenSource _cts;
        private SmartDispatcherTimer _timer;

        private List<Role> _roleList;
        private Role _role;
        private List<Component> _saList;

        public IConnection _connection;


        private ObservableCollection<AlarmMessage> _alarmMessageList;
        public ObservableCollection<AlarmMessage> AlarmMessageList
        {
            get
            {
                if (_alarmMessageList == null)
                    _alarmMessageList = new ObservableCollection<AlarmMessage>();

                if (_alarmMessageList.Count == 0)
                {
                    IsAlarmEmpty = true;
                }
                else IsAlarmEmpty = false;

                return _alarmMessageList;
            }
            set
            {
                SetProperty<ObservableCollection<AlarmMessage>>(ref _alarmMessageList, value);
            }
        }

        private AlarmMessage _lastAlarmMessage;
        public AlarmMessage LastAlarmMessage
        {
            get
            {
                return _lastAlarmMessage;
            }
            set
            {
                ToastViewModel.ShowInformation(value.Message);
                SetProperty<AlarmMessage>(ref _lastAlarmMessage, value);
            }
        }

        private bool _isAlarmEmpty = true;
        public bool IsAlarmEmpty
        {
            get
            {
                return _isAlarmEmpty;
            }
            set
            {
                SetProperty<bool>(ref _isAlarmEmpty, value);
            }
        }



        private bool _alarmIsOpen;
        public bool AlarmIsOpen
        {
            get
            {
                return _alarmIsOpen;
            }
            set
            {
                SetProperty<bool>(ref _alarmIsOpen, value);
            }
        }

        private AlarmMessage _detailAlarm;
        public AlarmMessage DetailAlarm
        {
            get
            {
                return _detailAlarm;
            }
            set
            {
                SetProperty<AlarmMessage>(ref _detailAlarm, value);
            }
        }

        private DelegateCommand<object> _deleteAlarmCommand;
        public ICommand DeleteAlarmCommand
        {
            get
            {
                if (_deleteAlarmCommand == null)
                    _deleteAlarmCommand = new DelegateCommand<object>(obj =>
                    {
                        if (obj != null)
                        {
                            DeleteAlarm(obj);
                        }
                    });
                return _deleteAlarmCommand;
            }
        }

        private void DeleteAlarm(object obj)
        {
            AlarmMessage DeleteTargetAlarm = obj as AlarmMessage;

            List<AlarmMessage> temp = new List<AlarmMessage>();
            temp = (from a in AlarmMessageList
                    where (a.Id != DeleteTargetAlarm.Id) || (a.Date != DeleteTargetAlarm.Date) || (a.DetailMessage != DeleteTargetAlarm.DetailMessage)
                    select a).ToList();

            AlarmMessageList = null;
            AlarmMessageList = temp.ToObservableCollection();
        }


        private DelegateCommand _deleteAllAlarmCommand;
        public ICommand DeleteAllAlarmCommand
        {
            get
            {
                if (_deleteAllAlarmCommand == null)
                    _deleteAllAlarmCommand = new DelegateCommand(() =>
                    {
                        AlarmMessageList.Clear();
                        IsAlarmEmpty = true;
                    });
                return _deleteAllAlarmCommand;
            }
        }

        public AlarmPortListener()
        {
            PortNumber = 34581;
            _udpClient = new UdpClient(PortNumber);
            _ipEndPoint = new IPEndPoint(IPAddress.Any, PortNumber);
            _timer = new SmartDispatcherTimer();
        }

        /// <summary>
        /// 초단위 갱신주기를 인자로 받는다.
        /// 기본값은 5초에 한번 갱신
        /// </summary>
        /// <param name="timespan"></param>
        public void Start(Role role, List<Component> componentList)
        {
            if(_udpClient.Client == null) _udpClient = new UdpClient(PortNumber);
            ToastViewModel = new ToastViewModel();
            _role = role;
            _saList = componentList;

            if (_cts == null)
            {
                _cts = new CancellationTokenSource();

                _timer.IsReentrant = false;
                _timer.Interval = TimeSpan.FromSeconds(5);
                _timer.TickTask = async () =>
                {
                    await TaskExtension.WithCancellation(
                        ReceiveAsyncWithTimeout(PortNumber, 2000), _cts.Token);
                };
                _timer.Start();
            }
        }
        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
                _timer.Stop();
                _udpClient.Close();
                _udpClient.Dispose();
                ToastViewModel.OnUnloaded();
            }
        }

        public async Task ReceiveAsyncWithTimeout(int pushPortNumber, int timeout)
        {

            var cancelTokenSource = new CancellationTokenSource(timeout);
            try
            {
                await TaskExtension.WithCancellation(ReceiveAsync(pushPortNumber),
                    cancelTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // timeout이 떨어짐 
            }
            finally
            {
                cancelTokenSource.Dispose();
            }
        }

        private async Task ReceiveAsync(int pushPortNumber)
        {
            Debug.WriteLine("Start sa push from port : " + pushPortNumber.ToString());
            var pushResult = await _udpClient.ReceiveAsync();
            string encodedData = Encoding.UTF8.GetString(pushResult.Buffer);

            //sender 생성 전에 사용하는 임시 테스트 데이터
            //var pushResult = "{\"instance\":[{\"date\":\"2021-04-11 18:14:30\",\"component\":\"DP-MSQ\",\"id\":\"msq(dcc)\",\"msg\":\"test msg\",\"detailmsg\":\"test detail msg\"},{\"date\":\"2021-04-11 18:14:30\",\"component\":\"KE-WIN\",\"id\":\"test_win\",\"msg\":\"test msg\",\"detailmsg\":\"test detail msg\"}]}";

            Debug.WriteLine("End receive sa Alarm Message : " + encodedData);

            PushAlarmMessage = encodedData;
        }

        // 포트로부터 수신한 Push 알림 정보
        private string _pushAlarmMessage;
        public string PushAlarmMessage
        {
            get { return _pushAlarmMessage; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _pushAlarmMessage = value;
                    ParsingAlarmMessage(_pushAlarmMessage);
                }
            }
        }

        private void ParsingAlarmMessage(string stringAlarmMessage)
        {
            try
            {
                List<AlarmMessage> temp = new List<AlarmMessage>();

                var jsonResult = JObject.Parse(stringAlarmMessage);

                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
                var parserMessage = JsonConvert.DeserializeObject<List<AlarmMessage>>(
                    jsonResult["instance"].ToString(), dateTimeConverter);



                if ((_role.Features.Where(x => x.Id == Asset.RoleData.um_id1.Id).Where(y => y.Readable == true).Count() > 0) || (_role.Features.Where(x => x.Id == Asset.RoleData.um_id5.Id).Where(y => y.Readable == true).Count() > 0))
                {
                    foreach (var sa in _saList)
                    {
                        var tempMessage = parserMessage.Where(x => x.Id == sa.Id).FirstOrDefault();
                        if (tempMessage != null)
                        {
                            AlarmMessageList.Add(tempMessage);

                            if (AlarmMessageList.Count() != 0)
                            {
                                LastAlarmMessage = AlarmMessageList.Last();
                            }
                        }
                    }
                }
                else
                {
                    //서버별 권한 사용시 로직
                    if (_role.Components.Count() > 0)
                    {
                        foreach (var s in _role.Components)
                        {
                            temp.Add(parserMessage.Where(x => x.Id == s).First());
                        }
                        foreach (var sa in _saList)
                        {
                            AlarmMessageList.Add(temp.Where(x => x.Id == sa.Id).First());
                            if (AlarmMessageList.Count() != 0)
                            {
                                LastAlarmMessage = AlarmMessageList.Last();
                            }
                        }
                    }
                    else
                    {
                        List<string> roleSaNameList = new List<string>();
                        foreach (var sa in _saList)
                        {
                            foreach (var groupName in _role.Groups)
                            {
                                roleSaNameList.Add((from a in sa.GroupList
                                                    where a == groupName
                                                    select sa.Id).First());
                            }
                        }

                        foreach (var sa in roleSaNameList)
                        {
                            AlarmMessageList.Add(parserMessage.Where(x => x.Id == sa).First());

                            if (AlarmMessageList.Count() != 0)
                            {
                                LastAlarmMessage = AlarmMessageList.Last();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message.ToString());
            }
        }

    }
}
