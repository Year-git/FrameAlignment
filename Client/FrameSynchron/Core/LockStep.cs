using Framework;

/// <summary>
/// 帧同步核心
/// </summary>
public class LockStep
{
    //累计运行的时间
    public Fix64 AccumilatedTime{get;private set;} = 0;
    //下一个逻辑帧的时间
    public Fix64 NextGameTime{get;private set;} = 0;
    //预定的每帧的时间长度
    public Fix64 FrameLen{get;private set;}
    //两帧之间的时间差
    public Fix64 Interpolation{get;private set;} = 0;
    //数据
    public FrameSynchronData FsData{get;private set;}

    public LockStep(FrameSynchronData param)
    {
        FsData = param;
        Start();
    }

    public void Start()
    {
        FrameLen = (float)FsData.FixFrameLen;
        AccumilatedTime = 0;
        NextGameTime = 0;
        Interpolation = 0;
    }

    public void FixedUpdate()
    {
        Fix64 deltaTime = GTime.FixedDeltaTime;

        /**************以下是帧同步的核心逻辑*********************/
        AccumilatedTime = AccumilatedTime + deltaTime;

        //如果真实累计的时间超过游戏帧逻辑原本应有的时间,则循环执行逻辑,确保整个逻辑的运算不会因为帧间隔时间的波动而计算出不同的结果
        while (AccumilatedTime > NextGameTime)
        {
            // -----------------------------------
            // 替换帧同步的数据同步
            LocalFrameSynServer.SynFrameData();
            // -----------------------------------

            //运行与游戏相关的具体逻辑
            GEvent.DispatchEvent(GacEvent.UpdateFrameLogic);
            //计算下一个逻辑帧应有的时间
            NextGameTime += FrameLen;
            //游戏逻辑帧自增
            FsData.GameLogicFrame += 1;
        }

        //计算两帧的时间差,用于补间动画（interpolation是从0渐变到1的值，为什么？请查看Vector3.Lerp的用法）
        Interpolation = (AccumilatedTime + FrameLen - NextGameTime) / FrameLen;

        //更新渲染
        GEvent.DispatchEvent(GacEvent.UpdateFrameRender, Interpolation);
        /**************帧同步的核心逻辑完毕*********************/
    }
}