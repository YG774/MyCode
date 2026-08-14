using System.Diagnostics;
using System.Text;

namespace MyCSharpCode
{
    public class TaskItem
    {
        public int Id { get; set; }
        public bool AComplete { get; set; }
        public bool BComplete { get; set; }
        public bool CComplete { get; set; }
    }

    public class Imp

    {
        const int TIMEOUT = 30_000;
        readonly object _lockObj = new object();
        int _aNo, _bNo, _cNo, _paNo, _pbNo, _pcNo;
        AutoResetEvent _aEvent = new(false), _bEvent = new(false), _cEvent = new(false);
        List<int>? _aWaitList, _bWaitList, _cWaitList;
        List<TaskItem> _taskItems;

        public Imp(int num)
        {
            _taskItems = new List<TaskItem>(num);
            for (int i = 0; i < num; i++)
                _taskItems.Add(new TaskItem() { Id = i });
        }

        async Task ExecuteA(TaskItem item)
        {
            await Task.Delay(500);
            item.AComplete = true;
        }
        async Task ExecuteB(TaskItem item)
        {
            await Task.Delay(500);
            item.BComplete = true;
        }
        async Task ExecuteC(TaskItem item)
        {
            await Task.Delay(500);
            item.CComplete = true;
        }
        async Task CloseA(TaskItem item)
        {
            await Task.Delay(500);
            return;
        }
        async Task CloseB(TaskItem item)
        {
            await Task.Delay(500);
            return;
        }
        async Task CloseC(TaskItem item)
        {
            await Task.Delay(500);
            return;
        }

