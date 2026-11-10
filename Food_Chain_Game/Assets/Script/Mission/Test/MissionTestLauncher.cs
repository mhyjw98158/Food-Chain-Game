using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mission Test 씬 전용. 그룹 탭으로 한 그룹의 미션 버튼만 보여 주고, 버튼을 누르면
/// 게임(MissionUIManager)과 같은 방식으로 미션 프리팹을 새로 만들어 화면 전체 영역에서 시작한다.
/// 성공·실패·ESC·닫기 버튼으로 미션이 끝나면 지우고 버튼 화면으로 돌아온다.
/// </summary>
public class MissionTestLauncher : MonoBehaviour
{
    [Serializable]
    public class Group
    {
        public string label;
        public GameObject buttonGroup;
        public Button tabButton;
    }

    [Header("Refs")]
    [Tooltip("미션이 만들어지는 부모(캔버스 전체를 채우는 RectTransform)")]
    [SerializeField] private RectTransform missionRoot;
    [Tooltip("미션 진행 중에는 숨길 버튼 화면(탭 + 버튼 묶음)")]
    [SerializeField] private GameObject[] menuObjects;
    [Tooltip("미션 진행 중에만 보이는 닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("Groups")]
    [SerializeField] private List<Group> groups = new List<Group>();
    [SerializeField] private int startGroup = 0;
    [SerializeField] private Color tabNormalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("Input")]
    [SerializeField] private KeyCode abortKey = KeyCode.Escape;

    private BaseMission _active;
    private int _currentGroup = -1;

    private void Awake()
    {
        for (int i = 0; i < groups.Count; i++)
        {
            int idx = i;
            if (groups[i] != null && groups[i].tabButton != null)
                groups[i].tabButton.onClick.AddListener(() => ShowGroup(idx));
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Abort);
    }

    private void Start()
    {
        SetMenuVisible(true);
        ShowGroup(Mathf.Clamp(startGroup, 0, Mathf.Max(0, groups.Count - 1)));
    }

    private void Update()
    {
        if (_active != null && Input.GetKeyDown(abortKey))
            Abort();
    }

    public void ShowGroup(int index)
    {
        _currentGroup = index;
        for (int i = 0; i < groups.Count; i++)
        {
            Group g = groups[i];
            if (g == null) continue;

            if (g.buttonGroup != null)
                g.buttonGroup.SetActive(i == index);

            if (g.tabButton != null)
            {
                Image img = g.tabButton.GetComponent<Image>();
                if (img != null) img.color = i == index ? tabSelectedColor : tabNormalColor;
            }
        }
    }

    // 버튼 OnClick에 미션 프리팹을 인자로 연결해서 쓴다
    public void Launch(BaseMission prefab)
    {
        if (prefab == null || missionRoot == null)
        {
            Debug.LogError("[MissionTest] 미션 프리팹 또는 missionRoot가 비어 있음");
            return;
        }

        DestroyActive();

        BaseMission instance = Instantiate(prefab, missionRoot);
        instance.name = prefab.name;
        _active = instance;
        instance.gameObject.SetActive(true);

        instance.OnMissionCompleted = m => Finish(m, "성공");
        instance.OnMissionFailed = m => Finish(m, "실패");

        SetMenuVisible(false);
        instance.Begin();
    }

    public void Abort()
    {
        if (_active == null) return;
        Debug.Log($"[MissionTest] {_active.name} 닫음");
        DestroyActive();
        SetMenuVisible(true);
    }

    private void Finish(BaseMission mission, string result)
    {
        if (mission == null || mission != _active) return;
        Debug.Log($"[MissionTest] {mission.name} {result}");
        DestroyActive();
        SetMenuVisible(true);
    }

    private void DestroyActive()
    {
        if (_active == null) return;
        Destroy(_active.gameObject);
        _active = null;
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuObjects != null)
        {
            for (int i = 0; i < menuObjects.Length; i++)
                if (menuObjects[i] != null) menuObjects[i].SetActive(visible);
        }

        if (closeButton != null)
            closeButton.gameObject.SetActive(!visible);

        if (visible && _currentGroup >= 0)
            ShowGroup(_currentGroup);
    }
}
