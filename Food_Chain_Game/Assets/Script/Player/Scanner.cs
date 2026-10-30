using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public GamePlayer myPlayer;

    public enum ScanTargetType { None, Player, Mission, Corpse, Investigation, Disguise, Predict }

    [Header("Interaction Target")]
    public ScanTargetType currentType = ScanTargetType.None;
    public ScanTargetType CurrentType => currentType;

    public Component currentInteractTarget;
    public Component CurrentInteractTarget => currentInteractTarget;
    public uint targetNetId;
    public uint CurrentTargetNetId => targetNetId;
    public GamePlayer currentTarget;
    public GamePlayer prevTarget;
    public HashSet<GamePlayer> playersInRange = new();

    public MissionObject mission;
    public MissionObject CurrentMission => mission;

    public Corpse corpse;
    public Corpse CurrentTargetCorpse => corpse;
    public HashSet<Corpse> corpsesInRange = new();
    public InvestigationObject investigation;
    public InvestigationObject CurrentInvestigation => investigation;
    public DisguiseObject disguise;
    public DisguiseObject CurrentDisguise => disguise;
    public PredictObject predict;
    public PredictObject CurrentPredict => predict;

    public float scanRadius = 1.2f;
    public LayerMask scanLayers;

    public float updateInterval = 0.1f;
    private float timer = 0f;

    [Header("Interact Prompt")]
    [Tooltip("키 안내(\"[키] ○○ 시작\")에 표시할 행동 이름")]
    [SerializeField] private string attackActionName = "사냥";
    [SerializeField] private string missionActionName = "미션";
    [SerializeField] private string corpseActionName = "부검";
    [SerializeField] private string investigationActionName = "탐색";
    [SerializeField] private string disguiseActionName = "위장";
    [SerializeField] private string predictActionName = "예측";

    // 지금 테두리를 켜 둔 대상
    private Component _hintTarget;

    private void Update()
    {
        if (myPlayer == null || !myPlayer.isLocalPlayer) return;       

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            ScanArea();
        }
    }
    private void ScanArea()
    {
        playersInRange.Clear();
        corpsesInRange.Clear();

        currentTarget = null;
        targetNetId = 0;
        mission = null;
        corpse = null;
        investigation = null;
        disguise = null;
        predict = null;

        currentType = ScanTargetType.None;
        currentInteractTarget = null;

        Vector2 center = transform.position;
        float bestDist = Mathf.Infinity;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, scanRadius, scanLayers);
        bool canAttack = CanAttackNow();

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(center, hit.transform.position);

            if (hit.TryGetComponent(out GamePlayer gp))
            {
                if (canAttack && gp != myPlayer && gp.isAlive)
                {
                    playersInRange.Add(gp);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Player;
                        currentInteractTarget = gp;

                        currentTarget = gp;
                        targetNetId = gp.netId;

                        mission = null;
                        corpse = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }

            if (hit.TryGetComponent(out MissionObject obj))
            {
                if (CanInteractMission(obj))
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Mission;
                        currentInteractTarget = obj;

                        mission = obj;

                        currentTarget = null;
                        targetNetId = 0;
                        corpse = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            if (hit.TryGetComponent(out Corpse targetCorpse))
            {
                if (myPlayer.isAlive && myPlayer.IsHelper() && !myPlayer.HasScannedCorpse(targetCorpse))
                {
                    corpsesInRange.Add(targetCorpse);

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Corpse;
                        currentInteractTarget = targetCorpse;

                        corpse = targetCorpse;

                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            if (hit.TryGetComponent(out InvestigationObject inv))
            {
                if (myPlayer.isAlive)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Investigation;
                        currentInteractTarget = inv;

                        investigation = inv;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        disguise = null;
                        predict = null;
                    }
                }
            }
            // 위장 오브젝트는 카멜레온에게만 상호작용 대상이 된다(역할은 본인만 알므로 다른 플레이어에게 드러나지 않음)
            if (hit.TryGetComponent(out DisguiseObject disguiseObj))
            {
                if (myPlayer.isAlive && myPlayer.CanDisguise)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Disguise;
                        currentInteractTarget = disguiseObj;

                        disguise = disguiseObj;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        predict = null;
                    }
                }
            }
            // 승리 예측 오브젝트는 예측 능력이 있는 플레이어(올빼미)에게만 상호작용 대상이 된다
            if (hit.TryGetComponent(out PredictObject predictObj))
            {
                if (myPlayer.isAlive && myPlayer.CanPredict)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        currentType = ScanTargetType.Predict;
                        currentInteractTarget = predictObj;

                        predict = predictObj;

                        corpse = null;
                        currentTarget = null;
                        targetNetId = 0;
                        mission = null;
                        investigation = null;
                        disguise = null;
                    }
                }
            }
        }


        UpdateInteractUI(currentInteractTarget);
    }
    // 공격할 수 있을 때만 플레이어를 대상으로 잡는다(Kill UI도 이때만 뜬다).
    // 실제 판정은 서버(GamePlayer.CmdAttack)가 같은 조건으로 한다. hasAttacked는 본인에게 동기화되는 값
    private bool CanAttackNow()
    {
        if (!myPlayer.isAlive || !myPlayer.isPredator || myPlayer.hasAttacked) return false;

        return GameMamager.Instance != null && GameMamager.Instance.IsNightPhase;
    }
    private bool CanInteractMission(MissionObject obj)
    {
        if (obj == null) return false;
        if (!myPlayer.CanDoMissions) return false;

        return myPlayer.CanStartMissionType(obj.MissionType);
    }
    public void UpdateInteractUI(Component target)
    {
        if (prevTarget != null && prevTarget != currentTarget)
            prevTarget.SetKillUI(false);

        UpdateInteractHint();

        if(target is GamePlayer gp)
        {
            if(myPlayer != null && myPlayer.isPredator)
            {
                gp.SetKillUI(true);
            }
            prevTarget = currentTarget;
        }
    }

    // 테두리와 키 안내는 상호작용 키가 지금 실행할 대상 하나만 따라간다(PlayerMove.Interaction과 같은 조건)
    private void UpdateInteractHint()
    {
        Component target = CanShowInteractHint() ? currentInteractTarget : null;

        if (target != _hintTarget)
        {
            if (_hintTarget != null) InteractOutline.Of(_hintTarget).SetVisible(false);
            if (target != null) InteractOutline.Of(target).SetVisible(true);
            _hintTarget = target;
        }

        if (target != null) InteractPromptUI.Show(GetActionName());
        else InteractPromptUI.Hide();
    }

    private bool CanShowInteractHint()
    {
        if (UIManager.Instance == null || !UIManager.Instance.CanInteract() || ChatInputFocus.IsFocused) return false;

        return GameMamager.Instance != null && GameMamager.Instance.isRoundActive;
    }

    private string GetActionName()
    {
        switch (currentType)
        {
            case ScanTargetType.Player: return attackActionName;
            case ScanTargetType.Mission: return missionActionName;
            case ScanTargetType.Corpse: return corpseActionName;
            case ScanTargetType.Investigation: return investigationActionName;
            case ScanTargetType.Disguise: return disguiseActionName;
            case ScanTargetType.Predict: return predictActionName;
            default: return string.Empty;
        }
    }
}