        async Task ATask(int[] indexs, CancellationToken token)
        {
            List<int> waitList = [];
            int i = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int cur = -1;
                lock (_lockObj)
                {
                    if (_bWaitList != null && _bWaitList.Contains(_paNo))
                    {
                        if (_bNo != -1)
                            throw new Exception("A逻辑错误1");
                        _bWaitList = null;
                        _bNo = _paNo;
                        _bEvent.Set();
                    }
                    else if (_cWaitList != null && _cWaitList.Contains(_paNo))
                    {
                        if (_cNo != -1)
                            throw new Exception("A逻辑错误2");
                        _cWaitList = null;
                        _cNo = _paNo;
                        _cEvent.Set();
                    }
                    foreach (var waitNo in waitList)
                    {
                        if (waitNo != _bNo && waitNo != _pbNo && waitNo != _cNo && waitNo != _pcNo)
                        {
                            _paNo = _aNo;
                            _aNo = waitNo;
                            cur = waitNo;
                            break;
                        }
                    }
                    if (cur == -1)
                    {
                        while (i < indexs.Length)
                        {
                            int no = indexs[i];
                            if (no != _bNo && no != _pbNo && no != _cNo && no != _pcNo)
                            {
                                _paNo = _aNo;
                                _aNo = no;
                                cur = no;
                                i++;
                                break;
                            }
                            waitList.Add(no);
                            i++;
                        }
                        if (cur == -1)
                        {
                            _paNo = _aNo;
                            _aNo = -1;
                            if (waitList.Count == 0 && _paNo == -1)
                            {
                                return;
                            }
                            else if (_paNo == -1)
                            {
                                _aWaitList = waitList;
                                _aEvent.Reset();
                            }
                        }
                    }
                    else
                    {
                        if (cur != _aNo)
                            throw new Exception("A逻辑错误3");
                        if (!waitList.Remove(_aNo))
                            throw new Exception("A逻辑错误4");
                    }
                }
                if (cur >= 0)
                {
                    if (cur != _aNo)
                        throw new Exception("A逻辑错误5");
                    var cell = _taskItems[_aNo];
                    await ExecuteA(cell);
                }
                else if (_paNo != -1)
                {
                    var cell = _taskItems[_paNo];
                    await CloseA(cell);
                }
                else if (waitList.Count > 0)
                {
                    var waitResult = _aEvent.WaitOne(TIMEOUT);
                    if (!waitResult)
                        throw new Exception("A逻辑错误6");
                    if (_aNo < 0)
                        throw new Exception("A逻辑错误7");
                    var cell = _taskItems[_aNo];
                    await ExecuteA(cell);
                    if (!waitList.Remove(_aNo))
                        throw new Exception("A逻辑错误8");
                }
                else
                    throw new Exception("A逻辑错误9");
            }
        }
        async Task BTask(int[] indexs, CancellationToken token)
        {
            List<int> waitList = [];
            int i = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int cur = -1;
                lock (_lockObj)
                {
                    if (_aWaitList != null && _aWaitList.Contains(_pbNo))
                    {
                        if (_aNo != -1)
                            throw new Exception("B逻辑错误1");
                        _aWaitList = null;
                        _aNo = _pbNo;
                        _aEvent.Set();
                    }
                    else if (_cWaitList != null && _cWaitList.Contains(_pbNo))
                    {
                        if (_cNo != -1)
                            throw new Exception("B逻辑错误2");
                        _cWaitList = null;
                        _cNo = _pbNo;
                        _cEvent.Set();
                    }
                    foreach (var waitNo in waitList)
                    {
                        if (waitNo != _aNo && waitNo != _paNo && waitNo != _cNo && waitNo != _pcNo)
                        {
                            _pbNo = _bNo;
                            _bNo = waitNo;
                            cur = waitNo;
                            break;
                        }
                    }
                    if (cur == -1)
                    {
                        while (i < indexs.Length)
                        {
                            int no = indexs[i];
                            if (no != _aNo && no != _paNo && no != _cNo && no != _pcNo)
                            {
                                _pbNo = _bNo;
                                _bNo = no;
                                cur = no;
                                i++;
                                break;
                            }
                            waitList.Add(no);
                            i++;
                        }
                        if (cur == -1)
                        {
                            _pbNo = _bNo;
                            _bNo = -1;
                            if (waitList.Count == 0 && _pbNo == -1)
                                return;
                            else if (_pbNo == -1)
                            {
                                _bWaitList = waitList;
                                _bEvent.Reset();
                            }
                        }
                    }
                    else
                    {
                        if (cur != _bNo)
                            throw new Exception("B逻辑错误3");
                        if (!waitList.Remove(_bNo))
                            throw new Exception("B逻辑错误4");
                    }
                }
                if (cur >= 0)
                {
                    if (cur != _bNo)
                        throw new Exception("B逻辑错误5");
                    var cell = _taskItems[_bNo];
                    await ExecuteB(cell);
                }
                else if (_pbNo != -1)
                {
                    var cell = _taskItems[_pbNo];
                    await CloseB(cell);
                }
                else if (waitList.Count > 0)
                {
                    var waitResult = _bEvent.WaitOne(TIMEOUT);
                    if (!waitResult)
                        throw new Exception("B逻辑错误6");
                    if (_bNo < 0)
                        throw new Exception("B逻辑错误7");
                    var cell = _taskItems[_bNo];
                    await ExecuteB(cell);
                    if (!waitList.Remove(_bNo))
                        throw new Exception("B逻辑错误8");
                }
                else
                    throw new Exception("B逻辑错误9");
            }
        }
        async Task CTask(int[] indexs, CancellationToken token)
        {
            List<int> waitList = [];
            int i = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int cur = -1;
                lock (_lockObj)
                {
                    if (_aWaitList != null && _aWaitList.Contains(_pcNo))
                    {
                        if (_aNo != -1)
                            throw new Exception("C逻辑错误1");
                        _aWaitList = null;
                        _aNo = _pcNo;
                        _aEvent.Set();
                    }
                    else if (_bWaitList != null && _bWaitList.Contains(_pcNo))
                    {
                        if (_bNo != -1)
                            throw new Exception("C逻辑错误2");
                        _bWaitList = null;
                        _bNo = _pcNo;
                        _bEvent.Set();
                    }
                    foreach (var waitNo in waitList)
                    {
                        if (waitNo != _aNo && waitNo != _paNo && waitNo != _bNo && waitNo != _pbNo)
                        {
                            _pcNo = _cNo;
                            _cNo = waitNo;
                            cur = waitNo;
                            break;
                        }
                    }
                    if (cur == -1)
                    {
                        while (i < indexs.Length)
                        {
                            int no = indexs[i];
                            if (no != _aNo && no != _paNo && no != _bNo && no != _pbNo)
                            {
                                _pcNo = _cNo;
                                _cNo = no;
                                cur = no;
                                i++;
                                break;
                            }
                            waitList.Add(no);
                            i++;
                        }
                        if (cur == -1)
                        {
                            _pcNo = _cNo;
                            _cNo = -1;
                            if (waitList.Count == 0 && _pcNo == -1)
                                return;
                            else if (_pcNo == -1)
                            {
                                _cWaitList = waitList;
                                _cEvent.Reset();
                            }
                        }
                    }
                    else
                    {
                        if (cur != _cNo)
                            throw new Exception("C逻辑错误3");
                        if (!waitList.Remove(_cNo))
                            throw new Exception("C逻辑错误4");
                    }
                }
                if (cur >= 0)
                {
                    if (cur != _cNo)
                        throw new Exception("C逻辑错误5");
                    var cell = _taskItems[_cNo];
                    await ExecuteC(cell);
                }
                else if (_pcNo != -1)
                {
                    var cell = _taskItems[_pcNo];
                    await Task.Delay(100, token);
                    await CloseC(cell);
                    await Task.Delay(100, token);
                    await CloseC(cell);
                }
                else if (waitList.Count > 0)
                {
                    var waitResult = _cEvent.WaitOne(TIMEOUT);
                    if (!waitResult)
                        throw new Exception("C逻辑错误6");
                    if (_cNo < 0)
                        throw new Exception("C逻辑错误7");
                    var cell = _taskItems[_cNo];
                    await ExecuteC(cell);
                    if (!waitList.Remove(_cNo))
                        throw new Exception("C逻辑错误8");
                }
                else
                    throw new Exception("C逻辑错误9");
            }
        }

        public async Task ExecuteAsync(int[] aIndexs, int[] bIndexs, int[] cIndexs, CancellationToken token = default)
        {
            _aNo = -1;
            _bNo = -1;
            _cNo = -1;
            _paNo = -1;
            _pbNo = -1;
            _pcNo = -1;
            _aWaitList = null;
            _bWaitList = null;
            _cWaitList = null;

            Task aTask = ATask(aIndexs, token);
            Task bTask = BTask(bIndexs, token);
            Task cTask = CTask(cIndexs, token);

            Task waitTask = Task.WhenAll(aTask, bTask, cTask);

            try
            {
                await waitTask;
            }
            finally
            {
                if (waitTask.Exception != null)
                {
                    StringBuilder msg = new StringBuilder();
                    if (token.IsCancellationRequested)
                        msg.AppendLine("任务取消");
                    else
                        msg.AppendLine("任务异常");
                    msg.AppendLine($"_aNo={_aNo}");
                    msg.AppendLine($"_paNo={_paNo}");
                    msg.AppendLine($"_bNo={_bNo}");
                    msg.AppendLine($"_pbNo={_pbNo}");
                    msg.AppendLine($"_cNo={_cNo}");
                    msg.AppendLine($"_pcNo={_pcNo}");
                    if (_aWaitList != null)
                        msg.AppendLine($"_aWaitList = {string.Join(',', _aWaitList)}");
                    if (_bWaitList != null)
                        msg.AppendLine($"_bWaitList = {string.Join(',', _bWaitList)}");
                    if (_cWaitList != null)
                        msg.Append($"_cWaitList = {string.Join(',', _cWaitList)}");
                    Debug.WriteLine(msg.ToString());
                    Debug.WriteLine(waitTask.Exception.ToString());
                }
            }
        }
    }
}