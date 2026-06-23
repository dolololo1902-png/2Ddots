using UnityEngine;
using TMPro;

// 클래식 게임오브젝트 텍스트 콤보 컴포넌트
public class PlayerNameTag : MonoBehaviour
{
    public TextMeshPro NameText;

    public void SetName(string name)
    {
        if (NameText != null)
        {
            NameText.text = name;
        }
    }
}
