using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NickNamemanager
{
    private const string NicknameKey = "PlayerNickname";

    public static string GetNickname()
    {
        return PlayerPrefs.GetString(NicknameKey, "");
    }

    public static void SetNickname(string nickname)
    {
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();
    }

    public static bool HasNickname()
    {
        return !string.IsNullOrEmpty(GetNickname());
    }

    // 저장된 닉네임이 지금 규칙(길이, 금지어)에 맞으면 null, 아니면 안내 문구.
    // 금지어가 추가되기 전에 저장했거나 PlayerPrefs를 직접 고친 경우를 잡는다
    public static string ValidateSavedNickname()
    {
        return NicknameRules.Instance.Validate(GetNickname());
    }
}
