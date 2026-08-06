using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening; // Sử dụng thư viện DOTween bạn vừa cài

namespace Meowdoku.Core
{
    // Script chuyên trách việc chuyển Scene bất đồng bộ mượt mà
    // Giúp game không bị giật lag khi chuyển từ Home sang Gameplay
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance;

        [Header("UI References")]
        public CanvasGroup faderCanvasGroup; // Dùng để làm mờ màn hình khi chuyển cảnh
        public Slider progressBar;           // Thanh tiến trình (tùy chọn)

        private void Awake()
        {
            // Thiết lập Singleton để SceneLoader tồn tại mãi mãi qua các màn
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Hàm được gọi từ các nơi khác khi muốn chuyển màn. Ví dụ: SceneLoader.Instance.LoadScene("GameplayScene");
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            // 1. Dùng DOTween làm mờ màn hình thành màu đen (Fade In)
            if (faderCanvasGroup != null)
            {
                faderCanvasGroup.blocksRaycasts = true;
                faderCanvasGroup.DOFade(1f, 0.5f); // Fade mất 0.5 giây
                yield return new WaitForSeconds(0.5f);
            }

            // 2. Bắt đầu tải Scene ngầm (Bất đồng bộ)
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            // Không cho phép Scene tự động hiển thị ngay khi tải xong (để đợi hiệu ứng)
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                // Cập nhật thanh tiến trình (nếu có)
                if (progressBar != null)
                {
                    progressBar.value = asyncLoad.progress;
                }

                // Khi tiến trình tải đạt 90% (0.9), Unity coi như đã tải xong dữ liệu
                if (asyncLoad.progress >= 0.9f)
                {
                    // 3. Kích hoạt Scene mới
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null; // Đợi khung hình tiếp theo
            }

            // 4. Scene mới đã load xong, làm sáng màn hình trở lại (Fade Out)
            if (faderCanvasGroup != null)
            {
                faderCanvasGroup.DOFade(0f, 0.5f); // Fade sáng mất 0.5 giây
                yield return new WaitForSeconds(0.5f);
                faderCanvasGroup.blocksRaycasts = false;
            }
        }
    }
}
