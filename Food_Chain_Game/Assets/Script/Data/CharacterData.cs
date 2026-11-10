using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterData
{

    public enum TerritoryType
    {
        Sky, River, Field, Forest
    }

    public static class CharacterConfig
    {
        public static readonly Dictionary<AnimalType, CharacterInfoData> Characters = new()
        {
            { AnimalType.Wolf, new CharacterInfoData("늑대", TerritoryType.Field, "포식자 서열 1위. 라운드마다 한 번,\n다른 동물을 공격해 잡아먹을 수 있습니다.", "동물들을 잡아먹으며 생존하세요.\n한 라운드라도 굶으면 사망합니다.") },
            { AnimalType.Crocodile, new CharacterInfoData("악어", TerritoryType.River, "포식자 서열 2위. 라운드마다 한 번,\n늑대를 뺀 동물을 공격할 수 있습니다.", "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Hawk, new CharacterInfoData("매", TerritoryType.Sky, "포식자 서열 3위. 라운드마다 한 번,\n자신보다 서열이 낮은 동물을 공격할 수 있습니다.", "동물들을 잡아먹으며 생존하세요.\n두 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Hyena, new CharacterInfoData("하이에나", TerritoryType.Field, "포식자 서열 4위. 라운드마다 한 번,\n피식자를 공격할 수 있습니다.", "늑대가 사망하면 승리합니다.\n세 라운드를 연속으로 굶으면 사망합니다.") },
            { AnimalType.Scorpion, new CharacterInfoData("전갈", TerritoryType.Forest, "독 미션을 완료하면 독을 얻습니다.\n독이 있을 때 공격받으면 반격해 공격자를 죽입니다.", "독으로 반격해 포식자를 죽이면 승리합니다.") },
            { AnimalType.Owl, new CharacterInfoData("올빼미", TerritoryType.Sky, "예측 오브젝트에서 승리할 플레이어를\n한 명 예측할 수 있습니다.", "예측 오브젝트에서 승리할 플레이어를 예측하세요.\n예측한 플레이어가 승리하면 함께 승리합니다.") },
            { AnimalType.Zebra, new CharacterInfoData("얼룩말", TerritoryType.Field, "짝(타조)과 가까이 있으면 밤에도 시야가 넓어지고,\n짝이 공격받으면 알림을 받습니다.", "게임 종료까지 살아남으세요. ") },
            { AnimalType.Hippo, new CharacterInfoData("하마", TerritoryType.River, "사망하면 복수 미션이 생깁니다.", "게임 종료까지 살아남으세요.") },
            { AnimalType.Meerkat, new CharacterInfoData("미어캣", TerritoryType.Field, "밤에도 시야가 좁아지지 않습니다.", "게임 종료까지 살아남으세요.") },
            { AnimalType.Ostrich, new CharacterInfoData("타조", TerritoryType.Sky, "짝(얼룩말)과 가까이 있으면 밤에도 시야가 넓어지고,\n짝이 공격받으면 알림을 받습니다.", "게임 종료까지 살아남으세요.") },
            { AnimalType.Crow, new CharacterInfoData("까마귀", TerritoryType.Sky, "특별한 능력이 없습니다.", "늑대가 생존할 수 있도록 도와주세요. 늑대가 사망시 패배합니다.") },
            { AnimalType.Plover, new CharacterInfoData("악어새", TerritoryType.River, "특별한 능력이 없습니다.", "악어가 생존할 수 있도록 도와주세요. 악어가 사망시 패배합니다.") },
            { AnimalType.Chameleon, new CharacterInfoData("카멜레온", TerritoryType.Forest, "위장 오브젝트에서 다른 동물로 위장하면\n탐색당했을 때 그 동물로 보입니다.", "위장 오브젝트에서 다른 동물로 위장할 수 있습니다.\n게임 종료까지 살아남으세요.") },
        };
    }
    public struct CharacterInfoData
    {
        public string DisplayName;
        public TerritoryType HomeTerritory;
        // 시작 연출에 보여 주는 능력 설명
        public string AbilityDescription;
        public string WinConditionDescription;

        public CharacterInfoData(string displayName, TerritoryType territory, string ability, string winCondition)
        {
            DisplayName = displayName;
            HomeTerritory = territory;
            AbilityDescription = ability;
            WinConditionDescription = winCondition;
        }
    }
}
public static class PredatorPriority
{
    private static readonly Dictionary<PredatorType, int> priorityMap = new()
    {
        { PredatorType.Scorpion, -1 },
        { PredatorType.Hyena, 1 },
        { PredatorType.Hawk, 2 },
        { PredatorType.Crocodile, 3 },
        { PredatorType.Wolf, 4 },
        { PredatorType.Prey, 0 }
    };

    public static int GetPriority(PredatorType type)
    {
        return priorityMap.TryGetValue(type, out var p) ? p : 0;
    }
    

    public static bool CanAttack(PredatorType attacker, PredatorType target)
    {
        if (attacker == PredatorType.Scorpion) return false;
        if (target == PredatorType.Scorpion) return false;
        return GetPriority(attacker) > GetPriority(target);
    }
}
