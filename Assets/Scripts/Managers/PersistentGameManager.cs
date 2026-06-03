using System.Collections.Generic;
using UnityEngine;

namespace RungTramTraSu
{
    public class PersistentGameManager : MonoBehaviour
    {
        private static PersistentGameManager instance;

        public static PersistentGameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("PersistentGameManager");
                    instance = go.AddComponent<PersistentGameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public List<Texture2D> savedPhotos = new List<Texture2D>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SavePhoto(Texture2D photo)
        {
            // Clone texture to prevent memory release on scene changes
            Texture2D newTex = new Texture2D(photo.width, photo.height, photo.format, false);
            newTex.SetPixels(photo.GetPixels());
            newTex.Apply();
            savedPhotos.Add(newTex);
            Debug.Log("[PersistentGameManager] Photo saved! Total: " + savedPhotos.Count);
        }

        public void ClearPhotos()
        {
            foreach (var tex in savedPhotos)
            {
                if (tex != null) Destroy(tex);
            }
            savedPhotos.Clear();
        }
    }
}
