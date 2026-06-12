using UnityEngine;

namespace FFF.Core.Events
{
    /// <summary>
    /// string 데이터를 전달하는 이벤트 채널.
    /// Scene 이름 전달 등에 활용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStringEvent", menuName = "FFF/Events/String Event")]
    public class StringEvent : GameEvent<string> { }
}