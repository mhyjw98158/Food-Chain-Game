using System.Collections.Generic;
using UnityEngine;

// 결과 화면 본판: 왼쪽(밝은 쪽)에 승리자, 오른쪽(어두운 쪽)에 패배자를 놓고 모두의 직업을 공개한다.
// 한쪽 인원이 singleRowMax 이하면 한 줄, 넘으면 두 줄(홀수면 윗줄이 한 명 더 많다)로 놓는다
public class ResultBoardUI : MonoBehaviour
{
    [SerializeField] private ResultSlotUI slotPrefab;

    [Header("Rows")]
    [SerializeField] private RectTransform winnerTopRow;
    [SerializeField] private RectTransform winnerBottomRow;
    [SerializeField] private RectTransform loserTopRow;
    [SerializeField] private RectTransform loserBottomRow;

    [Tooltip("한쪽 인원이 이 수 이하면 한 줄로, 넘으면 두 줄로 놓는다")]
    [SerializeField] private int singleRowMax = 6;

    private readonly List<GameObject> _slots = new();

    public void Show(GameResultEntry[] entries, uint localNetId)
    {
        Clear();

        var winners = new List<GameResultEntry>();
        var losers = new List<GameResultEntry>();
        if (entries != null)
        {
            foreach (var entry in entries)
                (entry.isWinner ? winners : losers).Add(entry);
        }

        Place(winners, winnerTopRow, winnerBottomRow, localNetId);
        Place(losers, loserTopRow, loserBottomRow, localNetId);
    }

    private void Place(List<GameResultEntry> side, RectTransform topRow, RectTransform bottomRow, uint localNetId)
    {
        if (topRow == null || slotPrefab == null) return;

        bool twoRows = side.Count > singleRowMax && bottomRow != null;
        int topCount = twoRows ? (side.Count + 1) / 2 : side.Count;

        topRow.gameObject.SetActive(side.Count > 0);
        if (bottomRow != null) bottomRow.gameObject.SetActive(twoRows);

        for (int i = 0; i < side.Count; i++)
        {
            var row = i < topCount ? topRow : bottomRow;
            var slot = Instantiate(slotPrefab, row);
            slot.Setup(side[i], side[i].netId == localNetId);
            _slots.Add(slot.gameObject);
        }
    }

    private void Clear()
    {
        foreach (var slot in _slots)
        {
            if (slot != null) Destroy(slot);
        }
        _slots.Clear();
    }
}
