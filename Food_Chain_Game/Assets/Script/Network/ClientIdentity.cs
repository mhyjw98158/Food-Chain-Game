using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// 추방당한 플레이어를 다시 알아보기 위한 기기 ID. 닉네임은 바꿀 수 있고 IP는 같은 공유기의 다른 사람과 겹치므로 기기로 구분한다.
// 기기 고유값이 그대로 나가지 않도록 해시만 보낸다. 클라이언트가 보내는 값이므로 변조된 클라이언트까지 막지는 못한다
public static class ClientIdentity
{
    private static string _id;

    public static string Id
    {
        get
        {
            if (_id == null) _id = Create();
            return _id;
        }
    }

    private static string Create()
    {
        string source = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
        // 같은 PC의 ParrelSync 클론끼리 다른 플레이어로 구분되게 한다
        source += Application.dataPath;
#endif
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));

            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
