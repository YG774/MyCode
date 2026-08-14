namespace MyCSharpCode
{
    public class OeeConfig
    {
        public const int 运行 = 1;
        public const int 待机 = 2;
        public const int 故障 = 3;
        public const int 待料 = 4;

        readonly static object _lockObject = new();

        public OeeConfig() { }
        public static OeeConfig Instance { get; set; } = new();
        public int Ct { get; set; } = 5;
        public int CurrentPlanCount { get; set; } = 4320;
        public DateTime LastShutDownDateTime { get; set; }
        public List<StateInfo> StateInfos { get; set; } = [new(运行, DateTime.Now - TimeSpan.FromDays(2))];

        public TimeSpan GetStateTimeSpan(int state, DateTime beginTime, DateTime endTime)
        {
            if (beginTime >= endTime)
                throw new Exception("开始时间不能大于等于结束时间");
            TimeSpan result = TimeSpan.Zero;
            lock (_lockObject)
            {
                var stateInfos = StateInfos;
                if (stateInfos == null)
                    return result;
                int i = 0;
                for (; i < stateInfos.Count; i++)
                {
                    var stateDateTime = stateInfos[i].DateTime;
                    if (stateDateTime >= beginTime)
                        break;
                }
                if (i >= stateInfos.Count)
                {
                    var last = stateInfos[^1];
                    if (last.State == state)
                        result += endTime - beginTime;
                    return result;
                }

                if (stateInfos[i].DateTime > beginTime && i - 1 >= 0 && state == stateInfos[i - 1].State)
                {
                    result += stateInfos[i].DateTime - beginTime;
                }

                var preStateInfo = stateInfos[i++];
                while (i < stateInfos.Count)
                {
                    var curStateInfo = stateInfos[i];
                    if (preStateInfo.State == state)
                    {
                        if (curStateInfo.DateTime >= endTime)
                        {
                            result += endTime - preStateInfo.DateTime;
                            return result;
                        }
                        else
                        {
                            result += curStateInfo.DateTime - preStateInfo.DateTime;
                        }
                    }
                    else if (curStateInfo.DateTime >= endTime)
                        return result;
                    preStateInfo = curStateInfo;
                    i++;
                }
                if (preStateInfo.State == state)
                {
                    result += endTime - preStateInfo.DateTime;
                }
                return result;
            }
        }

        public TimeSpan GetStatesTimeSpan(int[] states, DateTime beginTime, DateTime endTime, out string? msg)
        {
            msg = string.Empty;
            if (beginTime >= endTime)
                throw new Exception("开始时间不能大于等于结束时间");
            TimeSpan result = TimeSpan.Zero;
            lock (_lockObject)
            {
                var stateInfos = StateInfos;
                if (stateInfos == null)
                    return result;
                int i = 0;
                for (; i < stateInfos.Count; i++)
                {
                    var stateDateTime = stateInfos[i].DateTime;
                    if (stateDateTime >= beginTime)
                        break;
                }
                if (i >= stateInfos.Count)
                {
                    var last = stateInfos[^1];
                    if (states.Contains(last.State))
                        result += endTime - beginTime;
                    msg = last.Msg;
                    return result;
                }

                if (stateInfos[i].DateTime > beginTime && i - 1 >= 0 && states.Contains(stateInfos[i - 1].State))
                {
                    result += stateInfos[i].DateTime - beginTime;
                }

                var preStateInfo = stateInfos[i++];
                while (i < stateInfos.Count)
                {
                    var curStateInfo = stateInfos[i];
                    if (states.Contains(preStateInfo.State))
                    {
                        if (curStateInfo.DateTime >= endTime)
                        {
                            result += endTime - preStateInfo.DateTime;
                            msg = curStateInfo.Msg;
                            return result;
                        }
                        else
                        {
                            msg = curStateInfo.Msg;
                            result += curStateInfo.DateTime - preStateInfo.DateTime;
                        }
                    }
                    else if (curStateInfo.DateTime >= endTime)
                        return result;
                    preStateInfo = curStateInfo;
                    i++;
                }
                if (states.Contains(preStateInfo.State))
                {
                    result += endTime - preStateInfo.DateTime;
                    msg = preStateInfo.Msg;
                }
                return result;
            }
        }

        public int GetErrCount(DateTime beginTime, DateTime endTime)
        {
            if (beginTime >= endTime)
                throw new Exception("开始时间不能大于等于结束时间");
            int result = 0;
            lock (_lockObject)
            {
                var stateInfos = StateInfos;
                if (stateInfos == null)
                    return result;
                int i = 0;
                for (; i < stateInfos.Count; i++)
                {
                    var stateDateTime = stateInfos[i].DateTime;
                    if (stateDateTime >= beginTime)
                        break;
                }
                if (i >= stateInfos.Count)
                {
                    return result;
                }

                while (i < stateInfos.Count)
                {
                    var curStateInfo = stateInfos[i];
                    if (curStateInfo.State == 故障)
                    {
                        result++;
                    }
                    if (curStateInfo.DateTime >= endTime)
                        return result;
                    i++;
                }
                return result;
            }
        }

        public void AddState(int state, DateTime dateTime, string msg = "")
        {
            lock (_lockObject)
            {
                if (StateInfos.Count > 0)
                {
                    var last = StateInfos[^1];
                    if (last.DateTime >= dateTime)
                    {
                        throw new Exception("新添加的时间不能小于旧的时间");
                    }
                    if (last.State != state /*|| (last.State == 故障 && state == 待机)*/)
                    {
                        StateInfos.Add(new(state, dateTime) { Msg = msg });
                    }
                }
                else
                {
                    StateInfos.Add(new(state, dateTime) { Msg = msg });
                }
                RemoveExpiration();
            }
        }

        public void RemoveExpiration()
        {
            int count = StateInfos.Count;
            if (count < 101)
                return;
            var expirationDateTime = DateTime.Now - TimeSpan.FromDays(2);
            int i = 0;
            while (i < count - 100)
            {
                var stateInfo = StateInfos[i];
                if (stateInfo.DateTime >= expirationDateTime)
                    break;
                i++;
            }
            if (i > 0)
                StateInfos.RemoveRange(0, i);
        }
    }

    public record class StateInfo(int State, DateTime DateTime)
    {
        public string? Msg { get; set; }
    }
}