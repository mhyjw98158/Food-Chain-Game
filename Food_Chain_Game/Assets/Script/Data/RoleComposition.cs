using System.Collections.Generic;
using UnityEngine;

// 역할 구성표(Resources/RoleComposition.asset).
// 게임 시작 때 서버가 인원수와 방장 설정(포식자 수·중립 수, 자동이면 아래 표)으로 이번 판에 나올 역할을 정한다.
// - 포식자: "항상 포함"(늑대)을 먼저 넣고 나머지는 무작위
// - 중립: 무작위
// - 나머지 자리: 피식자와 조력자 중 무작위. 조력자는 필요한 역할(까마귀 → 늑대, 악어새 → 악어)이 있을 때만,
//   짝 역할(얼룩말·타조)은 둘이 함께 들어갈 자리가 있을 때만 나온다
// - 나머지 자리가 모자라면(후보보다 인원이 많으면) 남은 중립 → 남은 포식자 순으로 더 넣는다
[CreateAssetMenu(fileName = "RoleComposition", menuName = "Game/Role Composition")]
public class RoleComposition : ScriptableObject
{
    private const string ResourcePath = "RoleComposition";

    // 설정 값에서 "자동"(인원별 표를 따른다)
    public const int Auto = -1;

    public enum RoleCategory { Predator, Neutral, Prey, Helper }

    [System.Serializable]
    public struct RoleEntry
    {
        public AnimalType animal;
        public RoleCategory category;
        [Tooltip("포식자 중 항상 넣는 역할")]
        public bool alwaysInclude;
        [Tooltip("이 역할이 있을 때만 나온다(None이면 조건 없음)")]
        public AnimalType requires;
        [Tooltip("이 역할과 함께만 나온다(None이면 혼자 나올 수 있음). 양쪽에 서로를 적는다")]
        public AnimalType pairWith;

        public RoleEntry(AnimalType animal, RoleCategory category, bool alwaysInclude = false,
            AnimalType requires = AnimalType.None, AnimalType pairWith = AnimalType.None)
        {
            this.animal = animal;
            this.category = category;
            this.alwaysInclude = alwaysInclude;
            this.requires = requires;
            this.pairWith = pairWith;
        }
    }

    // minPlayers명 이상이면 count. 위에서부터 차례로 보고 마지막으로 맞는 줄을 쓴다
    [System.Serializable]
    public struct CountStep
    {
        public int minPlayers;
        public int count;

        public CountStep(int minPlayers, int count)
        {
            this.minPlayers = minPlayers;
            this.count = count;
        }
    }

    [SerializeField] private List<RoleEntry> roles = new List<RoleEntry>
    {
        new RoleEntry(AnimalType.Wolf, RoleCategory.Predator, alwaysInclude: true),
        new RoleEntry(AnimalType.Crocodile, RoleCategory.Predator),
        new RoleEntry(AnimalType.Hawk, RoleCategory.Predator),
        new RoleEntry(AnimalType.Hyena, RoleCategory.Predator, requires: AnimalType.Wolf),
        new RoleEntry(AnimalType.Scorpion, RoleCategory.Neutral),
        new RoleEntry(AnimalType.Owl, RoleCategory.Neutral),
        new RoleEntry(AnimalType.Chameleon, RoleCategory.Neutral),
        new RoleEntry(AnimalType.Zebra, RoleCategory.Prey, pairWith: AnimalType.Ostrich),
        new RoleEntry(AnimalType.Ostrich, RoleCategory.Prey, pairWith: AnimalType.Zebra),
        new RoleEntry(AnimalType.Hippo, RoleCategory.Prey),
        new RoleEntry(AnimalType.Meerkat, RoleCategory.Prey),
        new RoleEntry(AnimalType.Crow, RoleCategory.Helper, requires: AnimalType.Wolf),
        new RoleEntry(AnimalType.Plover, RoleCategory.Helper, requires: AnimalType.Crocodile),
    };

