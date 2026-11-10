using UnityEngine;

// 결과 화면에 보여 줄 플레이어 한 명의 정보. 게임이 끝난 뒤에만 보내므로 역할을 담아도 된다
public struct GameResultEntry
{
    public uint netId;
    public string nickname;
    public AnimalType animalType;
    public bool isPredator;
    public bool isWinner;
    public Color color;
    public int costumeIndex;
}
