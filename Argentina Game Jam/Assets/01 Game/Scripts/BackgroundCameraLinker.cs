using UnityEngine;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class BackgroundCameraLinker : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cámara que renderiza el fondo de UI (debe ser tipo Overlay)")]
    public Camera uiBackgroundCamera;

    private Camera _mainCamera;
    private UniversalAdditionalCameraData _mainCameraData;

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();

        if (_mainCamera == null)
        {
            Debug.LogError("BackgroundCameraLinker: No se encontró componente Camera en este GameObject.");
            enabled = false;
            return;
        }

        if (uiBackgroundCamera == null)
        {
            Debug.LogError("BackgroundCameraLinker: 'uiBackgroundCamera' no está asignada. Por favor, arrastra la cámara de fondo en el Inspector.");
            enabled = false;
            return;
        }

        ConfigureCameras();
    }

    private void ConfigureCameras()
    {
        // 1. Configurar la cámara de fondo como Overlay
        var backgroundCameraData = uiBackgroundCamera.GetUniversalAdditionalCameraData();
        if (backgroundCameraData != null)
        {
            backgroundCameraData.renderType = CameraRenderType.Overlay;
        }

        // 2. Obtener datos adicionales de la cámara principal
        _mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        if (_mainCameraData == null)
        {
            Debug.LogError("BackgroundCameraLinker: No se pudo obtener UniversalAdditionalCameraData de la cámara principal.");
            return;
        }

        // 3. Configurar la cámara principal
        _mainCameraData.renderPostProcessing = true;
        _mainCamera.clearFlags = CameraClearFlags.Skybox;

        // 4. Limpiar el stack y añadir la cámara de fondo
        _mainCameraData.cameraStack.Clear();
        _mainCameraData.cameraStack.Add(uiBackgroundCamera);

        // 5. Configurar el Culling Mask para que la cámara principal ignore la capa UI
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer != -1)
        {
            _mainCamera.cullingMask &= ~(1 << uiLayer);
        }

        Debug.Log("BackgroundCameraLinker: Configuración completada.");
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BackgroundCameraLinker))]
    public class BackgroundCameraLinkerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BackgroundCameraLinker linker = (BackgroundCameraLinker)target;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Haz clic en el botón para configurar la cámara de fondo como Overlay y desactivar Clear Depth automáticamente.", MessageType.Info);

            if (GUILayout.Button("Configurar Cámara de Fondo (Editor)", GUILayout.Height(30)))
            {
                if (linker.uiBackgroundCamera == null)
                {
                    EditorUtility.DisplayDialog("Error", "Primero arrastra la cámara de fondo al campo 'UI Background Camera'.", "OK");
                    return;
                }

                // Configurar como Overlay
                var backgroundCameraData = linker.uiBackgroundCamera.GetUniversalAdditionalCameraData();
                if (backgroundCameraData != null)
                {
                    Undo.RecordObject(backgroundCameraData, "Configure Background Camera");
                    backgroundCameraData.renderType = CameraRenderType.Overlay;
                    
                    // Usar SerializedObject para modificar clearDepth
                    SerializedObject so = new SerializedObject(backgroundCameraData);
                    SerializedProperty clearDepthProp = so.FindProperty("m_ClearDepth");
                    if (clearDepthProp != null)
                    {
                        clearDepthProp.boolValue = false;
                        so.ApplyModifiedProperties();
                    }

                    EditorUtility.SetDirty(backgroundCameraData);
                    EditorUtility.SetDirty(linker.uiBackgroundCamera);
                }

                // Configurar culling mask
                linker.uiBackgroundCamera.cullingMask = LayerMask.GetMask("UI background");
                linker.uiBackgroundCamera.depth = 10;

                EditorUtility.DisplayDialog("Éxito", "La cámara de fondo ha sido configurada correctamente como Overlay con Clear Depth desactivado.", "OK");
            }
        }
    }
#endif
}