    [Header("자동일 때 인원별 수")]
    [SerializeField] private List<CountStep> autoPredators = new List<CountStep>
    {
        new CountStep(1, 1), new CountStep(6, 2), new CountStep(9, 3), new CountStep(12, 4),
    };
    [SerializeField] private List<CountStep> autoNeutrals = new List<CountStep>
    {
        new CountStep(1, 0), new CountStep(6, 1), new CountStep(10, 2),
    };

    private static RoleComposition _instance;

    public static RoleComposition Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<RoleComposition>(ResourcePath);
                if (_instance == null)
                {
                    Debug.LogWarning($"[RoleComposition] Resources/{ResourcePath} 가 없습니다. 기본 구성표를 씁니다.");
                    _instance = CreateInstance<RoleComposition>();
                }
            }
            return _instance;
        }
    }

    public int CountOf(RoleCategory category)
    {
        int count = 0;
        foreach (var role in roles)
        {
            if (role.category == category && role.animal != AnimalType.None) count++;
        }
        return count;
    }

    // 인원수와 설정으로 실제 포식자 수·중립 수를 정한다(로비 표시와 배정이 함께 쓴다).
    // 포식자는 1명 이상, 포식자가 아닌 사람이 1명 이상 남게(1명일 때 제외), 중립은 남는 자리 안에서
    public void ResolveCounts(int playerCount, int predatorSetting, int neutralSetting, out int predators, out int neutrals)
    {
        playerCount = Mathf.Max(1, playerCount);

        predators = predatorSetting == Auto ? Lookup(autoPredators, playerCount) : predatorSetting;
        int maxPredators = Mathf.Min(CountOf(RoleCategory.Predator), Mathf.Max(1, playerCount - 1));
        predators = Mathf.Clamp(predators, 1, Mathf.Max(1, maxPredators));

        neutrals = neutralSetting == Auto ? Lookup(autoNeutrals, playerCount) : neutralSetting;
        int maxNeutrals = Mathf.Min(CountOf(RoleCategory.Neutral), Mathf.Max(0, playerCount - predators));
        neutrals = Mathf.Clamp(neutrals, 0, maxNeutrals);
    }

    // 이번 판에 나올 역할 이름(CharacterZoneData 키) 목록. 인원수만큼 돌려준다(역할이 모자라면 중복이 생긴다)
    public List<string> Build(int playerCount, int predatorSetting, int neutralSetting)
    {
        playerCount = Mathf.Max(1, playerCount);
        ResolveCounts(playerCount, predatorSetting, neutralSetting, out int predatorCount, out int neutralCount);

        var chosen = new List<AnimalType>();

        // 포식자: 항상 포함 → 나머지 무작위(필요한 역할이 이미 있는 것만)
        foreach (var role in roles)
        {
            if (chosen.Count >= predatorCount) break;
            if (role.category == RoleCategory.Predator && role.alwaysInclude) chosen.Add(role.animal);
        }
        AddRandom(chosen, RoleCategory.Predator, predatorCount - chosen.Count);

        AddRandom(chosen, RoleCategory.Neutral, neutralCount);

        // 나머지 자리: 피식자 + 조력자. 짝 역할은 두 자리를 함께 쓴다
        // 피식자·조력자가 모자라면 남은 중립 → 남은 포식자를 하나씩 더 넣고 다시 채운다
        // (포식자가 더해지면 그 조력자도 나올 수 있게 된다. 예: 악어 → 악어새)
        int remaining = playerCount - chosen.Count;
        while (remaining > 0)
        {
            var units = FillerUnits(chosen, remaining);
            if (units.Count > 0)
            {
                var unit = units[Random.Range(0, units.Count)];
                chosen.AddRange(unit);
                remaining -= unit.Count;
                continue;
            }

            // 한 자리만 남았는데 짝 묶음만 남았으면, 혼자 들어간 피식자·조력자 하나를 빼고 짝을 넣는다
            if (remaining == 1 && TrySwapSoloForPair(chosen))
            {
                remaining--;
                continue;
            }

            if (AddRandom(chosen, RoleCategory.Neutral, 1) == 0 && AddRandom(chosen, RoleCategory.Predator, 1) == 0) break;
            remaining--;
        }

        var result = new List<string>(playerCount);
        foreach (var animal in chosen) result.Add(animal.ToString());

        if (remaining > 0)
        {
            Debug.LogWarning($"[RoleComposition] 역할({result.Count}종)보다 인원({playerCount}명)이 많아 역할이 중복됩니다.");
            int unique = result.Count;
            for (int i = 0; i < remaining && unique > 0; i++) result.Add(result[Random.Range(0, unique)]);
        }
        return result;
    }

    // category에서 아직 안 뽑은 역할을 최대 count개 무작위로 넣는다. 넣은 수를 돌려준다
    private int AddRandom(List<AnimalType> chosen, RoleCategory category, int count)
    {
        int added = 0;
        while (added < count)
        {
            var candidates = new List<AnimalType>();
            foreach (var role in roles)
            {
                if (role.category != category || role.animal == AnimalType.None || chosen.Contains(role.animal)) continue;
                if (role.requires != AnimalType.None && !chosen.Contains(role.requires)) continue;
                candidates.Add(role.animal);
            }
            if (candidates.Count == 0) break;

            chosen.Add(candidates[Random.Range(0, candidates.Count)]);
            added++;
        }
        return added;
    }

    // 지금 넣을 수 있는 피식자·조력자 묶음(혼자 또는 짝). 남은 자리에 들어가는 것만
    private List<List<AnimalType>> FillerUnits(List<AnimalType> chosen, int remaining)
    {
        var units = new List<List<AnimalType>>();
        foreach (var role in roles)
        {
            if (role.category != RoleCategory.Prey && role.category != RoleCategory.Helper) continue;
            if (role.animal == AnimalType.None || chosen.Contains(role.animal)) continue;
            if (role.requires != AnimalType.None && !chosen.Contains(role.requires)) continue;

            // 짝이 목록에 없으면 혼자 나올 수 있는 역할로 본다
            if (role.pairWith == AnimalType.None || IndexOf(role.pairWith) == int.MaxValue)
            {
                units.Add(new List<AnimalType> { role.animal });
                continue;
            }

            // 짝은 목록에서 먼저 나오는 쪽이 묶음을 대표한다(같은 묶음이 두 번 들어가지 않게)
            if (remaining < 2 || chosen.Contains(role.pairWith) || IndexOf(role.pairWith) < IndexOf(role.animal)) continue;
            units.Add(new List<AnimalType> { role.animal, role.pairWith });
        }
        return units;
    }

    private bool TrySwapSoloForPair(List<AnimalType> chosen)
    {
        List<AnimalType> pair = null;
        foreach (var unit in FillerUnits(chosen, 2))
        {
            if (unit.Count == 2) { pair = unit; break; }
        }
        if (pair == null) return false;

        var solos = new List<AnimalType>();
        foreach (var role in roles)
        {
            if (role.category != RoleCategory.Prey && role.category != RoleCategory.Helper) continue;
            if (!chosen.Contains(role.animal) || IsRequiredByChosen(chosen, role.animal)) continue;
            if (role.pairWith != AnimalType.None && IndexOf(role.pairWith) != int.MaxValue) continue;
            solos.Add(role.animal);
        }
        if (solos.Count == 0) return false;

        chosen.Remove(solos[Random.Range(0, solos.Count)]);
        chosen.AddRange(pair);
        return true;
    }

    private bool IsRequiredByChosen(List<AnimalType> chosen, AnimalType animal)
    {
        foreach (var role in roles)
        {
            if (role.requires == animal && chosen.Contains(role.animal)) return true;
        }
        return false;
    }

    private int IndexOf(AnimalType animal)
    {
        for (int i = 0; i < roles.Count; i++)
        {
            if (roles[i].animal == animal) return i;
        }
        return int.MaxValue;
    }

    private static int Lookup(List<CountStep> steps, int playerCount)
    {
        int count = 0;
        foreach (var step in steps)
        {
            if (playerCount >= step.minPlayers) count = step.count;
        }
        return count;
    }
}
