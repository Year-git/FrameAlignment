using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// 帧同步数据类
/// </summary>
public class FrameSynchronData
{
    //是否是暂停状态、进入结算界面时, 战斗逻辑就会暂停
    public bool PauseState { get; set; }
    //是否是回放状态
    public bool ReplayState { get; set; }
    //游戏的逻辑帧
    public int GameLogicFrame { get; set; }
    //预定的每帧的时间长度
    public Fix64 FixFrameLen { get; private set; }
    //随机数对象
    public SeedRandom SRandom { get; set; }
    //所有回放操作的队列
    public List<JArray> PlayerBackOperationList { get; set; }
    //所有操作的队列
    public List<JArray> PlayerOperationList { get; set; }
    //帧同步运行时间（毫秒）
    public int FrameRunningTime { get; set; }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public FrameSynchronData(int seed)
    {
        FixFrameLen = 1f / 60f;
        PlayerBackOperationList = new List<JArray>();
        PlayerOperationList = new List<JArray>();
    }

    /// <summary>
    /// 销毁数据
    /// </summary>
    public void Dispose()
    {
        PlayerOperationList.Clear();
        PlayerBackOperationList.Clear();
    }
}