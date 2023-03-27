using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DccPushAlarmTester.ViewModel;

namespace DccPushAlarmTester.Core
{
    public class ViewModelLocator
    {
        private PushAlarmTesterViewModel _pushAlarmTesterViewModel;
        public PushAlarmTesterViewModel PushAlarmTesterViewModel
        {
            get
            {
                if (_pushAlarmTesterViewModel == null)
                {
                    _pushAlarmTesterViewModel = new PushAlarmTesterViewModel();
                }
                return _pushAlarmTesterViewModel;
            }
        }
    }
}
