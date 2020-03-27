-- global function
local print = print
local pairs = pairs
local ipairs = ipairs
local tostring = tostring
local math_random = math.random
local table_insert = table.insert

-- local
local CFrameSynchron = RequireClass("CFrameSynchron")

function CFrameSynchron:_constructor(i_oPlayer)
    --玩家对象
    self.oPlayer = i_oPlayer
    --开启状态
    self.bStart = false
    --每一帧的间隔时间（毫秒）
    self.frameLen = 66.6666667
    --关键帧对应的逻辑帧节点
    self.keyFrameLen = 5
    --真实累计的时间
    self.accumilatedTime = 0
    --下一帧的时间节点
    self.nextFrameTime = 0
    --关键帧
    self.keyFrame = 0
    --逻辑帧
    self.logicFrame = 0
    --逻辑帧操作列表
    self.logicFrameOperation = {}
    --关键帧操作列表
    self.keyFrameOperation = {}
    --所有操作列表
    self.allOperation = {}
end

--深拷贝
local function DeepCopy(orig)
    local copy
    if type(orig) == "table" then
        copy = {}
        for orig_key, orig_value in next, orig, nil do
            copy[DeepCopy(orig_key)] = DeepCopy(orig_value)
        end
        setmetatable(copy, DeepCopy(getmetatable(orig)))
    else
        copy = orig
    end
    return copy
end

--开始帧同步
function CFrameSynchron:Start()
    self.bStart = true
    self.oPlayer:SendToClient("C_StartFrameSynchron")
end

--结束帧同步
function CFrameSynchron:End()
    self.bStart = false
end

--帧同步核心
function CFrameSynchron:Update(i_nDeltaMsec)
    if not self.bStart then
        return
    end

    local deltaTime = i_nDeltaMsec
    self.accumilatedTime = self.accumilatedTime + deltaTime
    while (self.accumilatedTime > self.nextFrameTime) do
        --将当前帧操作存入关键帧操作中
        table_insert(self.keyFrameOperation, DeepCopy(self.logicFrameOperation))
        self.logicFrameOperation = {}
        --计算下一个逻辑帧应有的时间
        self.nextFrameTime = self.nextFrameTime + self.frameLen
        --游戏逻辑帧自增
        self.logicFrame = self.logicFrame + 1

        if self.logicFrame % self.keyFrameLen == 0 then
            self.keyFrame = self.keyFrame + 1
            self:SendOperation()
        end
    end
end

--保存操作
function CFrameSynchron:SaveOperation(t_Data)
    table_insert(self.logicFrameOperation, t_Data)
end

--发送一个关键帧的操作
function CFrameSynchron:SendOperation()
    --创建随机种子
    local seed = math_random(1, 1000)
    self.oPlayer:SendToClient("C_PlayerOperationMsg", self.keyFrame, self.keyFrameOperation, seed)
    table_insert(self.allOperation, DeepCopy(self.keyFrameOperation))
    self.keyFrameOperation = {}
end

------------------------------------------------------------------------------------------------------
--接收客户端操作信息
DefineC.G_FSOperationMsg = function(i_oPlayer, t_Data)
    print("KernelServer G_FSOperationMsg")
    CFrameSynchron:SaveOperation(t_Data)
end
