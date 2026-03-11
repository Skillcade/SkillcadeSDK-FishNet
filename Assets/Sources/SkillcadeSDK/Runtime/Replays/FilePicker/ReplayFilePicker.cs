using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace SkillcadeSDK.Replays
{
    /// <summary>
    /// Кроссплатформенный сервис выбора файлов реплеев.
    /// Поддерживает десктоп (Windows/Mac/Linux) и WebGL.
    /// </summary>
    public class ReplayFilePicker : MonoBehaviour
    {
        private const string ReplayExtension = "replay";
        private const string FileFilterDescription = "Replay Files";

        private event Action<ReplayFileResult> OnFileSelected;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ReplayFilePicker_OpenFileDialog(string gameObjectName, string callbackMethod, string accept);
#endif

        /// <summary>
        /// Открывает диалог выбора файла.
        /// </summary>
        public void OpenFileDialog()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenWebGLDialog();
#else
            OpenDesktopDialog();
#endif
        }

        /// <summary>
        /// Асинхронная версия выбора файла.
        /// </summary>
        public Task<ReplayFileResult> OpenFileDialogAsync()
        {
            var tcs = new TaskCompletionSource<ReplayFileResult>();
            
            void Handler(ReplayFileResult result)
            {
                OnFileSelected -= Handler;
                tcs.TrySetResult(result);
            }
            
            OnFileSelected += Handler;
            OpenFileDialog();
            
            return tcs.Task;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void OpenDesktopDialog()
        {
            try
            {
                var path = OpenDesktopFilePanel();
                
                if (string.IsNullOrEmpty(path))
                {
                    OnFileSelected?.Invoke(ReplayFileResult.Cancelled());
                    return;
                }

                using var file = File.OpenRead(path);
                var data = new byte[file.Length];
                file.Read(data, 0, data.Length);
                
                var fileName = Path.GetFileName(path);
                OnFileSelected?.Invoke(ReplayFileResult.Success(fileName, path, data));
            }
            catch (Exception e)
            {
                OnFileSelected?.Invoke(ReplayFileResult.Failed(e.Message));
            }
        }

        private string OpenDesktopFilePanel()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanel(
                "Select replay file",
                Application.streamingAssetsPath,
                ReplayExtension);
#elif UNITY_STANDALONE_WIN
            return WindowsFilePanelUtility.OpenFilePanel(ReplayExtension, FileFilterDescription);
#else
            return null;
#endif
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        private void OpenWebGLDialog()
        {
            ReplayFilePicker_OpenFileDialog(
                gameObject.name,
                nameof(OnFileSelectedWeb),
                $".{ReplayExtension}");
        }

        private void HandleWebGLResult(string fileName, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                OnFileSelected?.Invoke(ReplayFileResult.Cancelled());
            }
            else
            {
                OnFileSelected?.Invoke(
                    ReplayFileResult.Success(fileName, null, data));
            }
        }
#endif
        
#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnFileSelectedWeb(string base64WithName)
        {
            if (string.IsNullOrEmpty(base64WithName))
            {
                HandleWebGLResult(null, null);
                return;
            }

            var separatorIndex = base64WithName.IndexOf('|');
            if (separatorIndex < 0)
            {
                HandleWebGLResult(null, null);
                return;
            }

            var fileName = base64WithName.Substring(0, separatorIndex);
            var base64 = base64WithName.Substring(separatorIndex + 1);
            var data = Convert.FromBase64String(base64);
            
            HandleWebGLResult(fileName, data);
        }
#endif
    }
}