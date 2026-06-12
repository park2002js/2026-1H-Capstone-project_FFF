using UnityEngine;

namespace FFF.Core.Events
{
    /// <summary>
    /// int 데이터를 전달하는 이벤트 채널.
    /// 스테이지 번호, 데미지 수치 등에 활용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewIntEvent", menuName = "FFF/Events/Int Event")]
    public class IntEvent : GameEvent<int> { }
}