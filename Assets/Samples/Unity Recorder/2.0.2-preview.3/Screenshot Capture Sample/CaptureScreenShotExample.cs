

using System.Linq;
#if UNITY_EDITOR
using System.Collections;
using System.Dynamic;
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace UnityEngine.Recorder.Examples
{
    public class CaptureScreenShotExample : MonoBehaviour
    {
       RecorderController m_RecorderController;
       public GameObject HideMe;
             
       void OnEnable()
       {
           var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
           m_RecorderController = new RecorderController(controllerSettings);

           // Image
           var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
           imageRecorder.name = "My Image Recorder";
           imageRecorder.enabled = true;
           imageRecorder.outputFormat = ImageRecorderOutputFormat.JPEG;
           imageRecorder.captureAlpha = false;
           imageRecorder.outputFile = Path.Combine(Application.persistentDataPath, "Screenshots", "image_") + DefaultWildcard.Take;
    
           imageRecorder.imageInputSettings = new GameViewInputSettings
           {
               outputWidth = 2160,
               outputHeight = 2160,
           };
    
           // Setup Recording
           controllerSettings.AddRecorderSettings(imageRecorder);
           controllerSettings.SetRecordModeToSingleFrame(0);
       }
    
        void Update()
        {
            if (Input.GetKeyDown("s"))
            {
                HideMe.SetActive(false);
                m_RecorderController.StartRecording();
                StartCoroutine(WaitForCapture());
            }
        }
        
        IEnumerator WaitForCapture()
        {
            yield return new WaitWhile(() => m_RecorderController.IsRecording());
            Debug.Log($"Saved to {m_RecorderController.settings.recorderSettings.First().outputFile}");
            HideMe.SetActive(true);
        }
    }
 }
    
 #endif
