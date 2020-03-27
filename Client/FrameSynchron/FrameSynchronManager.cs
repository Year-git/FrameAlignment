using System;
using System.IO;
using Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
public class FrameSynchronManager : Singleton<FrameSynchronManager>
{
    //帧同步核心逻辑对象
    public LockStep LockStep{get;private set;}
    //帧同步数据对象
    public FrameSynchronData fsData{get;private set;}
    //战斗回放数据
    private string BattleRecord { get; set; }
    //战斗数据写入路径
    private static readonly string PersistentDataPath = Application.persistentDataPath + "/BattleRecords";
    //是否已开始帧同步
    public bool isStart{get;private set;} = false;

    public void Start(bool replay = false, string recordName = "")
    {
        //-----------------------------------
        //本地帧同步开始
        LocalFrameSynServer.Start();
        //-----------------------------------
        
        //监听事件
        RegistEvent();

        //初始化数据
        fsData = new FrameSynchronData(1000)
        {
            //初始为暂停状态
            PauseState = true
        };

        //回放
        if (replay)
        {
            fsData.ReplayState = true;
            LoadBattleData(recordName);
        }

        //初始化帧同步逻辑对象
        LockStep = new LockStep(fsData);

        //游戏运行速度
        GTime.TimeScale = 1;

        isStart = true;
    }

    public void OnDestroy()
    {
        if(fsData == null){
            return;
        }
        //移除监听事件
        RemoveEvent();

        //销毁帧同步逻辑对象
        LockStep = null;

        //游戏运行速度
        GTime.TimeScale = 1;

        //销毁数据
        fsData.Dispose();
        fsData = null;

        isStart = false;
    }

    private void RegistEvent()
    {
        GEvent.RegistEvent(GacEvent.FixedUpdate, FixedUpdate);
        GEvent.RegistEvent(GacEvent.UpdateFrameLogic, UpdateFrameLogic);
        GEvent.RegistEvent<Fix64>(GacEvent.UpdateFrameRender, UpdateFrameRender);
    }

    private void RemoveEvent()
    {
        GEvent.RemoveEvent(GacEvent.FixedUpdate, FixedUpdate);
        GEvent.RemoveEvent(GacEvent.UpdateFrameLogic, UpdateFrameLogic);
        GEvent.RemoveEvent<Fix64>(GacEvent.UpdateFrameRender, UpdateFrameRender);
    }

    public void FixedUpdate()
    {
        if (fsData.PauseState)
        {
            return;
        }
        LockStep.FixedUpdate();
    }
    public void LogicFrame(int logicFrame)
    {
        // if ((logicFrame - fsData.GameLogicFrame) > 20)
        // {
        //     GTime.TimeScale = 1.5f;
        // }
        // else
        // {
        //     GTime.TimeScale = 1;
        // }
    }

    /// <summary>
    /// 更新游戏逻辑
    /// </summary>
    private void UpdateFrameLogic()
    {
        //如果当前逻辑帧无可执行操作时、则暂停游戏、进行锁帧
        // if (fsData.GameLogicFrame >= fsData.PlayerOperationList.Count)
        // {
        //     // Debug.Log("fsData.GameLogicFrame >= fsData.PlayerOperationList.Count");
        //     fsData.GameLogicFrame = fsData.PlayerOperationList.Count - 1;
        //     fsData.PauseState = true;
        //     return;
        // }
        if (fsData.ReplayState)
        {
            Debug.Log("fsData.ReplayState");
        }
        else
        {
            this.fsData.FrameRunningTime = (int)(this.fsData.FixFrameLen * 1000 * this.fsData.GameLogicFrame);
            SeedRandom.Init(fsData.GameLogicFrame);

            // Unity物理刷新前调用（逻辑帧处理）
            MapManager.Instance.UpdatePreMapFrameSynLogic(fsData.PlayerOperationList[fsData.GameLogicFrame]);

#if UNITY_EDITOR
    if (!Physics.autoSimulation)
    {
            // Unity物理引擎刷新一次
            Physics.Simulate((float)fsData.FixFrameLen);

            // Unity物理刷新后调用
            MapManager.Instance.UpdatePostMapFrameSynLogic();
    }
#else
            // Unity物理引擎刷新一次
            Physics.Simulate((float)fsData.FixFrameLen);

            // Unity物理刷新后调用
            MapManager.Instance.UpdatePostMapFrameSynLogic();
#endif
        }
    }
 
    /// <summary>
    /// 更新渲染
    /// </summary>
    /// <param name="interpolation">两帧的时间差,用于补间动画</param>
    private void UpdateFrameRender(Fix64 interpolation)
    {
        MapManager.Instance.UpdateMapFrameSynRender(interpolation);
    }

