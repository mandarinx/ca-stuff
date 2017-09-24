using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Stamping))]
public class StampingEditor : Editor {

    private Stamping stamping;

    private void OnEnable() {
        stamping = target as Stamping;
    }
//    
//    private void OnSceneGUI() {
//        if (Event.current.type != EventType.KeyUp ||
//            Event.current.keyCode != KeyCode.A) {
//            return;
//        }
//
//        Event.current.Use();
//        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//        RaycastHit hitInfo;
//        
//        if (!Physics.Raycast(worldRay, out hitInfo, 10000)) {
//            return;
//        }
//        
//        stamping.AddStamp(new Vector3(hitInfo.point.x, hitInfo.point.z, 3));
//    }
}
