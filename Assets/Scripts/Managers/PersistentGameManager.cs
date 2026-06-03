using System.Collections.Generic;
using UnityEngine;

namespace RungTramTraSu
{
    public class PersistentGameManager : MonoBehaviour
    {
        public static PersistentGameManager Instance { get; private set; }

        public List<Texture2D> savedPhotos = new List<Texture2D>();

        private void Awake()
        {
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
