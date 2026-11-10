using UnityEngine;

// FishB 계열 미션의 던지는 도구 공통 부모: fishB 그물(FishNetB), fishC 작살(HarpoonC)
public abstract class FishNetBBase : MonoBehaviour
{
    public abstract void Init(FishB mission, RectTransform controlArea, RectTransform fishingArea);
}
