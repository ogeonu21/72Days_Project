#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// CustomEditor 어트리뷰트를 사용해 MeetingEvent 클래스와 연결
[CustomEditor(typeof(MeetingEvent))]
public class MeetingEventEditor : Editor
{
    // SerializedProperty를 사용해 인스펙터의 변수들을 관리
    SerializedProperty eventCategoryProp;
    SerializedProperty typeProp;
    SerializedProperty whoProp;

    private void OnEnable()
    {
        // 스크립트 활성화 시 변수들을 SerializedProperty로 초기화
        eventCategoryProp = serializedObject.FindProperty("eventCategory");
        typeProp = serializedObject.FindProperty("type");
        whoProp = serializedObject.FindProperty("who");
    }

    public override void OnInspectorGUI()
    {
        // SerializedObject를 업데이트하여 최신 변경사항을 가져옴
        serializedObject.Update();

        // eventCategory와 type 필드를 기본적으로 표시
        EditorGUILayout.PropertyField(eventCategoryProp);
        EditorGUILayout.PropertyField(typeProp);

        // type이 '전투하기'일 때만 who 필드를 표시
        // Enum 값은 정수로 변환하여 비교
        if (typeProp.enumValueIndex == (int)MeetingEvent.meetingEventType.전투하기)
        {
            EditorGUILayout.PropertyField(whoProp);
        }

        // 변경사항을 SO에 적용
        serializedObject.ApplyModifiedProperties();
    }
}
#endif