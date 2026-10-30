using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 닉네임 규칙(길이, 금지어, 중복). 타이틀에서 입력을 막고, 서버(RoomPlayer.CmdSetData)가 같은 규칙으로 다시 검사한다.
// 금지어는 Resources의 NicknameRules 에셋에서 고친다. 공백·기호·숫자를 지우고 비교하므로 단어는 글자만 적는다
[CreateAssetMenu(menuName = "Data/Nickname Rules", fileName = "NicknameRules")]
public class NicknameRules : ScriptableObject
{
    public const string ResourcePath = "NicknameRules";

    [SerializeField] private int maxLength = 8;
    [Tooltip("서버에서 쓸 수 없는 닉네임을 대신할 이름")]
    [SerializeField] private string fallbackNickname = "Player";
    [Tooltip("같은 닉네임이 있을 때 붙이기 시작하는 숫자")]
    [SerializeField] private int firstDuplicateNumber = 2;

    [Header("Words")]
    [Tooltip("닉네임 어디에든 들어 있으면 막는 단어")]
    [SerializeField] private string[] bannedWords = new string[0];
    [Tooltip("닉네임 전체가 같을 때만 막는 단어. 정상 단어에 들어가기 쉬운 말과 사칭을 막을 이름(시스템, 운영자 등)을 넣는다")]
    [SerializeField] private string[] exactBannedWords = new string[0];

    [Header("Messages")]
    [SerializeField] private string emptyMessage = "닉네임을 작성해주세요.";
    [Tooltip("{0}: 최대 글자 수")]
    [SerializeField] private string tooLongMessage = "닉네임은 {0}글자 이하로 지어주세요.";
    [SerializeField] private string bannedMessage = "사용할 수 없는 닉네임입니다.";
    [Tooltip("{0}: 방에서 쓰게 된 이름")]
    [SerializeField] private string duplicateNotice = "같은 닉네임이 있어 이 방에서는 {0}(으)로 표시됩니다.";
    [Tooltip("{0}: 방에서 쓰게 된 이름")]
    [SerializeField] private string replacedNotice = "사용할 수 없는 닉네임이라 이 방에서는 {0}(으)로 표시됩니다.";

    public string BannedMessage => bannedMessage;

    private static NicknameRules _instance;

    public static NicknameRules Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<NicknameRules>(ResourcePath);
                if (_instance == null)
                {
                    Debug.LogWarning($"[NicknameRules] Resources/{ResourcePath} 에 규칙이 없습니다. 기본 길이 제한만 적용합니다.");
                    _instance = CreateInstance<NicknameRules>();
                }
            }
            return _instance;
        }
    }

    // 타이틀 입력 검사. 쓸 수 있으면 null, 아니면 안내 문구
    public string Validate(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return emptyMessage;
        if (nickname.Length > maxLength) return string.Format(tooLongMessage, maxLength);
        return IsBanned(nickname) ? bannedMessage : null;
    }

    public bool IsBanned(string nickname)
    {
        string text = LettersOnly(nickname);
        if (text.Length == 0) return false;

        foreach (string word in exactBannedWords)
        {
            if (LettersOnly(word) == text) return true;
        }

        foreach (string word in bannedWords)
        {
            string letters = LettersOnly(word);
            if (letters.Length > 0 && text.Contains(letters)) return true;
        }
        return false;
    }

    // 서버: 받은 닉네임을 방에서 쓸 이름으로 만든다. 규칙에 맞지 않으면 대체 이름을 쓰고, 방에 같은 이름이 있으면 숫자를 붙인다.
    // 여기서 붙은 숫자는 그 방에서만 쓰이고 저장된 닉네임은 바뀌지 않는다
    public string MakeRoomNickname(string requested, IEnumerable<string> takenNames, out string notice)
    {
        string name = string.IsNullOrWhiteSpace(requested) ? string.Empty : requested.Trim();
        if (name.Length > maxLength) name = name.Substring(0, maxLength);

        bool replaced = name.Length == 0 || IsBanned(name);
        if (replaced) name = fallbackNickname;

        var taken = new HashSet<string>();
        foreach (string other in takenNames)
        {
            if (!string.IsNullOrEmpty(other)) taken.Add(ComparisonKey(other));
        }

        string unique = name;
        for (int number = firstDuplicateNumber; taken.Contains(ComparisonKey(unique)); number++)
            unique = name + number;

        notice = replaced ? string.Format(replacedNotice, unique)
            : unique != name ? string.Format(duplicateNotice, unique)
            : null;
        return unique;
    }

    // 같은 닉네임인지 비교하는 값. 대소문자와 공백만 달라서는 구분되지 않게 한다
    private static string ComparisonKey(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text.Normalize(NormalizationForm.FormKC))
        {
            if (!char.IsWhiteSpace(c) && !char.IsControl(c) && char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.Format)
                builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    // 금지어 비교용: 글자만 남긴다(시 발, 시.발, 시1발을 같은 말로 본다)
    private static string LettersOnly(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (char c in text.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetter(c)) builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }
}