    /// <summary>
    /// 发送玩家操作
    /// </summary>
    /// <param name="type">操作类型</param>
    /// <param name="playerID">玩家id</param>
    /// <param name="info">操作信息</param>
    public void SendPlayerOperation(FrameSynchronOperationType type, int playerID, string info)
    {
        if (fsData == null)
        {
            Debug.LogError("SendPlayerOperation Error!!!");
            return;
        }
        JArray oData = new JArray
        {
            type,
            playerID,
            info
        };
        //向服务器发送玩家操作
        Network.CSharpSend(GacToGas.G_FSOperationMsg, oData);
    }

    /// <summary>
    /// 保存玩家操作
    /// </summary>
    /// <param name="data">帧数据包</param>
    /// <param name="seed">随机种子</param>
    public void SavePlayerOperation(JArray data, int seed)
    {
        if (fsData == null || data == null)
        {
            Debug.LogError("SavePlayerOperation Error!!!");
            return;
        }
        fsData.SRandom = SeedRandom.New(seed);
        List<JArray> oData = new List<JArray>();
        for (int i = 0; i < data.Count; i++)
        {
            int logicFrame = (int)data[i][0];
            if(logicFrame!= fsData.PlayerOperationList.Count){
                //Debug.Log("logicFrame!= fsData.PlayerOperationList.Count" + logicFrame + ":" + fsData.PlayerOperationList.Count);
            }
            long time = (long)data[i][1];
            JArray op = (JArray)data[i][2];
            fsData.PlayerOperationList.Add(op);
        }
        fsData.PauseState = false;
        int difference = fsData.PlayerOperationList.Count - fsData.GameLogicFrame;
        // 追帧 临时代码
        //UnityEngine.Debug.Log("游戏逻辑帧 = " + fsData.GameLogicFrame + "  差帧" + difference);
        if(difference > data.Count + 1){
            float scale = difference / data.Count;
            scale = scale > 10 ? 10 : scale;
            GTime.TimeScale = scale;
        }else{
            GTime.TimeScale = 1;
        }
    }

    /// <summary>
    /// 保存战斗数据
    /// </summary>
    public void SaveBattleData()
    {
        if (fsData.PlayerOperationList.Count <= 0)
        {
            Debug.LogError("SaveBattleData Error!!!");
            return;
        }
        string battleRecord = JsonConvert.SerializeObject(fsData.PlayerOperationList);
        if (!Directory.Exists(PersistentDataPath))
        {
            Directory.CreateDirectory(PersistentDataPath);
        }
        string recordName = DateTime.Now.ToString("s") + ".bytes";
        recordName = recordName.Replace("T", "-");
        recordName = recordName.Replace(":", "-");
        File.WriteAllText(PersistentDataPath + "/" + recordName, battleRecord);
    }

    /// <summary>
    /// 获取战斗数据文件名列表
    /// </summary>
    /// <returns></returns>
    private List<string> GetBattleDataFilesName()
    {
        if (!Directory.Exists(PersistentDataPath))
        {
            Debug.LogError("GetBattleDataFilesName Error!!!");
            return null;
        }

        List<string> ret = new List<string>();

        string[] files = Directory.GetFiles(PersistentDataPath);
        foreach (string file in files)
        {
            string strName = Path.GetFileNameWithoutExtension(file);
            ret.Add(strName);
        }
        return ret;
    }

    /// <summary>
    /// 加载战斗数据
    /// </summary>
    /// <param name="recordName">文件名称</param>
    /// <returns></returns>
    private IEnumerator LoadBattleData(string recordName)
    {
        if (string.IsNullOrEmpty(recordName))
        {
            Debug.LogError("LoadBattleData Error!!!");
            yield break;
        }

        string filePath = PersistentDataPath + "/" + recordName + ".bytes";
        if (!File.Exists(filePath))
        {
            Debug.LogError("LoadBattleData Error!!!");
            yield break;
        }

        UnityWebRequest www = UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();
        if (www.error != null || www.responseCode != 200)
        {
            www.Dispose();
            Debug.LogError("LoadBattleData Error!!!");
            yield break;
        }

        string battleRecord = www.downloadHandler.text;
        if (string.IsNullOrEmpty(battleRecord))
        {
            Debug.LogError("LoadBattleData Error");
            yield break;
        }
        fsData.PlayerBackOperationList = JsonConvert.DeserializeObject<List<JArray>>(battleRecord);
    }

    public int GetGameLogicFrame()
    {
        return fsData.GameLogicFrame;
    }
}