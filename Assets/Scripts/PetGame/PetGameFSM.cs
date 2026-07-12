using F8Framework.Core;
using UnityEngine;

/// <summary>
/// 铲屎官疯了 — 游戏状态机（基于 F8Framework FSM）
/// Owner: PetGameManager
/// </summary>
public static class PetGameFSM
{
    public static IFSM<PetGameManager> Create(PetGameManager gm)
    {
        var idle     = new IdleState();
        var selected = new SelectedState();
        var pouring  = new PouringState();
        var feeding  = new FeedingState();
        var win      = new WinState();
        var fail     = new FailState();

        var fsm = FSMManager.Instance.CreateFSM<PetGameManager>("PetGame", gm,
            idle, selected, pouring, feeding, win, fail);
        fsm.DefaultState = idle;
        fsm.ChangeToDefaultState();
        return fsm;
    }
}

// ===== 状态基类 =====
public abstract class PetGameState : FSMState<PetGameManager>
{
    private PetGameManager _gm;
    protected PetGameManager gm => _gm;

    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        _gm = fsm.Owner;
    }

    public override void OnStateUpdate(IFSM<PetGameManager> fsm) { }
    public override void OnStateLateUpdate(IFSM<PetGameManager> fsm) { }
    public override void OnStateFixedUpdate(IFSM<PetGameManager> fsm) { }
}

// ===== Idle — 等玩家选碗 =====
public class IdleState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.selectedBowlId = -1;
        gm.OnBowlClicked = bowlId =>
        {
            gm.selectedBowlId = bowlId;
            fsm.ChangeState<SelectedState>();
        };
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}

// ===== Selected — 已选中一个碗，等选目标 =====
public class SelectedState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.OnBowlClicked = bowlId =>
        {
            if (bowlId == gm.selectedBowlId)
            {
                // 取消选中 → 回到 Idle
                gm.selectedBowlId = -1;
                fsm.ChangeState<IdleState>();
                return;
            }
            // 倒入：selected → bowlId
            gm.PourFromTo(gm.selectedBowlId, bowlId, fsm);
        };
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}

// ===== Pouring — 倒食物动画中 =====
public class PouringState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.OnBowlClicked = _ => { }; // 动画中无响应
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}

// ===== Feeding — 喂食动画中 =====
public class FeedingState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.OnBowlClicked = _ => { };
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}

// ===== Win / Fail =====
public class WinState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.OnBowlClicked = _ => { };
        gm.onLevelComplete.Invoke(gm.CalcStars());
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}

public class FailState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> fsm)
    {
        gm.OnBowlClicked = _ => { };
        gm.onLevelFail.Invoke();
    }
    public override void OnStateExit(IFSM<PetGameManager> fsm) { }
}
