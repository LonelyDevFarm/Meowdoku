using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    // Hệ thống Object Pool siêu nhẹ và đa năng.
    // Dùng để tái sử dụng các ô bàn cờ (Cell) và các hiệu ứng nổ (VFX) thay vì xóa đi tạo lại.
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Tự động tạo nếu chưa có (rất tiện khi test trực tiếp từ GameplayScene)
                    GameObject go = new GameObject("PoolManager_Auto");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Bộ lưu trữ các hàng đợi (Queue) chứa các Object đang rảnh rỗi.
        // Dùng tag (tên) làm chìa khóa để phân biệt Cell, VFX Mèo, VFX Lỗi...
        private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject); // Cho phép xài xuyên suốt các Scene
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // Lấy một Object ra khỏi "Hồ bơi" (Pool)
        public GameObject SpawnFromPool(string tag, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                _poolDictionary.Add(tag, new Queue<GameObject>());
            }

            GameObject objToSpawn;

            // Nếu trong hồ có sẵn đồ rảnh rỗi thì lấy ra xài
            if (_poolDictionary[tag].Count > 0)
            {
                objToSpawn = _poolDictionary[tag].Dequeue();
            }
            else
            {
                // Nếu hết đồ thì mới đẻ thêm (Instantiate)
                objToSpawn = Instantiate(prefab);
            }

            // Với UI (RectTransform), phải dùng SetParent(parent, false) để không bị lệch tọa độ và tỷ lệ
            objToSpawn.transform.SetParent(parent, false);
            
            // Dùng localPosition thay vì position để nó nằm đúng bên trong Board
            objToSpawn.transform.localPosition = position;
            objToSpawn.transform.localRotation = rotation;
            objToSpawn.transform.localScale = Vector3.one; 

            return objToSpawn;
        }

        // Cất một Object trở lại "Hồ bơi" để sau này tái sử dụng
        public void ReturnToPool(string tag, GameObject obj)
        {
            obj.SetActive(false); // Tắt nó đi thay vì Destroy()

            if (!_poolDictionary.ContainsKey(tag))
            {
                _poolDictionary.Add(tag, new Queue<GameObject>());
            }

            // Dọn dẹp phụ huynh (Parent) để nó nằm gọn trong PoolManager
            obj.transform.SetParent(transform);

            _poolDictionary[tag].Enqueue(obj);
        }
    }
}
