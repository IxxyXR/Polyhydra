using System.Linq;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;


public class ScreenShot : MonoBehaviour
{
   RecorderController m_RecorderController;
   [Tooltip("Press Shift + this to trigger a screenshot")]
   public KeyCode ShortcutKey = KeyCode.S;
   public string FolderName = "Screenshots";
   public string FilenamePrefix = "screenshot_";
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
       imageRecorder.outputFile = Path.Combine(Application.persistentDataPath, FolderName, FilenamePrefix) + DefaultWildcard.Take;

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
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(ShortcutKey))
        {
            Debug.Log("terterter");
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
