using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownHandler : MonoBehaviour
{
    [System.Serializable]
    public struct IndexedGameObject
    {
        public int index;
        public GameObject gameObject;
    }

    public List<IndexedGameObject> indexedGameObjects;

    private void Start()
    {
        
    }

    public void SetActiveGameObject(int index)
    {
        foreach (var indexedObject in indexedGameObjects)
        {
            indexedObject.gameObject.SetActive(indexedObject.index == index);
        }
    }
}
