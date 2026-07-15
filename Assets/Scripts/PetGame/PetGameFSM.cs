using F8Framework.Core;
using F8Framework.Launcher;
using UnityEngine;

/// <summary>
/// 铲屎官疯了 — 游戏状态机（F8Framework FSM）
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
    protected PetGameManager gm;
    protected IFSM<PetGameManager> fsm;

    public override void OnInitialization(IFSM<PetGameManager> f)
    {
        fsm = f;
        gm = f.Owner;
    }

    public override void OnStateUpdate(IFSM<PetGameManager> f) { }
    public override void OnStateLateUpdate(IFSM<PetGameManager> f) { }
    public override void OnStateFixedUpdate(IFSM<PetGameManager> f) { }
}

// ===== Idle — 等玩家选碗 =====
public class IdleState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f)
    {
        gm.selectedBowlId = -1;
        gm.OnBowlClicked = bowlId =>
        {
            gm.selectedBowlId = bowlId;
            gm.onSelectionChanged.Invoke();
            fsm.ChangeState<SelectedState>();
        };
    }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}

// ===== Selected — 已选中一个碗，等选目标 =====
public class SelectedState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f)
    {
        gm.OnBowlClicked = bowlId =>
        {
            if (bowlId == gm.selectedBowlId)
            {
                gm.selectedBowlId = -1;
                gm.onSelectionChanged.Invoke();
                fsm.ChangeState<IdleState>();
                return;
            }
            gm.PourFromTo(gm.selectedBowlId, bowlId, fsm);
        };
    }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}

// ===== Pouring / Feeding — 动画中无响应 =====
public class PouringState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f) { gm.OnBowlClicked = _ => { }; }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}
public class FeedingState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f) { gm.OnBowlClicked = _ => { }; }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}

// ===== Win / Fail =====
public class WinState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f)
    {
        gm.OnBowlClicked = _ => { };
        // 通关结算：存档 + 发奖励
        var result = gm.OnLevelWin();
        gm.onLevelComplete.Invoke(result.stars);
    }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}
public class FailState : PetGameState
{
    public override void OnStateEnter(IFSM<PetGameManager> f)
    {
        gm.OnBowlClicked = _ => { };
        gm.onLevelFail.Invoke();
    }
    public override void OnStateExit(IFSM<PetGameManager> f) { }
}
